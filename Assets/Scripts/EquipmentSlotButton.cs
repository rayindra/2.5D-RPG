using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Taruh di prefab 1 baris item equipment di daftar inventory. Dipakai oleh PartyMenuController.
public class EquipmentSlotButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI bonusText;
    [SerializeField] private Button button;

    private EquipmentInfo item;
    private Action<EquipmentInfo> onClickCallback;

    public void Setup(EquipmentInfo equipment, Action<EquipmentInfo> onClick)
    {
        item = equipment;
        onClickCallback = onClick;

        if (nameText != null) nameText.text = string.Format("[{0}] {1}", equipment.Type, equipment.EquipmentName);
        if (bonusText != null) bonusText.text = string.Format("HP+{0}  STR+{1}  INI+{2}", equipment.HealthBonus, equipment.StrBonus, equipment.InitiativeBonus);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickCallback?.Invoke(item));
        }
    }
}
