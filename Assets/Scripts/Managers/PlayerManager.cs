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

            Instance.Spawn(playerId, username);
        }
        public Player.Player GetPlayer(ushort clientId)
        {
            return _players.First(pair => pair.Value.PlayerId == clientId).Value;
        }

        private void Spawn(ushort playerId, string username)
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
            player.IsLocal = playerId == NetworkManager.Instance.Client.Id;
            newPlayer.name = $"{username} ({playerId})";

            _players.Add(playerId, player);
        }
    }
}