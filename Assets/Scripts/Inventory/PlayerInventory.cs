using System.Collections;
using System.Collections.Generic;
using Weapons;
using Enums;

namespace PlayerInventory
{
    [System.Serializable]
    public class PlayerInventory
    {
        public Dictionary<InventorySlot, Weapon> PlayerItems = new Dictionary<InventorySlot, Weapon>();

        public Weapon GetWeaponAtSlot(InventorySlot inventorySlot)
        {
            if(!PlayerItems.ContainsKey(inventorySlot))
            {
                return null;
            }

            return PlayerItems[inventorySlot];
        }
    }
}