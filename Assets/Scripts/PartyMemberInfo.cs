using UnityEngine;

[CreateAssetMenu(menuName = "New Party Member")]
public class PartyMemberInfo : ScriptableObject
{
    public string MemberName;
    public int StartingLevel;
    public int BaseHealth;
    public int BaseStr;
    public int BaseInitiative;
    public GameObject MemberBattleVisualPrefab;    // what will be displayed in battle scene
    public GameObject MemberOverworldVisualPrefab; // what will be displayed in the overworld scene

    [Header("EXP & Level Up")]
    public int BaseExpToLevel = 100;      // EXP needed to go from StartingLevel to the next level
    public float ExpGrowthRate = 1.15f;   // multiplier applied per level (curva EXP requirement)
    public int HealthPerLevel = 10;       // MaxHealth gained per level up
    public int StrPerLevel = 2;           // Strength gained per level up
    public int InitiativePerLevel = 1;    // Initiative gained per level up

    [Header("SFX")]
    public AudioClip AttackSound;
    public AudioClip HitSound;
    public AudioClip DeathSound;
}
