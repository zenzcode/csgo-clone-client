using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets
{
    public class AssetManager : MonoBehaviour
    {
        private static AssetManager _instance;
        public static AssetManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<AssetManager>("Assets/GameAssets");
                }

                return _instance;
            }
            private set => _instance = value;
        }

        [SerializeField] private GameObject _lobbyPlayer;
        public GameObject LobbyPlayer => _lobbyPlayer;

        [SerializeField] private GameObject _lobbyPlayerUIPrefab;
        public GameObject LobbyPlayerUIPrefab => _lobbyPlayerUIPrefab;
    }
}