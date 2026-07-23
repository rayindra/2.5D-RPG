using UnityEngine;

public class WorldItemCollectible : MonoBehaviour
{
    public EquipmentInfo ItemToGive;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private SpriteRenderer itemSpriteRenderer;

    private const string PICKUP_MESSAGE_FORMAT = "Press [E] Pickup {0}";

    void Start()
    {
        if (itemSpriteRenderer != null && ItemToGive != null && ItemToGive.Icon != null)
            itemSpriteRenderer.sprite = ItemToGive.Icon;
    }

    public void ShowInteractPrompt(bool show)
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(show);
    }

    public string GetPickupMessage()
    {
        if (ItemToGive != null)
            return string.Format(PICKUP_MESSAGE_FORMAT, ItemToGive.EquipmentName);
        return string.Format(PICKUP_MESSAGE_FORMAT, "???");
    }

    public void Collect()
    {
        PartyManager pm = GameObject.FindFirstObjectByType<PartyManager>();
        if (pm != null)
        {
            pm.AddEquipmentToInventory(ItemToGive);
        }
        Destroy(gameObject);
    }
}