using System.Collections;
using System.Collections.Generic;
using Assets;
using Enums;
using Helper;
using Manager;
using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Managers.UI.Lobby
{
    public class LobbyUIManager : SingletonMonoBehavior<LobbyUIManager>
    {
        private Dictionary<ushort, GameObject> _playerBars;
        [SerializeField] private GameObject _defenderContainer;
        [SerializeField] private GameObject _attackerContainer;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private Button _switchTeamButton;

        protected override void Awake()
        {
            base.Awake();
            _playerBars = new Dictionary<ushort, GameObject>();
        }

        private void OnEnable()
        {
            EventManager.TeamChanged += EventManager_TeamChanged;
            EventManager.LocalPlayerReceived += EventManager_LocalPlayerReceived;
            EventManager.LeaderChanged += EventManager_LeaderChanged;
        }

        private void OnDisable()
        {
            EventManager.TeamChanged -= EventManager_TeamChanged;
            EventManager.LocalPlayerReceived -= EventManager_LocalPlayerReceived;
            EventManager.LeaderChanged -= EventManager_LeaderChanged;
        }

        private void EventManager_TeamChanged(ushort playerId)
        {
            if (_playerBars.ContainsKey(playerId))
            {
                Destroy(_playerBars[playerId]);
                _playerBars.Remove(playerId);
            }

            var team = TeamManager.Instance.GetTeam(playerId);

            if (team == Team.None)
            {
                return;
            }

            var newPlayerBar = Instantiate(AssetManager.Instance.LobbyPlayerUIPrefab,
                team == Team.Attacker ? _attackerContainer.transform : _defenderContainer.transform);


            if (!newPlayerBar.TryGetComponent<Image>(out var image))
            {
                return;
            }

            image.color = team == Team.Attacker
                ? TeamManager.Instance.AttackerColor
                : TeamManager.Instance.DefenderColor;

            var usernameText = newPlayerBar.GetComponentInChildren<TMP_Text>();

            if (!usernameText)
            {
                return;
            }

            usernameText.text = $"{PlayerManager.Instance.GetPlayer(playerId).Username}";

            _playerBars.Add(playerId, newPlayerBar);
        }

        private void EventManager_LocalPlayerReceived()
        {
           UpdateStartButtonVisibility();
        }

        private void EventManager_LeaderChanged(ushort newPlayerId)
        {
            UpdateStartButtonVisibility();
        }

        private void UpdateStartButtonVisibility()
        {
            _startButton.gameObject.SetActive(PlayerManager.Instance.GetLocalPlayer() == PlayerManager.Instance.GetCurrentLeader());
        }

        public void LeaveGame()
        {
            NetworkManager.Instance.Client.Disconnect();
            SceneManager.LoadScene("MainMenu");
        }

        public void SwitchTeam()
        {
            var message = Message.Create(MessageSendMode.Unreliable, (ushort)ClientToServerMessages.SwitchTeamRequest);
            NetworkManager.Instance.Client.Send(message);
        }
    }
}