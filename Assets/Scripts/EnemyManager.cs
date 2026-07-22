using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private EnemyInfo[] allEnemies;
    [SerializeField] private List<Enemy> currentEnemy;

    private static GameObject instance;
    private const float LEVEL_MODIFIER = 0.5f;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this.gameObject;
        }
        DontDestroyOnLoad(gameObject);
    }

    public void GenerateEnemiesByEncounter(Encounter[] encounters, int maxNumEnemies)
    {
        currentEnemy.Clear();
        int numEnemies = UnityEngine.Random.Range(1, maxNumEnemies + 1);
        for (int i = 0; i < numEnemies; i++)
        {
            Encounter tempEncounter = encounters[UnityEngine.Random.Range(0, encounters.Length)];
            int level = UnityEngine.Random.Range(tempEncounter.LevelMin, tempEncounter.LevelMax + 1);
            GenerateEnemyByName(tempEncounter.Enemy.EnemyName, level);
        }
    }
    private void GenerateEnemyByName(string enemyName, int level)
    {
        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (enemyName == allEnemies[i].EnemyName)
            {
                Enemy newEnemy = new Enemy();
                newEnemy.EnemyName = allEnemies[i].EnemyName;
                newEnemy.Level = level;
                float levelModifier = (LEVEL_MODIFIER * newEnemy.Level);

                newEnemy.MaxHealth = Mathf.RoundToInt(allEnemies[i].BaseHealth + (allEnemies[i].BaseHealth * levelModifier));
                newEnemy.CurrHealth = newEnemy.MaxHealth;
                newEnemy.Strength = Mathf.RoundToInt(allEnemies[i].BaseStr + (allEnemies[i].BaseStr * levelModifier));
                newEnemy.Initiative = Mathf.RoundToInt(allEnemies[i].BaseInitiative + (allEnemies[i].BaseInitiative * levelModifier));
                newEnemy.ExpReward = Mathf.RoundToInt(allEnemies[i].ExpReward + (allEnemies[i].ExpReward * levelModifier));
                newEnemy.EnemyVisualPrefab = allEnemies[i].EnemyVisualPrefab;

                // SFX
                newEnemy.AttackSound = allEnemies[i].AttackSound;
                newEnemy.HitSound = allEnemies[i].HitSound;
                newEnemy.DeathSound = allEnemies[i].DeathSound;

                // Equipment drop
                newEnemy.PossibleDrops = allEnemies[i].PossibleDrops;

                currentEnemy.Add(newEnemy);
            }
        }
    }

    public List<Enemy> GetCurrentEnemies()
    {
        return currentEnemy;
    }
}

[System.Serializable]
public class Enemy
{
    public string EnemyName;
    public int Level;
    public int CurrHealth;
    public int MaxHealth;
    public int Strength;
    public int Initiative;
    public int ExpReward;
    public GameObject EnemyVisualPrefab;

    // SFX
    public AudioClip AttackSound;
    public AudioClip HitSound;
    public AudioClip DeathSound;

    // Equipment drop
    public EquipmentDrop[] PossibleDrops;
}
