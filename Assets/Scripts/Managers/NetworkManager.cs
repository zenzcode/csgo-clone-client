using System;
using System.Collections;
using System.Collections.Generic;
using Enums;
using Helper;
using Riptide;
using Riptide.Utils;
using UnityEngine;

namespace Manager
{
    public class NetworkManager : SingletonMonoBehavior<NetworkManager>
    {
        public Client Client { get; private set; }

        private string _requestedUsername = string.Empty;

        protected override void Awake()
        {
            base.Awake();
            
#if UNITY_EDITOR
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, true);
#else
            RiptideLogger.Initialize(Debug.Log, true);
#endif
            
            Client = new Client();
            Client.ConnectionFailed += Client_ConnectionFailed;
            Client.Connected += Client_Connected;
        }

        private void Client_ConnectionFailed(object o, ConnectionFailedEventArgs eventArgs)
        {
            EventManager.CallConnectFailed();
        }

        private void Client_Connected(object o, EventArgs eventArgs)
        {
            SendUsername();
        }

        private void SendUsername()
        {
            var message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerMessages.Username);
            message.AddString(_requestedUsername);
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
        }
    }
}
