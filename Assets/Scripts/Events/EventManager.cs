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
}
