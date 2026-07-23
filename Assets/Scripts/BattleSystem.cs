using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BattleSystem : MonoBehaviour
{
    private enum BattleState { Start, Selection, Battle, Won, Lost, Run }

    [Header("Battle State")]
    [SerializeField] private BattleState state;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] partySpawnPoints;
    [SerializeField] private Transform[] enemySpawnPoints;

    [Header("Battlers")]
    [SerializeField] private List<BattleEntities> allBattlers = new List<BattleEntities>();
    [SerializeField] private List<BattleEntities> enemyBattlers = new List<BattleEntities>();
    [SerializeField] private List<BattleEntities> playerBattlers = new List<BattleEntities>();

    [Header("UI")]
    [SerializeField] private GameObject[] enemySelectionButtons;
    [SerializeField] private GameObject battleMenu;
    [SerializeField] private GameObject enemySelectionMenu;
    [SerializeField] private TextMeshProUGUI actionText;
    [SerializeField] private GameObject bottomTextPopUp;
    [SerializeField] private TextMeshProUGUI bottomText;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Level Up UI")]
    [SerializeField] private GameObject levelUpPopup;
    [SerializeField] private TextMeshProUGUI levelUpText;

    [Header("Battle Results UI (Drop, EXP, EXP ke level berikutnya)")]
    [SerializeField] private GameObject battleResultsPanel;
    [SerializeField] private TextMeshProUGUI dropsText;
    [SerializeField] private TextMeshProUGUI expGainedText;
    [SerializeField] private TextMeshProUGUI expToNextLevelText;
    [SerializeField] private Button continueButton; // tombol "Continue" -> kembali ke Overworld

    private PartyManager partyManager;
    private EnemyManager enemyManager;
    private int currentPlayer;
    private int totalExpEarned;
    private List<EquipmentInfo> battleDrops = new List<EquipmentInfo>(); // item yang berhasil drop selama battle ini

    private const string ACTION_MESSAGE = "'s Action:";
    private const string WIN_MESSAGE = "Your party won the battle";
    private const string LOSE_MESSAGE = "Your party has been defeated";
    private const string SUCCESSFULLY_RUN_MESSAGE = "You successfully ran away!";
    private const string UNSUCCESSFULLY_RUN_MESSAGE = "You failed to run away!";
    private const string EXP_GAINED_MESSAGE = "Party gained {0} EXP!";
    private const string NO_DROPS_MESSAGE = "Tidak ada item yang didapat.";
    private const int TURN_DURATION = 2;
    private const int RUN_CHANCE = 50;
    private const string OVERWORLD_SCENE = "OverworldScene";

    // Start is called before the first frame update
    void Start()
    {
        partyManager = GameObject.FindFirstObjectByType<PartyManager>();
        enemyManager = GameObject.FindFirstObjectByType<EnemyManager>();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelUpPopup != null) levelUpPopup.SetActive(false);
        if (battleResultsPanel != null) battleResultsPanel.SetActive(false);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueFromResults);

        CreatePartyEntities();
        CreateEnemyEntities();
        ShowBattleMenu();
        DetermineBattleOrder();

    }

    private IEnumerator BattleRoutine()
    {
        enemySelectionMenu.SetActive(false); // enemy selection menu disabled
        state = BattleState.Battle; // change our state to the battle state
        bottomTextPopUp.SetActive(true); //enable our bottom text

        //loop through all our battlers
        //-> do their approriate action

        for (int i = 0; i < allBattlers.Count; i++)
        {
            if (state == BattleState.Battle && allBattlers[i].CurrHealth > 0)
            {
                switch (allBattlers[i].BattleAction)
                {
                    case BattleEntities.Action.Attack:
                        // do the attack
                        yield return StartCoroutine(AttackRoutine(i));
                        break;
                    case BattleEntities.Action.Run:
                        yield return StartCoroutine(RunRoutine());
                        break;
                    default:
                        Debug.Log("Error - incorrect battle action");
                        break;
                }
            }
        }

        RemoveDeadBattlers(); // remove any dead battlers from the list

        if (state == BattleState.Battle)
        {
            bottomTextPopUp.SetActive(false);
            currentPlayer = 0;
            ShowBattleMenu();

        }

        yield return null;
        // if we havent won or lost, repeat the loop by opening the battle menu
    }

    private IEnumerator AttackRoutine(int i)
    {

        // players turn
        if (allBattlers[i].IsPlayer == true)
        {
            BattleEntities currAttacker = allBattlers[i];
            // Guard: Cek target saat ini masih hidup. Kalau sudah mati, retarget random.
            // Edge case: jika hanya attacker sendiri yang tersisa (karena enemy semua baru saja mati
            // di turn yang sama), Target CurrHealth <= 0 → GetRandomEnemy() return -1.
            bool needRetarget = currAttacker.Target < 0 || currAttacker.Target >= allBattlers.Count
                || allBattlers[currAttacker.Target].CurrHealth <= 0;
            if (needRetarget)
            {
                currAttacker.SetTarget(GetRandomEnemy());
            }
            // Kalau masih -1 (no enemy), skip turn — battle state seharusnya Won.
            if (currAttacker.Target < 0) yield break;
            BattleEntities currTarget = allBattlers[currAttacker.Target];
            AttackAction(currAttacker, currTarget); // attack selected enemy (attack action)
            yield return new WaitForSeconds(TURN_DURATION);// wait a few seconds

            // kill the enemy
            if (currTarget.CurrHealth <= 0)
            {
                bottomText.text = string.Format("{0} defeated {1}", currAttacker.Name, currTarget.Name);
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(currTarget.DeathSound); // suara musuh mati
                totalExpEarned += currTarget.ExpReward; // catat EXP dari musuh yang dikalahkan
                RollDropsForEnemy(currTarget); // cek item apa saja yang drop dari musuh ini
                yield return new WaitForSeconds(TURN_DURATION);// wait a few seconds
                enemyBattlers.Remove(currTarget);

                if (enemyBattlers.Count <= 0)
                {
                    state = BattleState.Won;
                    bottomText.text = WIN_MESSAGE;
                    yield return new WaitForSeconds(TURN_DURATION);// wait a few seconds
                    yield return StartCoroutine(AwardExpRoutine());
                    ShowBattleResults(); // tampilkan panel Drop + EXP + EXP ke level berikutnya
                }
            }
            // if no enemies remain
            // -> we won the battle
        }



        //enemies turn
        else if (i < allBattlers.Count && allBattlers[i].IsPlayer == false)
        {
            BattleEntities currAttacker = allBattlers[i];
            currAttacker.SetTarget(GetRandomPartyMember());// get random party member (target)
            // Guard: kalau tidak ada party member hidup → -1; skip attack.
            if (currAttacker.Target < 0) yield break;
            BattleEntities currTarget = allBattlers[currAttacker.Target];

            AttackAction(currAttacker, currTarget);// attack selected party member (attack action)
            yield return new WaitForSeconds(TURN_DURATION);// wait a few seconds

            if (currTarget.CurrHealth <= 0)
            {
                // kill the party member
                bottomText.text = string.Format("{0} defeated {1}", currAttacker.Name, currTarget.Name);
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(currTarget.DeathSound); // suara party member kalah
                yield return new WaitForSeconds(TURN_DURATION);// wait a few seconds
                playerBattlers.Remove(currTarget);

                if (playerBattlers.Count <= 0) // if no party members remain
                {
                    // -> we lost the battle
                    state = BattleState.Lost;
                    bottomText.text = LOSE_MESSAGE;
                    yield return new WaitForSeconds(TURN_DURATION);// wait a few seconds
                    ShowGameOver();
                }

            }

        }
    }

    private IEnumerator RunRoutine()
    {
        if (state == BattleState.Battle)
        {
            if (Random.Range(1, 101) >= RUN_CHANCE)
            {
                state = BattleState.Run;
                bottomText.text = SUCCESSFULLY_RUN_MESSAGE;
                allBattlers.Clear();
                yield return new WaitForSeconds(TURN_DURATION);// wait a few seconds
                SceneManager.LoadScene(OVERWORLD_SCENE);
                yield break;
            }
            else
            {
                bottomText.text = UNSUCCESSFULLY_RUN_MESSAGE;
                yield return new WaitForSeconds(TURN_DURATION);// wait a few seconds
            }
        }

    }

    // Menampilkan panel Game Over, menghentikan flow battle
    private void ShowGameOver()
    {
        bottomTextPopUp.SetActive(false);
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.Log("Game Over - assign gameOverPanel di Inspector untuk menampilkan UI");
        }
    }

    // Membagikan EXP ke party dan menampilkan popup jika ada yang naik level
    private IEnumerator AwardExpRoutine()
    {
        if (totalExpEarned <= 0) yield break;

        bottomTextPopUp.SetActive(true);
        bottomText.text = string.Format(EXP_GAINED_MESSAGE, totalExpEarned);
        yield return new WaitForSeconds(TURN_DURATION);

        List<LevelUpResult> levelUps = partyManager.AddExpToParty(totalExpEarned);

        for (int i = 0; i < levelUps.Count; i++)
        {
            if (levelUpPopup != null && levelUpText != null)
            {
                levelUpPopup.SetActive(true);
                levelUpText.text = string.Format("{0} reached Level {1}!", levelUps[i].MemberName, levelUps[i].NewLevel);
            }
            yield return new WaitForSeconds(TURN_DURATION);
        }

        if (levelUpPopup != null)
        {
            levelUpPopup.SetActive(false);
        }
    }

    // Menghitung item apa saja yang drop dari 1 musuh yang baru dikalahkan (berdasarkan DropChance masing-masing)
    private void RollDropsForEnemy(BattleEntities enemy)
    {
        if (enemy.PossibleDrops == null) return;

        for (int i = 0; i < enemy.PossibleDrops.Length; i++)
        {
            EquipmentDrop drop = enemy.PossibleDrops[i];
            if (drop.Item == null) continue;

            int roll = Random.Range(1, 101); // 1 - 100
            if (roll <= drop.DropChance)
            {
                battleDrops.Add(drop.Item);
            }
        }
    }

    // Menampilkan panel hasil battle: item yang didapat, total EXP, dan EXP yang masih dibutuhkan tiap member untuk level up
    private void ShowBattleResults()
    {
        bottomTextPopUp.SetActive(false);

        // masukkan semua item yang drop ke inventory party
        for (int i = 0; i < battleDrops.Count; i++)
        {
            partyManager.AddEquipmentToInventory(battleDrops[i]);
        }

        if (battleResultsPanel == null)
        {
            // kalau UI belum di-setup, langsung lanjut ke overworld seperti sebelumnya
            SceneManager.LoadScene(OVERWORLD_SCENE);
            return;
        }

        if (dropsText != null)
        {
            if (battleDrops.Count > 0)
            {
                string dropsList = "";
                for (int i = 0; i < battleDrops.Count; i++)
                {
                    dropsList += string.Format("- {0} ({1})\n", battleDrops[i].EquipmentName, battleDrops[i].Type);
                }
                dropsText.text = dropsList;
            }
            else
            {
                dropsText.text = NO_DROPS_MESSAGE;
            }
        }

        if (expGainedText != null)
        {
            expGainedText.text = string.Format(EXP_GAINED_MESSAGE, totalExpEarned);
        }

        if (expToNextLevelText != null)
        {
            List<PartyMember> currentParty = partyManager.GetCurrentParty();
            string expLines = "";
            for (int i = 0; i < currentParty.Count; i++)
            {
                PartyMember member = currentParty[i];
                int expNeeded = Mathf.Max(0, member.MaxExp - member.CurrExp);
                expLines += string.Format("{0} (Lv.{1}): butuh {2} EXP lagi\n", member.MemberName, member.Level, expNeeded);
            }
            expToNextLevelText.text = expLines;
        }

        battleResultsPanel.SetActive(true);
    }

    // Dipanggil dari tombol "Continue" di battleResultsPanel
    public void OnContinueFromResults()
    {
        if (battleResultsPanel != null) battleResultsPanel.SetActive(false);
        SceneManager.LoadScene(OVERWORLD_SCENE);
    }

    private void RemoveDeadBattlers()
    {
        // Iterate backward so RemoveAt doesn't skip adjacent elements.
        for (int i = allBattlers.Count - 1; i >= 0; i--)
        {
            if (allBattlers[i].CurrHealth <= 0)
            {
                allBattlers.RemoveAt(i);
            }
        }
    }

    private void CreatePartyEntities()
    {
        List<PartyMember> currentParty = new List<PartyMember>();
        currentParty = partyManager.GetAliveParty();

        for (int i = 0; i < currentParty.Count; i++)
        {
            BattleEntities tempEntity = new BattleEntities();

            // pakai stat TOTAL (base + bonus equipment) supaya equipment berpengaruh di battle
            int totalMaxHealth = currentParty[i].GetTotalMaxHealth();
            int clampedCurrHealth = Mathf.Min(currentParty[i].CurrHealth, totalMaxHealth);

            tempEntity.SetEntityValues(currentParty[i].MemberName, clampedCurrHealth, totalMaxHealth,
            currentParty[i].GetTotalInitiative(), currentParty[i].GetTotalStrength(), currentParty[i].Level, true);

            // SFX
            tempEntity.AttackSound = currentParty[i].AttackSound;
            tempEntity.HitSound = currentParty[i].HitSound;
            tempEntity.DeathSound = currentParty[i].DeathSound;

            BattleVisuals tempBattleVisuals = Instantiate(currentParty[i].MemberBattleVisualPrefab,
            partySpawnPoints[i].position, Quaternion.identity).GetComponent<BattleVisuals>();
            tempBattleVisuals.SetStartingValues(clampedCurrHealth, totalMaxHealth, currentParty[i].Level);
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
            tempEntity.ExpReward = currentEnemies[i].ExpReward;
            tempEntity.PossibleDrops = currentEnemies[i].PossibleDrops; // bawa data drop untuk dipakai saat musuh ini mati

            // SFX
            tempEntity.AttackSound = currentEnemies[i].AttackSound;
            tempEntity.HitSound = currentEnemies[i].HitSound;
            tempEntity.DeathSound = currentEnemies[i].DeathSound;

            BattleVisuals tempBattleVisuals = Instantiate(currentEnemies[i].EnemyVisualPrefab,
            enemySpawnPoints[i].position, Quaternion.identity).GetComponent<BattleVisuals>();
            tempBattleVisuals.SetStartingValues(currentEnemies[i].MaxHealth, currentEnemies[i].MaxHealth, currentEnemies[i].Level);
            tempEntity.BattleVisuals = tempBattleVisuals;

            allBattlers.Add(tempEntity);
            enemyBattlers.Add(tempEntity);
        }

    }

    public void ShowBattleMenu()
    {
        actionText.text = playerBattlers[currentPlayer].Name + ACTION_MESSAGE;
        battleMenu.SetActive(true);

    }

    public void ShowEnemySelectionMenu()
    {
        battleMenu.SetActive(false);
        SetEnemySelectionButtons();
        enemySelectionMenu.SetActive(true);
    }

    private void SetEnemySelectionButtons()
    {
        //disable all of our buttons
        for (int i = 0; i < enemySelectionButtons.Length; i++)
        {
            enemySelectionButtons[i].SetActive(false);
        }

        for (int j = 0; j < enemyBattlers.Count; j++)
        {
            enemySelectionButtons[j].SetActive(true);
            enemySelectionButtons[j].GetComponentInChildren<TextMeshProUGUI>().text = enemyBattlers[j].Name;
        }
        //enable buttons for each enemy
        // change the buttons text 
    }

    public void SelectEnemy(int currentEnemy)
    {
        // setting the current members target
        BattleEntities currentPlayerEntity = playerBattlers[currentPlayer];
        currentPlayerEntity.SetTarget(allBattlers.IndexOf(enemyBattlers[currentEnemy]));

        //tell the battle system this member intends to attack
        currentPlayerEntity.BattleAction = BattleEntities.Action.Attack;
        // increment through our party members
        currentPlayer++;

        if (currentPlayer >= playerBattlers.Count) //if all players have selected an action
        {
            //Start The Battle
            StartCoroutine(BattleRoutine());

        }
        else
        {
            enemySelectionMenu.SetActive(false);  // show the battle menu for the next player
            ShowBattleMenu();
        }


    }

    private void AttackAction(BattleEntities currAttacker, BattleEntities currTarget)
    {
        int damage = currAttacker.Strength; //get damage (can use an algorithm)
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(currAttacker.AttackSound); // suara serang
        if (currAttacker.BattleVisuals != null)
            currAttacker.BattleVisuals.PlayAttackAnimation(); // play the attack animation
        currTarget.CurrHealth -= damage; // dealing the damage
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(currTarget.HitSound); // suara kena hit
        if (currTarget.BattleVisuals != null)
            currTarget.BattleVisuals.PlayHitAnimation(); // play their hit anim
        currTarget.UpdateUI(); // update the UI
        bottomText.text = string.Format("{0} attacks {1} for {2} damage", currAttacker.Name, currTarget.Name, damage);
        SaveHealth();
    }


    private int GetRandomPartyMember()
    {
        List<int> partyMembers = new List<int>(); // daftar indeks party member yang masih hidup
        for (int i = 0; i < allBattlers.Count; i++)
        {
            if (allBattlers[i].IsPlayer == true && allBattlers[i].CurrHealth > 0)
            {
                partyMembers.Add(i);
            }
        }
        // Sentinel: list kosong → -1; caller AttackRoutine harus guard negative.
        if (partyMembers.Count == 0) return -1;
        return partyMembers[Random.Range(0, partyMembers.Count)];
    }

    private int GetRandomEnemy()
    {
        List<int> enemies = new List<int>();
        for (int i = 0; i < allBattlers.Count; i++)
        {
            if (allBattlers[i].IsPlayer == false && allBattlers[i].CurrHealth > 0)
            {
                enemies.Add(i);
            }
        }
        // Sentinel: list kosong → -1; caller AttackRoutine harus guard negative.
        if (enemies.Count == 0) return -1;
        return enemies[Random.Range(0, enemies.Count)];
    }

    private void SaveHealth()
    {
        for (int i = 0; i < playerBattlers.Count; i++)
        {
            partyManager.SaveHealth(i, playerBattlers[i].CurrHealth);
        }
    }

    private void DetermineBattleOrder()
    {
        allBattlers.Sort((bi1, bi2) => -bi1.Initiative.CompareTo(bi2.Initiative)); // sorts list by initiative in ascending order.
    }

    public void SelectRunAction()
    {
        state = BattleState.Selection;
        BattleEntities currentPlayerEntity = playerBattlers[currentPlayer];
        currentPlayerEntity.BattleAction = BattleEntities.Action.Run;

        battleMenu.SetActive(false);
        currentPlayer++;
        if (currentPlayer >= playerBattlers.Count) //if all players have selected an action
        {
            //Start The Battle
            StartCoroutine(BattleRoutine());

        }
        else
        {
            enemySelectionMenu.SetActive(false);  // show the battle menu for the next player
            ShowBattleMenu();
        }
    }


}

[System.Serializable]
public class BattleEntities
{
    public enum Action { Attack, Run }
    public Action BattleAction;

    public string Name;
    public int CurrHealth;
    public int MaxHealth;
    public int Initiative;
    public int Strength;
    public int Level;
    public bool IsPlayer;
    public int ExpReward;
    public BattleVisuals BattleVisuals;
    public int Target;
    public EquipmentDrop[] PossibleDrops; // hanya terisi untuk musuh, dipakai saat musuh ini mati

    // SFX
    public AudioClip AttackSound;
    public AudioClip HitSound;
    public AudioClip DeathSound;

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

    public void SetTarget(int target)
    {
        Target = target;
    }

    public void UpdateUI()
    {
        if (BattleVisuals != null)
            BattleVisuals.ChangeHealth(CurrHealth);
    }

}
