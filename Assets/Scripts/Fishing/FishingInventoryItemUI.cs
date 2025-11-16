using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small UI helper to show a CaughtFish entry. Wire the delete button if you want removal.
/// Uses TextMeshProUGUI which is the UI component visible in the inspector (TextMeshPro - Text (UI)).
/// </summary>
public class FishingInventoryItemUI : MonoBehaviour
{
    // Use the UI-specific TMP component class so it's unambiguous in the Inspector
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI timeText;
    public Image iconImage; // optional
    public Button deleteButton;

    int _index = -1;

    public void Setup(CaughtFish entry, int index)
    {
        _index = index;
        if (nameText != null) nameText.text = entry.fishName;
        if (timeText != null)
        {
            System.DateTime dt;
            if (System.DateTime.TryParse(entry.caughtAtUtcIso, null, System.Globalization.DateTimeStyles.RoundtripKind, out dt))
                timeText.text = dt.ToLocalTime().ToString("g");
            else
                timeText.text = entry.caughtAtUtcIso;
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => { FishingInventory.Instance?.RemoveAt(_index); });
        }
    }
}
