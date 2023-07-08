using Riptide;
using UnityEngine;

namespace Player.Game.Movement
{
    public struct MovementTickResult : IMessageSerializable
    {
        public uint Tick;
        public ushort ClientId;
        public Vector3 StartPosition;
        public Vector3 PassedEndPosition;
        public Vector3 ActualEndPosition;
        public float StartYaw;
        public float PassedEndYaw;
        public float ActualEndYaw;
        public float StartPitch;
        public float PassedEndPitch;
        public float ActualEndPitch;
        public float DeltaTime;
        public float Sensitivity;
        public int Input;

        public void Deserialize(Message message)
        {
            ClientId = message.GetUShort();
            Tick = message.GetUInt();
            StartPosition = message.GetVector3();
            PassedEndPosition = message.GetVector3();
            ActualEndPosition = message.GetVector3();
            StartYaw = message.GetFloat();
            PassedEndYaw = message.GetFloat();
            ActualEndYaw = message.GetFloat();
            StartPitch = message.GetFloat();
            PassedEndPitch = message.GetFloat();
            ActualEndPitch = message.GetFloat();
            DeltaTime = message.GetFloat();
            Sensitivity = message.GetFloat();
            Input = message.GetInt();
        }

        public void Serialize(Message message)
        {
            Debug.Log("This shouldnt be deserialized on the client.");
            throw new System.InvalidOperationException();
        }
    }
}