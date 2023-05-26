using System;
using System.Collections;
using System.Collections.Generic;
using Helper;
using Riptide;
using Riptide.Utils;
using UnityEngine;

namespace Manager
{
    public class NetworkManager : SingletonMonoBehavior<NetworkManager>
    {
        public Client Client { get; private set; }

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
        }

        private void Client_ConnectionFailed(object o, ConnectionFailedEventArgs eventArgs)
        {
            EventManager.CallConnectFailed();
        }

        public void Connect(string ip, string port, string username)
        {
            Client.Connect($"{ip}:{port}");
        }

        private void FixedUpdate()
        {
            Client.Update();
        }

        private void OnApplicationQuit()
        {
            Client.Disconnect();
        }
    }
}
