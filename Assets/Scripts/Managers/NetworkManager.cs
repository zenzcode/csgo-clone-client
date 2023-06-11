using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enums;
using Helper;
using Managers;
using Misc;
using Riptide;
using Riptide.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manager
{
    public class NetworkManager : SingletonMonoBehavior<NetworkManager>
    {
        public Client Client { get; private set; }

        private string _requestedUsername = string.Empty;

        [SerializeField] [Range(1, 15)] private float _rttCheckupInterval = 5;

        //RoundTripTime in MS
        [HideInInspector] public float Rtt = 0;

        private float _clientServerDelta = 0;

        [HideInInspector] public float ClientServerDelta => _clientServerDelta;

        private float _serverStartupTime = 0;

        [HideInInspector] public float ServerStartupTime => _serverStartupTime;

        private uint _tick = 0;

        [HideInInspector] public uint Tick => _tick;

        private const uint MaxDiff = 5;

        protected override void Awake()
        {
            base.Awake();
            SceneManager.LoadScene(Statics.MainMenuMapName, LoadSceneMode.Additive);
            
#if UNITY_EDITOR
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, true);
#else
            RiptideLogger.Initialize(Debug.Log, true);
#endif
            
            Client = new Client();
            Client.ConnectionFailed += Client_ConnectionFailed;
            Client.Connected += Client_Connected;
            Client.ClientDisconnected += Client_ClientDisconnected;
            Client.Disconnected += Client_Disconnected;
        }

        private void Client_ConnectionFailed(object o, ConnectionFailedEventArgs eventArgs)
        {
            EventManager.CallConnectFailed();
        }

        private void Client_Connected(object o, EventArgs eventArgs)
        {
            SceneManager.UnloadSceneAsync(Statics.MainMenuMapName);
            SceneManager.LoadScene(Statics.LobbyMapName, LoadSceneMode.Additive);
            SendUsername();
            InvokeRepeating(nameof(SendRttRequest), 0, _rttCheckupInterval);
        }

        private void SendUsername()
        {
            var message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerMessages.Username);
            message.AddString(_requestedUsername);
            Client.Send(message);
        }

        private void SendRttRequest()
        {
            var message = Message.Create(MessageSendMode.Unreliable, (ushort)ClientToServerMessages.RequestRTT);
            message.AddFloat(Time.realtimeSinceStartup);
            Client.Send(message);

        }

        public void Connect(string ip, string port, string username)
        {
            Client.Connect($"{ip}:{port}");
            _requestedUsername = username;
        }

        private void FixedUpdate()
        {
            if(Client.IsConnected)
            {
                _tick++;
            }
            Client.Update();
        }

        private void OnApplicationQuit()
        {
            Client.Disconnect();
            Client.ConnectionFailed -= Client_ConnectionFailed;
            Client.Connected -= Client_Connected;
            Client.ClientDisconnected -= Client_ClientDisconnected;
            Client.Disconnected -= Client_Disconnected;
        }

        private void SetRtt(float packageTime, float serverStartupTime, float serverTimeSinceLoad)
        {
            Rtt = (Time.realtimeSinceStartup - packageTime) * 1000;
            var serverTime = serverTimeSinceLoad + ((Rtt / 1000) * 0.5f);
            _serverStartupTime = serverStartupTime;
            _clientServerDelta = serverTime - Time.timeSinceLevelLoad;

            SendRttUpdate(Rtt);
        }

        private void SendRttUpdate(float rtt)
        {
            var message = Message.Create(MessageSendMode.Unreliable, (ushort)ClientToServerMessages.RTTUpdate);
            message.AddFloat(rtt);
            Client.Send(message);
        }

        private void Client_ClientDisconnected(object o, ClientDisconnectedEventArgs eventArgs)
        {
            EventManager.CallClientDisconnected(eventArgs.Id);
        }

        private void Client_Disconnected(object o, DisconnectedEventArgs eventArgs)
        {
            _tick = 0;
            CancelInvoke(nameof(SendRttRequest));
            for(var sceneIdx = 0; sceneIdx < SceneManager.sceneCount; ++sceneIdx)
            {
                var scene = SceneManager.GetSceneAt(sceneIdx);
                if (scene.name.Equals(Statics.PersistentMapName))
                    continue;

                SceneManager.UnloadSceneAsync(scene);
            }
            SceneManager.LoadScene(Statics.MainMenuMapName, LoadSceneMode.Additive);
            EventManager.CallLocalPlayerDisconnect();
        }

        private void ServerTickReceived(uint serverTick)
        {
            if(serverTick - _tick > MaxDiff)
            {
                Debug.LogWarning($"Server and Client are more than 5 ticks apart. (Server: {serverTick}; Client: {_tick})");
            }
            _tick = serverTick;
        }

        [MessageHandler((ushort)ServerToClientMessages.RTTAnswer)]
        private static void RttAnswer(Message message)
        {
            Instance.SetRtt(message.GetFloat(), message.GetFloat(), message.GetFloat());
        }

        [MessageHandler((ushort)ServerToClientMessages.RTTUpdate)]
        private static void RttUpdated(Message message)
        {
            var clientId = message.GetUShort();
            var rtt = message.GetFloat();

            var player = PlayerManager.Instance.GetPlayer(clientId);
            if (!player)
                return;

            player.LastKnownRtt = rtt;
            EventManager.CallRttUpdated(clientId, rtt);
        }

        [MessageHandler((ushort)ServerToClientMessages.TickUpdated)]
        private static void TickUpdateReceived(Message message)
        {
            var serverTick = message.GetUInt();

            Instance.ServerTickReceived(serverTick);
        }

        [MessageHandler((ushort)ServerToClientMessages.Travel)]
        private static void Travel(Message message)
        {
            var levelName = message.GetString();
            SceneManager.UnloadSceneAsync(Statics.LobbyMapName);
            var levelLoad = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
            levelLoad.completed += Instance.LevelLoad_Completed;
        }

        private void LevelLoad_Completed(AsyncOperation operation)
        {
            operation.completed -= LevelLoad_Completed;
            SendTravelFinishMessage();
        }

        private void SendTravelFinishMessage()
        {
            var message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerMessages.TravelFinished);
            NetworkManager.Instance.Client.Send(message);
        }

        public float GetServerTime()
        {
            return Time.timeSinceLevelLoad + ClientServerDelta;
        }
    }
}
