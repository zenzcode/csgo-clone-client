using Enums;
using Player.Game.Movement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public static class EventManager
{
    public static event Action ConnectFailed;

    public static void CallConnectFailed()
    {
        ConnectFailed?.Invoke();
    }
    
    public static event Action ConnectSuccess;

    public static void CallConnectSuccess()
    {
        ConnectSuccess?.Invoke();
    }

    public static event Action<ushort> TeamChanged;

    public static void CallTeamChanged(ushort playerId)
    {
        TeamChanged?.Invoke(playerId);
    }

    public static event Action<ushort> ClientDisconnected;
    public static void CallClientDisconnected(ushort playerId)
    {
        ClientDisconnected?.Invoke(playerId);
    }

    public static event Action LocalPlayerReceived;

    public static void CallLocalPlayerReceived()
    {
        LocalPlayerReceived?.Invoke();
    }

    public static event Action<ushort> LeaderChanged;

    public static void CallLeaderChanged(ushort newLeaderId)
    {
        LeaderChanged?.Invoke(newLeaderId);
    }

    public static event Action<ushort, float> RttUpdated;

    public static void CallRttUpdated(ushort playerId, float rtt)
    {
        RttUpdated?.Invoke(playerId, rtt);
    }

    public static event Action LocalPlayerDisconnect;

    public static void CallLocalPlayerDisconnect()
    {
        LocalPlayerDisconnect?.Invoke();
    }

    public static event Action<Timer, int> TimerStarted;

    public static void CallTimerStarted(Timer timer, int startTime)
    {
        TimerStarted?.Invoke(timer, startTime);
    }

    public static event Action<Timer, int> TimerUpdate;

    public static void CallTimerUpdate(Timer timer, int remainingTime)
    {
        TimerUpdate?.Invoke(timer, remainingTime);
    }

    public static event Action<Timer> TimerStopped;
    
    public static void CallTimerStopped(Timer timer)
    {
        TimerStopped?.Invoke(timer);
    }

    public static event Action<Timer> TimerEnded;

    public static void CallTimerEnded(Timer timer)
    {
        TimerEnded?.Invoke(timer);
    }

    public static event Action<MovementTickResult> MovementTickResultReceived;

    public static void CallMovementTickResultReceived(MovementTickResult tickResult)
    {
        MovementTickResultReceived?.Invoke(tickResult);
    }
}
