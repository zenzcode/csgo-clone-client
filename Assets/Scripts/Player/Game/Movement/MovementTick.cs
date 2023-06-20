using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Game.Movement
{
    public struct MovementTick
    {
        public ushort ClientId;
        public int Tick;
        public Transform StartPosition;
        public int Input;
        public Vector3 EulerAngles;
        public float MouseDeltaX;
        public float MouseDeltaY;
    }
}