using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Game.Movement
{
    public struct MovementTick
    {
        public ushort ClientId;
        public uint Tick;
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        public int Input;
        public Vector3 EulerAngles;
        public Vector3 EndEulerAngles;
        public float MouseDeltaX;
        public float MouseDeltaY;
        public float DeltaTime;
    }
}