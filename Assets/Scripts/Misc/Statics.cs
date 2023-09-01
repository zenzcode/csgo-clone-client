using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc
{
    public static class Statics
    {
        public const string LobbyMapName = "Lobby";
        public const string MainMenuMapName = "MainMenu";
        public const string PersistentMapName = "PersistentScene";
        public const int MinStartPlayers = 2;
        public const float MaxYawPitchDelta = 1.5f;
        public const float MaxPositionDelta = 1.5f;
        public const float MinPosDelta = 0.5f;

        public static int VelocityAnimationParamter = Animator.StringToHash("Velocity");
        public static int DirectionAnimationParamter = Animator.StringToHash("Direction");
    }
}
