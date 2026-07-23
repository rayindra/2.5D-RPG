using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Taruh script ini di root panel Party Menu (misalnya "PartyMenuPanel") di scene Overworld.
public class PartyMenuController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab; // tombol keyboard untuk buka/tutup menu

    [Header("Root Panel")]
    [SerializeField] private GameObject menuRoot; // panel utama yang di-nonaktifkan/aktifkan saat toggle

    [Header("Tabs")]
    [SerializeField] private GameObject statsTabPanel;
    [SerializeField] private GameObject equipmentTabPanel;
    [SerializeField] private GameObject managePartyTabPanel;

    [Header("Roster (daftar semua character joinable: aktif + cadangan)")]
    [SerializeField] private Transform rosterContainer;
    [SerializeField] private PartyRosterButton rosterButtonPrefab;

    [Header("Stats Tab UI")]
    [SerializeField] private TextMeshProUGUI statsNameText;
    [SerializeField] private TextMeshProUGUI statsLevelText;
    [SerializeField] private TextMeshProUGUI statsHealthText;
    [SerializeField] private TextMeshProUGUI statsStrText;
    [SerializeField] private TextMeshProUGUI statsInitiativeText;
    [SerializeField] private Slider statsExpSlider;
    [SerializeField] private TextMeshProUGUI statsExpText;

    [Header("Equipment Tab UI")]
    [SerializeField] private TextMeshProUGUI equippedWeaponText;
    [SerializeField] private TextMeshProUGUI equippedArmorText;
    [SerializeField] private TextMeshProUGUI equippedAccessoryText;
    [SerializeField] private Button unequipWeaponButton;
    [SerializeField] private Button unequipArmorButton;
    [SerializeField] private Button unequipAccessoryButton;
    [SerializeField] private Transform inventoryListContainer;
    [SerializeField] private EquipmentSlotButton inventoryItemButtonPrefab;

    [Header("Manage Party Tab UI")]
    [SerializeField] private TextMeshProUGUI manageInstructionText;

    private PartyManager partyManager;
    private PartyMember selectedMember;    // member yang detailnya sedang ditampilkan di tab Stats/Equipment
    private PartyMember swapSourceMember;  // member AKTIF yang dipilih untuk ditukar di tab Manage Party

    private readonly List<PartyRosterButton> spawnedRosterButtons = new List<PartyRosterButton>();
    private readonly List<EquipmentSlotButton> spawnedInventoryButtons = new List<EquipmentSlotButton>();

    private void Awake()
    {
        partyManager = GameObject.FindFirstObjectByType<PartyManager>();
        if (partyManager == null) Debug.LogError("PartyMenuController: PartyManager tidak ditemukan di scene!");

        if (menuRoot != null) menuRoot.SetActive(false);

        if (unequipWeaponButton != null) unequipWeaponButton.onClick.AddListener(() => OnUnequipPressed(EquipmentType.Weapon));
        if (unequipArmorButton != null) unequipArmorButton.onClick.AddListener(() => OnUnequipPressed(EquipmentType.Armor));
        if (unequipAccessoryButton != null) unequipAccessoryButton.onClick.AddListener(() => OnUnequipPressed(EquipmentType.Accessory));
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMenu();
        }
    }

    // ---------- OPEN / CLOSE ----------

    public void ToggleMenu()
    {
        if (menuRoot == null) return;

        bool willOpen = !menuRoot.activeSelf;
        menuRoot.SetActive(willOpen);

        if (willOpen)
        {
            OpenMenu();
        }
    }

    private void OpenMenu()
    {
        swapSourceMember = null;
        RefreshRoster();

        List<PartyMember> allMembers = GetAllJoinedMembers();
        selectedMember = allMembers.Count > 0 ? allMembers[0] : null;

        ShowStatsTab();
    }

    private List<PartyMember> GetAllJoinedMembers()
    {
        List<PartyMember> all = new List<PartyMember>();
        if (partyManager == null) return all;
        all.AddRange(partyManager.GetCurrentParty());
        all.AddRange(partyManager.GetReserveParty());
        return all;
    }

    // ---------- TAB SWITCHING (hubungkan ke OnClick tombol tab: Stats / Equipment / Manage Party) ----------

    public void ShowStatsTab()
    {
        SetActiveTab(statsTabPanel);
        RefreshStatsPanel();
    }

    public void ShowEquipmentTab()
    {
        SetActiveTab(equipmentTabPanel);
        RefreshEquipmentPanel();
    }

    public void ShowManagePartyTab()
    {
        SetActiveTab(managePartyTabPanel);
        swapSourceMember = null;
        RefreshManagePanel();
    }

    private void SetActiveTab(GameObject activePanel)
    {
        if (statsTabPanel != null) statsTabPanel.SetActive(activePanel == statsTabPanel);
        if (equipmentTabPanel != null) equipmentTabPanel.SetActive(activePanel == equipmentTabPanel);
        if (managePartyTabPanel != null) managePartyTabPanel.SetActive(activePanel == managePartyTabPanel);
    }

    // ---------- ROSTER ----------

    private void RefreshRoster()
    {
        if (rosterContainer == null) { Debug.LogError("RefreshRoster: rosterContainer tidak di-assign di Inspector PartyMenuController!"); return; }
        if (rosterButtonPrefab == null) { Debug.LogError("RefreshRoster: rosterButtonPrefab tidak di-assign di Inspector PartyMenuController!"); return; }

        List<PartyMember> allMembers = GetAllJoinedMembers();

        while (spawnedRosterButtons.Count < allMembers.Count)
        {
            spawnedRosterButtons.Add(Instantiate(rosterButtonPrefab, rosterContainer));
        }
        while (spawnedRosterButtons.Count > allMembers.Count)
        {
            PartyRosterButton extra = spawnedRosterButtons[spawnedRosterButtons.Count - 1];
            spawnedRosterButtons.RemoveAt(spawnedRosterButtons.Count - 1);
            Destroy(extra.gameObject);
        }

        for (int i = 0; i < allMembers.Count; i++)
        {
            PartyMember member = allMembers[i];
            bool isActive = partyManager.GetCurrentParty().Contains(member);
            spawnedRosterButtons[i].gameObject.SetActive(true);
            spawnedRosterButtons[i].Setup(member, isActive, OnRosterMemberClicked);
        }
    }

    private void OnRosterMemberClicked(PartyMember member)
    {
        // Kalau lagi di tab Manage Party, klik roster dipakai untuk memilih member yang mau ditukar.
        if (managePartyTabPanel != null && managePartyTabPanel.activeSelf)
        {
            HandleManagePartySelection(member);
            return;
        }

        selectedMember = member;
        RefreshStatsPanel();
        RefreshEquipmentPanel();
    }

    // ---------- STATS TAB ----------

    private void RefreshStatsPanel()
    {
        if (selectedMember == null) return;

        if (statsNameText != null) statsNameText.text = selectedMember.MemberName;
        if (statsLevelText != null) statsLevelText.text = string.Format("Lv. {0}", selectedMember.Level);
        if (statsHealthText != null) statsHealthText.text = string.Format("HP: {0} / {1}", selectedMember.CurrHealth, selectedMember.GetTotalMaxHealth());
        if (statsStrText != null) statsStrText.text = string.Format("STR: {0}", selectedMember.GetTotalStrength());
        if (statsInitiativeText != null) statsInitiativeText.text = string.Format("INI: {0}", selectedMember.GetTotalInitiative());

        if (statsExpSlider != null)
        {
            statsExpSlider.maxValue = selectedMember.MaxExp;
            statsExpSlider.value = selectedMember.CurrExp;
        }
        if (statsExpText != null) statsExpText.text = string.Format("{0} / {1} EXP", selectedMember.CurrExp, selectedMember.MaxExp);
    }

    // ---------- EQUIPMENT TAB ----------

    private void RefreshEquipmentPanel()
    {
        if (selectedMember == null) return;

        if (equippedWeaponText != null) equippedWeaponText.text = "Weapon: " + (selectedMember.EquippedWeapon != null ? selectedMember.EquippedWeapon.EquipmentName : "(kosong)");
        if (equippedArmorText != null) equippedArmorText.text = "Armor: " + (selectedMember.EquippedArmor != null ? selectedMember.EquippedArmor.EquipmentName : "(kosong)");
        if (equippedAccessoryText != null) equippedAccessoryText.text = "Accessory: " + (selectedMember.EquippedAccessory != null ? selectedMember.EquippedAccessory.EquipmentName : "(kosong)");

        RefreshInventoryList();
    }

    private void RefreshInventoryList()
    {
        List<EquipmentInfo> inventory = partyManager.GetInventory();

        while (spawnedInventoryButtons.Count < inventory.Count)
        {
            spawnedInventoryButtons.Add(Instantiate(inventoryItemButtonPrefab, inventoryListContainer));
        }
        while (spawnedInventoryButtons.Count > inventory.Count)
        {
            EquipmentSlotButton extra = spawnedInventoryButtons[spawnedInventoryButtons.Count - 1];
            spawnedInventoryButtons.RemoveAt(spawnedInventoryButtons.Count - 1);
            Destroy(extra.gameObject);
        }

        for (int i = 0; i < inventory.Count; i++)
        {
            spawnedInventoryButtons[i].gameObject.SetActive(true);
            spawnedInventoryButtons[i].Setup(inventory[i], OnInventoryItemClicked);
        }
    }

    // Klik item di daftar inventory -> langsung pasang ke selectedMember (auto-swap kalau slot sudah terisi)
    private void OnInventoryItemClicked(EquipmentInfo item)
    {
        if (selectedMember == null) return;

        partyManager.EquipItem(selectedMember, item);
        RefreshEquipmentPanel();
    }

    private void OnUnequipPressed(EquipmentType slot)
    {
        if (selectedMember == null) return;

        partyManager.UnequipItem(selectedMember, slot);
        RefreshEquipmentPanel();
    }

    // ---------- MANAGE PARTY TAB (swap active <-> reserve) ----------

    private void RefreshManagePanel()
    {
        if (manageInstructionText != null)
        {
            manageInstructionText.text = "Pilih 1 karakter dari party AKTIF, lalu pilih 1 karakter CADANGAN untuk bertukar posisi.";
        }
    }

    private void HandleManagePartySelection(PartyMember member)
    {
        bool isActive = partyManager.GetCurrentParty().Contains(member);

        if (isActive)
        {
            // pilih dulu member aktif yang mau dikeluarkan
            swapSourceMember = member;
            if (manageInstructionText != null)
            {
                manageInstructionText.text = string.Format("{0} dipilih dari party aktif. Sekarang pilih karakter CADANGAN untuk menggantikannya.", member.MemberName);
            }
        }
        else if (swapSourceMember != null)
        {
            // member kedua yang diklik adalah member cadangan -> lakukan swap
            bool success = partyManager.SwapPartyMember(swapSourceMember, member);
            if (success)
            {
                if (manageInstructionText != null)
                {
                    manageInstructionText.text = string.Format("{0} masuk party aktif, {1} pindah ke cadangan.", member.MemberName, swapSourceMember.MemberName);
                }
                swapSourceMember = null;
                RefreshRoster();
            }
        }
        else
        {
            if (manageInstructionText != null)
            {
                manageInstructionText.text = "Pilih dulu karakter dari party AKTIF sebelum memilih cadangan.";
            }
        }
    }
}
