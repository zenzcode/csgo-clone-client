using System.Collections;
using System.Collections.Generic;
using Enums;
using UnityEngine;

namespace Player
{
    public class Player : MonoBehaviour
    {
        [HideInInspector] public ushort PlayerId { get; set; }
        [HideInInspector] public string Username { get; set; }
        [HideInInspector] public Team Team { get; set; }
        [HideInInspector] public bool IsLocal { get; set; }
        [HideInInspector] public bool IsLeader { get; set; }
        [HideInInspector] public float LastKnownRtt { get; set; } = 1;
    }
}