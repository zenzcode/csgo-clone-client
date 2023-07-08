using Enums;
using Player.Game.Movement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventManager
{
    public static Action ConnectFailed;

    public static void CallConnectFailed()
    {
        ConnectFailed?.Invoke();
    }
    
    public static Action ConnectSuccess;

    public static void CallConnectSuccess()
    {
        ConnectSuccess?.Invoke();
    }

    public static Action<ushort> TeamChanged;

    public static void CallTeamChanged(ushort playerId)
    {
        TeamChanged?.Invoke(playerId);
    }

    public static Action<ushort> ClientDisconnected;
    public static void CallClientDisconnected(ushort playerId)
    {
        ClientDisconnected?.Invoke(playerId);
    }

    public static Action LocalPlayerReceived;

    public static void CallLocalPlayerReceived()
    {
        LocalPlayerReceived?.Invoke();
    }

    public static Action<ushort> LeaderChanged;

    public static void CallLeaderChanged(ushort newLeaderId)
    {
        LeaderChanged?.Invoke(newLeaderId);
    }

    public static Action<ushort, float> RttUpdated;

    public static void CallRttUpdated(ushort playerId, float rtt)
    {
        RttUpdated?.Invoke(playerId, rtt);
    }

    public static Action LocalPlayerDisconnect;

    public static void CallLocalPlayerDisconnect()
    {
        LocalPlayerDisconnect?.Invoke();
    }

    public static Action<Timer, int> TimerStarted;

    public static void CallTimerStarted(Timer timer, int startTime)
    {
        TimerStarted?.Invoke(timer, startTime);
    }

    public static Action<Timer, int> TimerUpdate;

    public static void CallTimerUpdate(Timer timer, int remainingTime)
    {
        TimerUpdate?.Invoke(timer, remainingTime);
    }

    public static Action<Timer> TimerStopped;
    
    public static void CallTimerStopped(Timer timer)
    {
        TimerStopped?.Invoke(timer);
    }

    public static Action<Timer> TimerEnded;

    public static void CallTimerEnded(Timer timer)
    {
        TimerEnded?.Invoke(timer);
    }

    public static Action<MovementTickResult> MovementTickResultReceived;

    public static void CallMovementTickResultReceived(MovementTickResult tickResult)
    {
        MovementTickResultReceived?.Invoke(tickResult);
    }
}
