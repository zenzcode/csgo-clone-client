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

        private bool _isTimerRunning = false;
        [HideInInspector] public bool IsTimerRunning => _isTimerRunning;
        private int _remainingTime = 0;
        private int _duration = 0;
        private float _start = 0;
        private Timer _activeTimer;

        [MessageHandler((ushort)ServerToClientMessages.TimerStarted)]
        private static void TimerStarted(Message message)
        {
            Instance.TryStartTimer(message);
        }

        private void TryStartTimer(Message message)
        {
            if (IsTimerRunning)
            {
                return;
            }

            var timer = (Timer)message.GetUShort();
            var time = message.GetInt();
            var startTime = message.GetFloat();

            EventManager.CallTimerStarted(timer, time);
            _activeTimer = timer;
            _remainingTime = time;
            _duration = time;
            _start = startTime;
            _isTimerRunning = true;
        }

        private void Update()
        {
            if(!IsTimerRunning)
            {
                return;
            }
            var startThisTick = _remainingTime;
            _remainingTime = Mathf.CeilToInt(_duration - NetworkManager.Instance.GetServerTime() + _start);

            if (startThisTick != _remainingTime)
            {
                EventManager.CallTimerUpdate(_activeTimer, Mathf.CeilToInt(_remainingTime));
            }

            if(_remainingTime <= 0)
            {
                EventManager.CallTimerEnded(_activeTimer);
                _isTimerRunning = false;
                Debug.Log("TIMER COMPLETE");
            }
        }
    }
}
