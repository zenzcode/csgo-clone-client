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
        }

        [MessageHandler((ushort)ServerToClientMessages.SpawnLobbyClient)]
        private static void SpawnClient(Message message)
        {
            var playerId = message.GetUShort();
            var username = message.GetString();
            var isLeader = message.GetBool();
            var lastKnownRtt = message.GetFloat();

            Instance.Spawn(playerId, username, isLeader, lastKnownRtt);
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
            EventManager.LocalPlayerDisconnect += EventManager_LocalPlayerDisconnect;
        }

        private void OnDisable()
        {
            EventManager.ClientDisconnected -= EventManager_ClientDisconnected;
            EventManager.LocalPlayerDisconnect -= EventManager_LocalPlayerDisconnect;
        }

        public void ClearPlayers()
        {
            foreach(var player in _players.Values)
            {
                Destroy(player.gameObject);
            }
            _players.Clear();
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

        private void EventManager_LocalPlayerDisconnect()
        {
            ClearPlayers();
        }

        private void UpdateLeader(ushort oldLeaderId, ushort newLeaderId)
        {
            Debug.Log($"Leader Update from {oldLeaderId} to {newLeaderId}");
            if (!_players.TryGetValue(newLeaderId, out var newLeader))
            {
                return;
            }

            newLeader.IsLeader = true;

            EventManager.CallLeaderChanged(newLeaderId);

            if (!_players.TryGetValue(oldLeaderId, out var oldLeader))
            {
                return;
            }

            oldLeader.IsLeader = false;
        }

        public Player.Player GetPlayer(ushort clientId)
        {
            return _players.FirstOrDefault(pair => pair.Value.PlayerId == clientId).Value;
        }

        public Player.Player GetLocalPlayer()
        {
            return _players.FirstOrDefault(player => player.Value.IsLocal).Value;
        }

        public Player.Player GetCurrentLeader()
        {
            return _players.FirstOrDefault(player => player.Value.IsLeader).Value;
        }

        public int GetPlayerCount()
        {
            return _players.Count;
        }

        public bool IsLocal(Player.Player player)
        {
            return player.PlayerId == NetworkManager.Instance.Client.Id;
        }
        
        public bool IsLocal(ushort playerId)
        {
            return playerId == NetworkManager.Instance.Client.Id;
        }

        private void Spawn(ushort playerId, string username, bool isLeader, float lastKnownRtt)
        {
            if (_players.ContainsKey(playerId))
            {
                return;
            }

            var newPlayer =Instantiate(AssetManager.Instance.LobbyPlayer);

            if (!newPlayer.TryGetComponent<Player.Player>(out var player))
            {
                return;
            }

            player.PlayerId = playerId;
            player.Username = username;
            player.IsLeader = isLeader;
            player.IsLocal = playerId == NetworkManager.Instance.Client.Id;
            player.LastKnownRtt = lastKnownRtt;
            newPlayer.name = $"{username} ({playerId})";

            _players.Add(playerId, player);

            if (player.IsLocal)
            {
                EventManager.CallLocalPlayerReceived();
            }
        }

        private void SpawnInMap(ushort playerId, Vector3 position, Quaternion rotation)
        {
            var player = GetPlayer(playerId);

            if(!player)
            {
                return;
            }

            Instantiate(AssetManager.Instance.GamePlayer, player.transform);

            player.gameObject.transform.position = position;
            player.gameObject.transform.rotation = rotation;
        }

        [MessageHandler((ushort)ServerToClientMessages.SpawnInMap)]
        private static void SpawnInMap(Message message)
        {
            Instance.SpawnInMap(message.GetUShort(), message.GetVector3(), message.GetQuaternion());
        }
    }
}