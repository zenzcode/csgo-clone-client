using System.Collections;
using System.Collections.Generic;
using Enums;
using Helper;
using Player;
using Riptide;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Managers
{
    public class TeamManager : SingletonMonoBehavior<TeamManager>
    {
        private Dictionary<Team, List<ushort>> _teamMembers;
        [SerializeField] private Color _defenderColor;
        [SerializeField] private Color _attackerColor;
        [SerializeField] private Color _defenderLocalColor;
        [SerializeField] private Color _attackerLocalColor;
        [HideInInspector] public Color DefenderColor => _defenderColor;
        [HideInInspector] public Color AttackerColor => _attackerColor;
        [HideInInspector] public Color DefenderLocalColor => _defenderLocalColor;
        [HideInInspector] public Color AttackerLocalColor => _attackerLocalColor;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this);
            _teamMembers = new Dictionary<Team, List<ushort>>()
            {
                { Team.Attacker, new List<ushort>() },
                { Team.Defender, new List<ushort>() }
            };
        }

        private void OnEnable()
        {
            EventManager.LocalPlayerDisconnect += EventManager_LocalPlayerDisconnect;
        }

        private void OnDisable()
        {
            EventManager.LocalPlayerDisconnect -= EventManager_LocalPlayerDisconnect;
        }

        private void EventManager_LocalPlayerDisconnect()
        {
            foreach (var team in _teamMembers.Values) {
                team.Clear();
            }
        }

        public Team GetTeam(ushort clientId)
        {
            foreach (var team in _teamMembers.Keys)
            {
                if (_teamMembers[team].Contains(clientId))
                {
                    return team;
                }
            }

            return Team.None;
        }

        private int GetPlayerCount(Team team)
        {
            return _teamMembers[team]?.Count ?? 0;
        }

        private void RemoveFromAllTeams(ushort clientId)
        {
            foreach (var team in _teamMembers.Keys)
            {
                if (_teamMembers[team].Remove(clientId))
                {
                    var player = PlayerManager.Instance.GetPlayer(clientId);
                    if (player)
                    {
                        player.Team = Team.None;
                    }
                }
            }
        }

        private void AddToTeam(ushort playerId, Team team)
        {
            if (!_teamMembers.ContainsKey(team))
            {
                return;
            }

            _teamMembers[team].Add(playerId);
            PlayerManager.Instance.GetPlayer(playerId).Team = team;
            EventManager.CallTeamChanged(playerId);
        }

        private void PlayerLeft(ushort playerId)
        {
            RemoveFromAllTeams(playerId);
            EventManager.CallTeamChanged(playerId);
        }

        [MessageHandler((ushort) ServerToClientMessages.TeamSet)]
        private static void TeamSet(Message message)
        {
            var playerId = message.GetUShort();
            var team = (Team)message.GetUShort();

            switch (team)
            {
                case Team.Attacker:
                case Team.Defender:
                    Instance.RemoveFromAllTeams(playerId);
                    Instance.AddToTeam(playerId, team);
                    break;
                case Team.None:
                    Instance.PlayerLeft(playerId);
                    break;
            }
        }
    }
}