using UnityEngine;

public enum EquipmentType
{
    Weapon,
    Armor,
    Accessory
}

[CreateAssetMenu(menuName = "New Equipment")]
public class EquipmentInfo : ScriptableObject
{
    public string EquipmentName;
    public EquipmentType Type;
    [TextArea] public string Description;
    public Sprite Icon;

    [Header("Stat Bonus")]
    public int HealthBonus;
    public int StrBonus;
    public int InitiativeBonus;
}

// 1 kemungkinan drop: item apa + berapa persen peluangnya (0-100).
// Dipakai di EnemyInfo.PossibleDrops.
[System.Serializable]
public class EquipmentDrop
{
    public EquipmentInfo Item;
    [Range(0, 100)] public int DropChance = 100;
}
