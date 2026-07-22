using UnityEngine;

[CreateAssetMenu(menuName = "New Enemy")]
public class EnemyInfo : ScriptableObject
{
    public string EnemyName;
    public int BaseHealth;
    public int BaseStr;
    public int BaseInitiative;
    public int ExpReward = 20;
    public GameObject EnemyVisualPrefab;

    [Header("SFX")]
    public AudioClip AttackSound;
    public AudioClip HitSound;
    public AudioClip DeathSound;

    [Header("Equipment Drop")]
    public EquipmentDrop[] PossibleDrops; // item apa saja yang mungkin drop dari musuh ini + peluangnya
}
