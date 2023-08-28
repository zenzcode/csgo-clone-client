using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helpe {
    public static class VectorExtensions
    {
        public static bool IsNearlyZero(this Vector2 vector)
        {
            return Mathf.Approximately(vector.magnitude, 0);
        }
    }
}