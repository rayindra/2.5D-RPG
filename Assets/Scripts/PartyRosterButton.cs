using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Taruh di prefab 1 baris/kartu roster member. Dipakai oleh PartyMenuController.
public class PartyRosterButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject activeIndicator; // contoh: label "AKTIF" atau highlight, aktif kalau member ada di party aktif
    [SerializeField] private Button button;

    private PartyMember member;
    private Action<PartyMember> onClickCallback;

    public void Setup(PartyMember partyMember, bool isActive, Action<PartyMember> onClick)
    {
        member = partyMember;
        onClickCallback = onClick;

        if (nameText != null) nameText.text = partyMember.MemberName;
        if (levelText != null) levelText.text = string.Format("Lv. {0}", partyMember.Level);
        if (activeIndicator != null) activeIndicator.SetActive(isActive);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickCallback?.Invoke(member));
        }
    }
}
