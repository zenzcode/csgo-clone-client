using System;
using System.Collections;
using System.Collections.Generic;
using Enums;
using Helper;
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
        }

        private void SetRtt(float packageTime)
        {
            Rtt = (Time.realtimeSinceStartup - packageTime) * 1000;
            Debug.Log($"RTT IS {Rtt} ms");
        }

        private void Client_ClientDisconnected(object o, ClientDisconnectedEventArgs eventArgs)
        {
            EventManager.CallClientDisconnected(eventArgs.Id);
            CancelInvoke(nameof(SendRttRequest));
        }

        [MessageHandler((ushort)ServerToClientMessages.RTTAnswer)]
        private static void RttAnswer(Message message)
        {
            //TODO: Check for lost package later using tick
            Instance.SetRtt(message.GetFloat());
        }
    }
}
