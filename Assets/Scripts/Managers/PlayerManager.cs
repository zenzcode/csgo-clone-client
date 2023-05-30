using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets;
using Enums;
using Helper;
using Manager;
using Riptide;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Managers
{
    public class PlayerManager : SingletonMonoBehavior<PlayerManager>
    {
        private Dictionary<ushort, Player.Player> _players;

        protected override void Awake()
        {
            base.Awake();
            _players = new Dictionary<ushort, Player.Player>();
            DontDestroyOnLoad(this);
        }

        [MessageHandler((ushort)ServerToClientMessages.SpawnClient)]
        private static void SpawnClient(Message message)
        {
            var playerId = message.GetUShort();
            var username = message.GetString();
            var isLeader = message.GetBool();

            Instance.Spawn(playerId, username, isLeader);
        }

        [MessageHandler((ushort)ServerToClientMessages.LeaderChanged)]
        private static void LeaderChanged(Message message)
        {
            var previousLeader = message.GetUShort();
            var newLeader = message.GetUShort();

            Instance.UpdateLeader(previousLeader, newLeader);
        }

        private void OnEnable()
        {
            EventManager.ClientDisconnected += EventManager_ClientDisconnected;
        }

        private void OnDisable()
        {
            EventManager.ClientDisconnected -= EventManager_ClientDisconnected;
        }

        private void EventManager_ClientDisconnected(ushort leaverId)
        {
            if (!_players.ContainsKey(leaverId))
            {
                return;
            }

            Destroy(_players[leaverId].gameObject);
            _players.Remove(leaverId);
        }

        private void UpdateLeader(ushort oldLeaderId, ushort newLeaderId)
        {
            Debug.Log($"Leader Update from {oldLeaderId} to {newLeaderId}");
            if (!_players.TryGetValue(oldLeaderId, out var oldLeader))
            {
                return;
            }

            oldLeader.IsLeader = false;

            if (!_players.TryGetValue(newLeaderId, out var newLeader))
            {
                return;
            }

            newLeader.IsLeader = true;
        }

        public Player.Player GetPlayer(ushort clientId)
        {
            return _players.FirstOrDefault(pair => pair.Value.PlayerId == clientId).Value;
        }

        private void Spawn(ushort playerId, string username, bool isLeader)
        {
            if (_players.ContainsKey(playerId))
            {
                return;
            }

            var newPlayer = Instantiate(AssetManager.Instance.LobbyPlayer);

            if (!newPlayer.TryGetComponent<Player.Player>(out var player))
            {
                return;
            }

            player.PlayerId = playerId;
            player.Username = username;
            player.IsLeader = isLeader;
            player.IsLocal = playerId == NetworkManager.Instance.Client.Id;
            newPlayer.name = $"{username} ({playerId})";

            _players.Add(playerId, player);
        }
    }
}