using Riptide;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Game.Movement
{
    public struct MovementTick : IMessageSerializable
    {
        public ushort ClientId;
        public uint Tick;
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        public int Input;
        public float Yaw;
        public float Pitch;
        public float EndYaw;
        public float EndPitch;
        public float MouseDeltaX;
        public float MouseDeltaY;
        public float DeltaTime;
        public float Sensitivity;
        public bool CrouchDown;
        public bool SlowWalkDown;

        public void Deserialize(Message message)
        {
            Debug.Log("There should be no need to deserialize this.");
            throw new System.InvalidOperationException();
        }

        public void Serialize(Message message)
        {
            message.AddUShort(ClientId);
            message.AddUInt(Tick);
            message.AddVector3(StartPosition);
            message.AddVector3(EndPosition);
            message.AddInt(Input);
            message.AddFloat(Yaw);
            message.AddFloat(EndYaw);
            message.AddFloat(Pitch);
            message.AddFloat(EndPitch);
            message.AddFloat(MouseDeltaX);
            message.AddFloat(MouseDeltaY);
            message.AddFloat(DeltaTime);
            message.AddFloat(Sensitivity);
            message.AddBool(CrouchDown);
            message.AddBool(SlowWalkDown);
        }
    }
}