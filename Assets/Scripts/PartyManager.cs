using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

public class PartyManager : MonoBehaviour
{
    public const int PARTY_LIMIT = 3; // maksimal character di party aktif (yang ikut battle)

    [SerializeField] private PartyMemberInfo[] allMembers;
    [SerializeField] private List<PartyMember> currentParty;   // party AKTIF, max PARTY_LIMIT
    [SerializeField] private List<PartyMember> reserveParty;   // character joinable yang belum masuk party aktif

    [SerializeField] private PartyMemberInfo defaultPartyMember;

    [Header("Equipment")]
    [SerializeField] private List<EquipmentInfo> inventory = new List<EquipmentInfo>(); // equipment yang dimiliki tapi belum dipakai

    private Vector3 playerPosition;
    private static GameObject instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this.gameObject;
        DontDestroyOnLoad(this.gameObject);

        if (defaultPartyMember != null)
            AddMemberToPartyByName(defaultPartyMember.MemberName);
    }

    // Menambahkan member baru (misal saat character joinable ditemukan di overworld).
    // Kalau party aktif masih ada slot kosong (< PARTY_LIMIT), otomatis masuk party aktif.
    // Kalau sudah penuh, masuk ke reserveParty (bisa ditukar manual lewat menu Manage Party).
    // Return true jika berhasil menemukan dan menambahkan member.
    public bool AddMemberToPartyByName(string memberName)
    {
        if (string.IsNullOrEmpty(memberName) || allMembers == null) return false;

        for (int i = 0; i < allMembers.Length; i++)
        {
            if (allMembers[i] == null) continue;
            if (allMembers[i].MemberName == memberName)
            {
                Debug.Log("Member ditemukan dan ditambahkan: " + memberName);
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

                // SFX
                newPartyMember.AttackSound = allMembers[i].AttackSound;
                newPartyMember.HitSound = allMembers[i].HitSound;
                newPartyMember.DeathSound = allMembers[i].DeathSound;

                if (currentParty.Count < PARTY_LIMIT)
                {
                    currentParty.Add(newPartyMember);
                }
                else
                {
                    reserveParty.Add(newPartyMember);
                }

                return true;
            }
        }

        Debug.LogWarning("PartyManager: Member \"" + memberName + "\" tidak ditemukan di allMembers! " +
            "Pastikan ScriptableObject-nya sudah di-assign di Inspector PartyManager.");
        return false;
    }

    public List<PartyMember> GetAliveParty()
    {
        // Copy currentParty (don't alias) so removing dead members doesn't mutate the source.
        // Iterate backward to avoid RemoveAt skip when consecutive dead members exist.
        List<PartyMember> aliveParty = new List<PartyMember>(currentParty);
        for (int i = aliveParty.Count - 1; i >= 0; i--)
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

    // Semua character joinable yang TIDAK ada di party aktif (menunggu di menu Manage Party)
    public List<PartyMember> GetReserveParty()
    {
        return reserveParty;
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

    // ---------- MANAGE PARTY (SWAP) ----------

    // Menukar posisi 1 member dari party AKTIF dengan 1 member dari reserveParty.
    // Dipanggil dari PartyMenuController saat player pilih 2 karakter untuk ditukar.
    public bool SwapPartyMember(PartyMember activeMember, PartyMember reserveMember)
    {
        int activeIndex = currentParty.IndexOf(activeMember);
        int reserveIndex = reserveParty.IndexOf(reserveMember);

        if (activeIndex < 0 || reserveIndex < 0) return false;

        currentParty[activeIndex] = reserveMember;
        reserveParty[reserveIndex] = activeMember;
        return true;
    }

    // ---------- EQUIPMENT ----------

    public List<EquipmentInfo> GetInventory()
    {
        return inventory;
    }

    // Dipanggil saat player dapat/beli equipment baru (misal dari loot atau shop)
    public void AddEquipmentToInventory(EquipmentInfo item)
    {
        if (item != null) inventory.Add(item);
    }

    // Pasang equipment dari inventory ke member. Kalau slot sudah terisi,
    // item lama otomatis dikembalikan ke inventory (auto swap).
    public bool EquipItem(PartyMember member, EquipmentInfo item)
    {
        if (member == null || item == null || !inventory.Contains(item)) return false;

        EquipmentInfo previousItem = null;

        switch (item.Type)
        {
            case EquipmentType.Weapon:
                previousItem = member.EquippedWeapon;
                member.EquippedWeapon = item;
                break;
            case EquipmentType.Armor:
                previousItem = member.EquippedArmor;
                member.EquippedArmor = item;
                break;
            case EquipmentType.Accessory:
                previousItem = member.EquippedAccessory;
                member.EquippedAccessory = item;
                break;
        }

        inventory.Remove(item);
        if (previousItem != null)
        {
            inventory.Add(previousItem);
        }

        // Jaga-jaga kalau HealthBonus item baru lebih kecil dari item lama, CurrHealth tidak melebihi MaxHealth total
        member.CurrHealth = Mathf.Min(member.CurrHealth, member.GetTotalMaxHealth());

        return true;
    }

    // Lepas equipment dari slot tertentu, kembalikan ke inventory
    public void UnequipItem(PartyMember member, EquipmentType slot)
    {
        if (member == null) return;

        EquipmentInfo removedItem = null;

        switch (slot)
        {
            case EquipmentType.Weapon:
                removedItem = member.EquippedWeapon;
                member.EquippedWeapon = null;
                break;
            case EquipmentType.Armor:
                removedItem = member.EquippedArmor;
                member.EquippedArmor = null;
                break;
            case EquipmentType.Accessory:
                removedItem = member.EquippedAccessory;
                member.EquippedAccessory = null;
                break;
        }

        if (removedItem != null)
        {
            inventory.Add(removedItem);
            member.CurrHealth = Mathf.Min(member.CurrHealth, member.GetTotalMaxHealth());
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

    // SFX
    public AudioClip AttackSound;
    public AudioClip HitSound;
    public AudioClip DeathSound;

    // Equipment yang sedang dipakai (bisa null kalau slot kosong)
    public EquipmentInfo EquippedWeapon;
    public EquipmentInfo EquippedArmor;
    public EquipmentInfo EquippedAccessory;

    // Stat total = base stat + bonus dari semua equipment yang terpasang.
    // Pakai method ini di battle/UI, jangan pakai MaxHealth/Strength/Initiative langsung
    // kalau equipment sudah aktif di game kamu.
    public int GetTotalMaxHealth()
    {
        int bonus = 0;
        if (EquippedWeapon != null) bonus += EquippedWeapon.HealthBonus;
        if (EquippedArmor != null) bonus += EquippedArmor.HealthBonus;
        if (EquippedAccessory != null) bonus += EquippedAccessory.HealthBonus;
        return MaxHealth + bonus;
    }

    public int GetTotalStrength()
    {
        int bonus = 0;
        if (EquippedWeapon != null) bonus += EquippedWeapon.StrBonus;
        if (EquippedArmor != null) bonus += EquippedArmor.StrBonus;
        if (EquippedAccessory != null) bonus += EquippedAccessory.StrBonus;
        return Strength + bonus;
    }

    public int GetTotalInitiative()
    {
        int bonus = 0;
        if (EquippedWeapon != null) bonus += EquippedWeapon.InitiativeBonus;
        if (EquippedArmor != null) bonus += EquippedArmor.InitiativeBonus;
        if (EquippedAccessory != null) bonus += EquippedAccessory.InitiativeBonus;
        return Initiative + bonus;
    }
}

[System.Serializable]
public class LevelUpResult
{
    public string MemberName;
    public int NewLevel;
    public int LevelsGained;
}
