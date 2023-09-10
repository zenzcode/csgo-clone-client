using Helper;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class InventoryManager : SingletonMonoBehavior<InventoryManager>
    {
        private Dictionary<ushort, PlayerInventory.PlayerInventory> _playerItems;
        [SerializeField] private Weapons.Weapon activeWeaponTest;

        protected override void Awake()
        {
            base.Awake();
            _playerItems = new Dictionary<ushort, PlayerInventory.PlayerInventory>();
        }

        public Weapons.Weapon GetActiveWeapon(ushort clientId)
        {
            return activeWeaponTest;
        }
    }

}