using UnityEngine;

public enum EquipementType
{
    Consumable = 0,
    Weapon = 1,
    Armor = 2,
    Passive = 3
}

public enum WeaponType
{
    Sword = 0,
    Axe,
    Staff = 1,
    Ranged = 2,
    Throwable = 3
}

public enum ArmorType
{
    CoreArmor = 0,
    Helmet = 1,
    Shield = 2
}

public enum PassiveObjectName
{
    Toolbag = 0
}

[System.Serializable]
public struct Equipement
{
    public EquipementType type;
    public int cost;
}
