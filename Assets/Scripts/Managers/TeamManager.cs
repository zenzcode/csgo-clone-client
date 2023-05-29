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
                    Instance.RemoveFromAllTeams(playerId);
                    break;
            }
        }
    }
}