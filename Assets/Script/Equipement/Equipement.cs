using UnityEngine;

public enum EquipementType
{
    Consumable = 0,
    Weapon = 1,
    Armor = 2,
    Passive = 3
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
