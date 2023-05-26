using System;
using System.Collections;
using System.Collections.Generic;
using Helper;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Managers.UI.MainMenu
{
    public class MainMenuManager : SingletonMonoBehavior<MainMenuManager>
    {
        [SerializeField] private GameObject mainScene;
        [SerializeField] private GameObject pleaseWaitScene;
        [SerializeField] private TMP_InputField ip;
        [SerializeField] private TMP_InputField port;
        [SerializeField] private TMP_InputField username;

        private void OnEnable()
        {
            EventManager.ConnectFailed += EventManager_ConnectFailed;
        }

        private void OnDisable()
        {
            EventManager.ConnectFailed -= EventManager_ConnectFailed;
        }

        public void TryConnect()
        {
            NetworkManager.Instance.Connect(GetIP(), GetPort(), GetUsername());
            ShowPleaseWaitScene();
        }

        private void EventManager_ConnectFailed()
        {
            ShowMainScene();
        }

        private void ShowMainScene()
        {
            pleaseWaitScene.SetActive(false);
            mainScene.SetActive(true);
        }

        private void ShowPleaseWaitScene()
        {
            mainScene.SetActive(false);
            pleaseWaitScene.SetActive(true);
        }

        private string GetIP()
        {
            return string.IsNullOrEmpty(ip.text) ? "127.0.0.1" : ip.text;
        }

        private string GetPort()
        {
            return string.IsNullOrEmpty(port.text) ? "27901" : port.text;
        }

        private string GetUsername()
        {
            return string.IsNullOrEmpty(username.text) ? "Guest" : username.text;
        }
    }
}

