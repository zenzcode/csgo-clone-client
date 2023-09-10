using Enums;
using Helper;
using Misc;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Managers
{
    public class IngameUIManager : SingletonMonoBehavior<IngameUIManager>
    {
        [SerializeField] private GameObject _clockHolder;
        [SerializeField] private TMP_Text _clockText;

        protected override void Awake()
        {
            base.Awake();
            EventManager.TimerStarted += EventManager_TimerStarted;
            EventManager.TimerUpdate += EventManager_TimerUpdate;
            EventManager.TimerEnded += EventManager_TimerEnded;
        }

        private void EventManager_TimerStarted(Timer timer, int startTime)
        {
            if(timer == Timer.WarmupTimer)
            {
                _clockText.color = new Color(255, 0, 0);
                _clockText.SetText(Statics.GetTimeInFormat(startTime));
            }
        }

        private void EventManager_TimerUpdate(Timer timer, int remainingTime)
        {
            if (timer == Timer.WarmupTimer)
            {
                _clockText.SetText(Statics.GetTimeInFormat(remainingTime));
            }
        }

        private void EventManager_TimerEnded(Timer timer)
        {
        }
    }
}
