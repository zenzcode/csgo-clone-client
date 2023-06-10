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
        private Dictionary<ushort, PlayerBar> _playerBars;
        [SerializeField] private GameObject _defenderContainer;
        [SerializeField] private GameObject _attackerContainer;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private Button _switchTeamButton;
        [SerializeField] private float _buttonDebounceTime = 3f;
        [SerializeField] private GameObject _clock;
        [SerializeField] private TMP_Text _clockText;

        protected override void Awake()
        {
            base.Awake();
            _playerBars = new Dictionary<ushort, PlayerBar>();
        }

        private void OnEnable()
        {
            EventManager.TeamChanged += EventManager_TeamChanged;
            EventManager.LeaderChanged += EventManager_LeaderChanged;
            EventManager.RttUpdated += EventManager_RttUpdated;
            EventManager.TimerStarted += EventManager_TimerStarted;
            EventManager.TimerUpdate += EventManager_TimerUpdate;
            EventManager.TimerEnded += EventManager_TimerEnded;
        }

        private void OnDisable()
        {
            EventManager.TeamChanged -= EventManager_TeamChanged;
            EventManager.LeaderChanged -= EventManager_LeaderChanged;
            EventManager.RttUpdated -= EventManager_RttUpdated;
            EventManager.TimerStarted -= EventManager_TimerStarted;
            EventManager.TimerUpdate -= EventManager_TimerUpdate;
            EventManager.TimerEnded -= EventManager_TimerEnded;
        }

        private void EventManager_TeamChanged(ushort playerId)
        {
            if (_playerBars.ContainsKey(playerId))
            {
                Destroy(_playerBars[playerId].gameObject);
                _playerBars[playerId].KickButton.onClick.RemoveAllListeners();
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

            image.color = GetColor(team, playerId);

            if(!newPlayerBar.TryGetComponent<PlayerBar>(out var playerBar))
            {
                return;
            }

            playerBar.Owner = playerId;
            _playerBars.Add(playerId, playerBar);

            var player = PlayerManager.Instance.GetPlayer(playerId);

            if (!player)
            {
                return;
            }

            EventManager.CallRttUpdated(playerId, player.LastKnownRtt);

            UpdateLeaderVisuals();

            playerBar.KickButton.onClick.AddListener(() =>
            {
                KickPlayer(playerBar.Owner);
            });

            playerBar.UsernameText.text = $"{player.Username}";
        }

        private Color GetColor(Team team, ushort playerId)
        {
            var localPlayer = PlayerManager.Instance.GetLocalPlayer();

            switch (team)
            {
                case Team.Attacker:
                    if (!localPlayer || (localPlayer != null && localPlayer.PlayerId != playerId))
                        return TeamManager.Instance.AttackerColor;
                    return TeamManager.Instance.AttackerLocalColor;
                case Team.Defender:
                    if (!localPlayer || (localPlayer != null && localPlayer.PlayerId != playerId))
                        return TeamManager.Instance.DefenderColor;
                    return TeamManager.Instance.DefenderLocalColor;
            }

            return Color.black;
        }

        private void EventManager_TimerStarted(Timer timer, int startTime)
        {
            if(timer == Timer.WarmupTimer)
            {
                _clock.SetActive(true);
                _clockText.SetText(GetTimeInFormat(startTime));
            }
        }

        private void EventManager_TimerUpdate(Timer timer, int remaining)
        {
            if(timer == Timer.WarmupTimer)
            {
                _clockText.SetText(GetTimeInFormat(remaining));
            }
        }

        private void EventManager_TimerEnded(Timer timer)
        {
            if(timer == Timer.WarmupTimer)
            {
                _clockText.SetText("Loading...");
            }
        }

        private string GetTimeInFormat(int seconds)
        {
            var minutes = (seconds / 60) % 60;
            seconds %= 60;

            return $"{minutes.ToString("D2")}:{seconds.ToString("D2")}";
        }


        private void EventManager_LeaderChanged(ushort newPlayerId)
        {
            UpdateLeaderVisuals();
        }

        private void EventManager_RttUpdated(ushort clientId, float rtt)
        {
            if (!_playerBars.ContainsKey(clientId))
            {
                return;
            }

            //TODO: Maybe only send integer rtt to everyone on update, since only that is actively used.
            _playerBars[clientId].PingText.SetText($"{Mathf.CeilToInt(rtt)}ms");
        }

        private void UpdateLeaderVisuals()
        {
            foreach(var playerBar in _playerBars.Values)
            {
                playerBar.LeaderIcon.SetActive(false);
                playerBar.KickButton.gameObject.SetActive(false);
            }

            UpdateStartButtonVisibility();
            UpdateKickIconVisibility();

            var leader = PlayerManager.Instance.GetCurrentLeader();
            if (!leader)
                return;

            if(!_playerBars.ContainsKey(leader.PlayerId))
            {
                return;
            }
            
            _playerBars[leader.PlayerId].LeaderIcon.SetActive(true);
        }

        private void UpdateStartButtonVisibility()
        {
            _startButton.gameObject.SetActive(PlayerManager.Instance.GetLocalPlayer() == PlayerManager.Instance.GetCurrentLeader());
        }

        private void UpdateKickIconVisibility()
        {
            foreach(var playerBar in _playerBars.Values)
            {
                playerBar.KickButton.gameObject.SetActive(PlayerManager.Instance.GetLocalPlayer() == PlayerManager.Instance.GetCurrentLeader() && 
                    playerBar.Owner != PlayerManager.Instance.GetLocalPlayer().PlayerId);
            }
        }

        public void LeaveGame()
        {
            NetworkManager.Instance.Client.Disconnect();
        }

        public void StartGame()
        {
            if (GameManager.Instance.IsTimerRunning)
                return;

            var message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerMessages.StartGameTimer);
            NetworkManager.Instance.Client.Send(message);
            _startButton.enabled = false;
        }

        public void SwitchTeam()
        {
            var message = Message.Create(MessageSendMode.Unreliable, (ushort)ClientToServerMessages.SwitchTeamRequest);
            NetworkManager.Instance.Client.Send(message);
            _switchTeamButton.enabled = false;
            Invoke(nameof(ReenableSwitchButton), _buttonDebounceTime);
        }

        private void ReenableSwitchButton()
        {
            _switchTeamButton.enabled = true;
        }

        private void KickPlayer(ushort playerId)
        {
            Debug.Log("Send kick request");
            var kickMessage = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerMessages.KickRequest);
            kickMessage.AddUShort(playerId);
            NetworkManager.Instance.Client.Send(kickMessage);
        }
    }
}