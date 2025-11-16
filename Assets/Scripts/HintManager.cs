using UnityEngine;
using TMPro;

/// <summary>
/// Centralized hint manager singleton. Other systems should call ShowHint/HideHint so
/// the project uses a single shared hint UI element.
/// Drop this on a persistent manager object (e.g. UIManager) and assign the HintObject
/// or let UI components register their hint object via RegisterHintObject.
/// </summary>
public class HintManager : MonoBehaviour
{
    public static HintManager Instance { get; private set; }

    [Tooltip("Optional: the GameObject that visually represents the hint (should contain a TMP text child).")]
    [SerializeField] private GameObject hintObject;

    // cached text component under hintObject
    private TextMeshProUGUI hintText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);

        if (hintObject != null)
            CacheHintText();
    }

    private void CacheHintText()
    {
        if (hintObject != null)
            hintText = hintObject.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    /// <summary>
    /// Register an external hint GameObject (useful if UI components create the hint in scene).
    /// </summary>
    public void RegisterHintObject(GameObject go)
    {
        hintObject = go;
        CacheHintText();
        if (hintObject != null) hintObject.SetActive(false);
    }

    /// <summary>
    /// Show the hint with the provided message.
    /// </summary>
    public void ShowHint(string message)
    {
        if (hintObject == null) return;
        hintObject.SetActive(true);
        if (hintText != null) hintText.text = message;
    }

    /// <summary>
    /// Convenience to show the standard interact hint with an object name.
    /// </summary>
    public void ShowInteractHint(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) ShowHint("Press E to interact");
        else ShowHint($"Press E to interact with {objectName}");
    }

    /// <summary>
    /// Hide the hint UI.
    /// </summary>
    public void HideHint()
    {
        if (hintObject == null) return;
        hintObject.SetActive(false);
    }
}
