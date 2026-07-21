using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

public class PartyManager : MonoBehaviour
{
    [SerializeField] private PartyMemberInfo[] allMembers;
    [SerializeField] private List<PartyMember> currentParty;

    [SerializeField] private PartyMemberInfo defaultPartyMember;

    private Vector3 playerPosition;
    private static GameObject instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this.gameObject;
            AddMemberToPartyByName(defaultPartyMember.MemberName);
        }
        DontDestroyOnLoad(this.gameObject);
      
    }

    public void AddMemberToPartyByName(string memberName)
    {
        for (int i = 0; i < allMembers.Length; i++)
        {
            if (allMembers[i].MemberName == memberName)
            {
                PartyMember newPartyMember = new PartyMember();
                newPartyMember.MemberName = allMembers[i].MemberName;
                newPartyMember.Level = allMembers[i].StartingLevel;
                newPartyMember.CurrHealth = allMembers[i].BaseHealth;
                newPartyMember.MaxHealth = newPartyMember.CurrHealth;
                newPartyMember.Strength = allMembers[i].BaseStr;
                newPartyMember.Initiative = allMembers[i].BaseInitiative;
                newPartyMember.MemberBattleVisualPrefab = allMembers[i].MemberBattleVisualPrefab;
                newPartyMember.MemberOverworldVisualPrefab = allMembers[i].MemberOverworldVisualPrefab;

                newPartyMember.MemberInfo = allMembers[i];
                newPartyMember.CurrExp = 0;
                newPartyMember.MaxExp = allMembers[i].BaseExpToLevel;

                currentParty.Add(newPartyMember);
            }
        }
    }

    public List<PartyMember> GetAliveParty()
    {
        List<PartyMember> aliveParty = new List<PartyMember>();
        aliveParty = currentParty;
        for (int i = 0; i < aliveParty.Count; i++)
        {
            if (aliveParty[i].CurrHealth <= 0)
            {
                aliveParty.RemoveAt(i);
            }
        }
        return aliveParty;
    }

    public List<PartyMember> GetCurrentParty()
    {
        return currentParty;
    }
    public void SaveHealth(int partyMember, int health)
    {
        currentParty[partyMember].CurrHealth = health;
    }

    public void SetPosition(Vector3 position)
    {
        playerPosition = position;
    }
    public Vector3 GetPosition()
    {
        return playerPosition;
    }

    // ---------- EXP & LEVEL UP ----------

    // Bagikan totalExp ke seluruh member party yang masih hidup, secara merata.
    // Mengembalikan daftar hasil level up (untuk ditampilkan di UI).
    public List<LevelUpResult> AddExpToParty(int totalExp)
    {
        List<LevelUpResult> results = new List<LevelUpResult>();
        List<PartyMember> aliveMembers = GetAliveParty();

        if (aliveMembers.Count <= 0 || totalExp <= 0)
        {
            return results;
        }

        int expPerMember = totalExp / aliveMembers.Count;

        for (int i = 0; i < aliveMembers.Count; i++)
        {
            int levelsGained = AddExpToMember(aliveMembers[i], expPerMember);
            if (levelsGained > 0)
            {
                LevelUpResult result = new LevelUpResult();
                result.MemberName = aliveMembers[i].MemberName;
                result.NewLevel = aliveMembers[i].Level;
                result.LevelsGained = levelsGained;
                results.Add(result);
            }
        }

        return results;
    }

    // Tambahkan EXP ke satu member dan proses level up (bisa naik lebih dari 1 level sekaligus).
    // Mengembalikan jumlah level yang didapat.
    private int AddExpToMember(PartyMember member, int exp)
    {
        member.CurrExp += exp;
        int levelsGained = 0;

        while (member.CurrExp >= member.MaxExp)
        {
            member.CurrExp -= member.MaxExp;
            member.Level++;
            levelsGained++;

            if (member.MemberInfo != null)
            {
                member.MaxHealth += member.MemberInfo.HealthPerLevel;
                member.CurrHealth = member.MaxHealth; // full heal saat level up
                member.Strength += member.MemberInfo.StrPerLevel;
                member.Initiative += member.MemberInfo.InitiativePerLevel;

                int levelsSinceStart = member.Level - member.MemberInfo.StartingLevel;
                member.MaxExp = Mathf.RoundToInt(member.MemberInfo.BaseExpToLevel *
                    Mathf.Pow(member.MemberInfo.ExpGrowthRate, levelsSinceStart));
            }
        }

        return levelsGained;
    }

    // Pulihkan seluruh party (dipanggil dari Game Over UI sebelum retry/kembali ke menu)
    public void ReviveParty()
    {
        for (int i = 0; i < currentParty.Count; i++)
        {
            currentParty[i].CurrHealth = currentParty[i].MaxHealth;
        }
    }
}

[System.Serializable]
public class PartyMember
{
    public string MemberName;
    public int Level;
    public int CurrHealth;
    public int MaxHealth;
    public int Strength;
    public int Initiative;
    public int CurrExp;
    public int MaxExp;
    public GameObject MemberBattleVisualPrefab;
    public GameObject MemberOverworldVisualPrefab;
    public PartyMemberInfo MemberInfo;
}

[System.Serializable]
public class LevelUpResult
{
    public string MemberName;
    public int NewLevel;
    public int LevelsGained;
}
