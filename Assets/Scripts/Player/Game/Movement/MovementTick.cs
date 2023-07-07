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
        public Vector3 EulerAngles;
        public Vector3 EndEulerAngles;
        public float MouseDeltaX;
        public float MouseDeltaY;
        public float DeltaTime;

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
            message.AddVector3(EulerAngles);
            message.AddVector3(EndEulerAngles);
            message.AddFloat(MouseDeltaX);
            message.AddFloat(MouseDeltaY);
            message.AddFloat(DeltaTime);
        }
    }
}