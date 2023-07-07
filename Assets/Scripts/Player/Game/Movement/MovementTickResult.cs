using Riptide;
using UnityEngine;

namespace Player.Game.Movement
{
    public struct MovementTickResult : IMessageSerializable
    {
        public int Tick;
        public Vector3 StartPosition;
        public Vector3 PassedEndPosition;
        public Vector3 ActualEndPosition;
        public Vector3 StartEulerAngles;
        public Vector3 PassedEndEulerAngles;
        public Vector3 ActualEndEulerAngles;
        public float DeltaTime;
        public int Input;

        public void Deserialize(Message message)
        {
            Tick = message.GetInt();
            StartPosition = message.GetVector3();
            PassedEndPosition = message.GetVector3();
            ActualEndPosition = message.GetVector3();
            StartEulerAngles = message.GetVector3();
            PassedEndEulerAngles = message.GetVector3();
            ActualEndEulerAngles = message.GetVector3();
            DeltaTime = message.GetFloat();
            Input = message.GetInt();
        }

        public void Serialize(Message message)
        {
            Debug.Log("This shouldnt be deserialized on the client.");
            throw new System.InvalidOperationException();
        }
    }
}