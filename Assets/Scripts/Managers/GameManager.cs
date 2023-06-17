using Enums;
using Helper;
using Riptide;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public class GameManager : SingletonMonoBehavior<GameManager>
    {
        public GameState State { get; private set; } = GameState.Lobby;

        [MessageHandler((ushort)ServerToClientMessages.GameStateUpdated)]
        private static void OnGameStateUpdate(Message message)
        {
            Instance.State = (GameState)message.GetUShort();
        }
    }
}
