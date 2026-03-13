using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] partySpawnPoints;
    [SerializeField] private Transform[] enemySpawnPoints;

    [Header("Battlers")]
    [SerializeField] private List<BattleEntities> allBattlers = new List<BattleEntities>();
    [SerializeField] private List<BattleEntities> enemyBattlers = new List<BattleEntities>();
    [SerializeField] private List<BattleEntities> playerBattlers = new List<BattleEntities>();

    private PartyManager partyManager;
    private EnemyManager enemyManager;

    // Start is called before the first frame update
    void Start()
    {
        partyManager = GameObject.FindFirstObjectByType<PartyManager>();
        enemyManager = GameObject.FindFirstObjectByType<EnemyManager>();

        CreatePartyEntities();
        CreateEnemyEntities();
    }

    private void CreatePartyEntities()
    {
        List<PartyMember> currentParty = new List<PartyMember>();
        currentParty = partyManager.GetCurrentParty();

        for (int i = 0; i < currentParty.Count; i++)
        {
            BattleEntities tempEntity = new BattleEntities();

            tempEntity.SetEntityValues(currentParty[i].MemberName, currentParty[i].CurrHealth, currentParty[i].MaxHealth,
            currentParty[i].Initiative, currentParty[i].Strength, currentParty[i].Level, true);

            BattleVisuals tempBattleVisuals = Instantiate(currentParty[i].MemberBattleVisualPrefab,
            partySpawnPoints[i].position, Quaternion.identity).GetComponent<BattleVisuals>();
            tempBattleVisuals.SetStartingValues(currentParty[i].MaxHealth, currentParty[i].MaxHealth, currentParty[i].Level);
            tempEntity.BattleVisuals = tempBattleVisuals;

            allBattlers.Add(tempEntity);
            playerBattlers.Add(tempEntity);
        }


    }

    private void CreateEnemyEntities()
    {
        List<Enemy> currentEnemies = new List<Enemy>();
        currentEnemies = enemyManager.GetCurrentEnemies();

        for (int i = 0; i < currentEnemies.Count; i++)
        {
            BattleEntities tempEntity = new BattleEntities();

            tempEntity.SetEntityValues(currentEnemies[i].EnemyName, currentEnemies[i].CurrHealth, currentEnemies[i].MaxHealth,
            currentEnemies[i].Initiative, currentEnemies[i].Strength, currentEnemies[i].Level, false);

            BattleVisuals tempBattleVisuals = Instantiate(currentEnemies[i].EnemyVisualPrefab,
            enemySpawnPoints[i].position, Quaternion.identity).GetComponent<BattleVisuals>();
            tempBattleVisuals.SetStartingValues(currentEnemies[i].MaxHealth, currentEnemies[i].MaxHealth, currentEnemies[i].Level);
            tempEntity.BattleVisuals = tempBattleVisuals;

            allBattlers.Add(tempEntity);
            enemyBattlers.Add(tempEntity);
        }

    }

}

[System.Serializable]
public class BattleEntities
{
    public string Name;
    public int CurrHealth;
    public int MaxHealth;
    public int Initiative;
    public int Strength;
    public int Level;
    public bool IsPlayer;
    public BattleVisuals BattleVisuals;

    public void SetEntityValues(string name, int currHealth, int maxHealth, int initiative, int strength, int level, bool isPlayer)
    {
        Name = name;
        CurrHealth = currHealth;
        MaxHealth = maxHealth;
        Initiative = initiative;
        Strength = strength;
        Level = level;
        IsPlayer = isPlayer;
    }

}
