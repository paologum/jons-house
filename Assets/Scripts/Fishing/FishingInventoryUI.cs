using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple UI controller for the FishingInventory.
/// Provide a prefab item (with FishingInventoryItemUI) and a content Transform to populate.
/// </summary>
public class FishingInventoryUI : MonoBehaviour
{
    public Transform contentParent;
    public GameObject itemPrefab; // prefab must have FishingInventoryItemUI component

    void Start()
    {
        if (FishingInventory.Instance != null)
            FishingInventory.Instance.OnInventoryChanged += RefreshUI;
        RefreshUI();
    }

    void OnDestroy()
    {
        if (FishingInventory.Instance != null)
            FishingInventory.Instance.OnInventoryChanged -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (contentParent == null || itemPrefab == null)
            return;

        // clear existing
        for (int i = contentParent.childCount - 1; i >= 0; --i)
            Destroy(contentParent.GetChild(i).gameObject);

        var items = FishingInventory.Instance?.Caught;
        if (items == null) return;

        for (int i = 0; i < items.Count; i++)
        {
            var entry = items[i];
            var go = Instantiate(itemPrefab, contentParent);
            var ui = go.GetComponent<FishingInventoryItemUI>();
            if (ui != null)
                ui.Setup(entry, i);
        }
    }

    // optional helper wired to a Clear button
    public void OnClearButton()
    {
        FishingInventory.Instance?.Clear();
    }
}
