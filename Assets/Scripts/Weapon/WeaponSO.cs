using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Weapons
{
    [CreateAssetMenu(fileName = "Weapon_", menuName = "Scriptable Objects/Weapons/Weapon")]
    public class WeaponSO : ScriptableObject
    {
        public string DisplayName;
        public int StartAmmo;
        public int MagazineCapacity;
        public int ReserveAmmo;
        public AnimationCurve Recoil;
    }
}