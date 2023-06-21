using Enums;
using Helper;
using Riptide;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TimerManagement;
using Unity.VisualScripting;
using UnityEngine;
using Timer = Enums.Timer;

namespace TimerManagement
{
    struct RunningTimer
    {
        public Enums.Timer Timer;
        public int RemainingTime;
        public int Duration;
        public float ServerStart;
    }
}

namespace Manager
{
    [DisallowMultipleComponent]
    public class TimerManager : SingletonMonoBehavior<TimerManager>
    {
        private List<RunningTimer> _runningTimers;

        protected override void Awake()
        {
            base.Awake();
            _runningTimers = new List<RunningTimer>();  
        }


        [MessageHandler((ushort)ServerToClientMessages.TimerStarted)]
        private static void TimerStarted(Message message)
        {
            Instance.TryStartTimer(message);
        }

        [MessageHandler((ushort)ServerToClientMessages.TimerAborted)]
        private static void TimerAborted(Message message)
        {
            Instance.TryStopTimer(message);
        }

        private void TryStopTimer(Message message)
        {
            var stoppedTimer = (Enums.Timer)message.GetUShort();

            if (!IsTimerRunning(stoppedTimer))
            {
                return;
            }

            _runningTimers = _runningTimers.Where(runningTimer => runningTimer.Timer != stoppedTimer).ToList();
            EventManager.CallTimerStopped(stoppedTimer);
        }
        
        private void TryStartTimer(Message message)
        {
            var timer = (Enums.Timer)message.GetUShort();

            if (IsTimerRunning(timer))
            {
                return;
            }

            var time = message.GetInt();
            var startTime = message.GetFloat();

            EventManager.CallTimerStarted(timer, time);
            var newRunningTimer = new RunningTimer()
            {
                Timer = timer,
                RemainingTime = time,
                Duration = time,
                ServerStart = startTime
            };
            _runningTimers.Add(newRunningTimer);
        }

        private void Update()
        {
            if(!IsAnyTimerActive())
            {
                return;
            }

            for(int i = _runningTimers.Count - 1; i >= 0; --i)
            {
                var timer = _runningTimers[i];
                var startThisTick = timer.RemainingTime;
                timer.RemainingTime = Mathf.CeilToInt(timer.Duration - NetworkManager.Instance.GetServerTime() + timer.ServerStart);
                if (startThisTick != timer.RemainingTime)
                {
                    EventManager.CallTimerUpdate(timer.Timer, Mathf.CeilToInt(timer.RemainingTime));
                }

                if (timer.RemainingTime <= 0)
                {
                    EventManager.CallTimerEnded(timer.Timer);
                    _runningTimers.RemoveAt(i);
                    continue;
                }

                _runningTimers[i] = timer;
            }
        }

        public bool IsTimerRunning(Enums.Timer timer)
        {
            return _runningTimers.Any(t => t.Timer == timer);
        }

        private bool IsAnyTimerActive()
        {
            return _runningTimers.Count > 0;
        }
    }
}