using System;
using System.Collections;
using System.Collections.Generic;
using Enums;
using Helper;
using Managers;
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

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this);
            
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
            SceneManager.LoadScene("Lobby");
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
            //TODO: Add Tick later to recognize lost package.
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
            CancelInvoke(nameof(SendRttRequest));
            SceneManager.LoadScene("MainMenu");
            EventManager.CallLocalPlayerDisconnect();
        }

        [MessageHandler((ushort)ServerToClientMessages.RTTAnswer)]
        private static void RttAnswer(Message message)
        {
            //TODO: Check for lost package later using tick
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

        public float GetServerTime()
        {
            return Time.timeSinceLevelLoad + ClientServerDelta;
        }
    }
}
