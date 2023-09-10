using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;

namespace Weapons
{
    public class Weapon : MonoBehaviour
    {
        #region Vars
        [SerializeField] private WeaponSO WeaponSO;
        public GameObject GrabPointRight;
        public GameObject GrabPointLeft;
        public int Ammo { get; private set; }
        public int ReserveAmmo { get; private set; }
        #endregion

        private void Awake()
        {
            Ammo = WeaponSO.StartAmmo;
            ReserveAmmo = WeaponSO.ReserveAmmo;
        }

        public string DisplayName => WeaponSO.DisplayName;

        public int StartAmmo => WeaponSO.StartAmmo;

        public int MagazineCapacity => WeaponSO.MagazineCapacity;

        public AnimationCurve RecoilCurve => WeaponSO.Recoil;
    }
}