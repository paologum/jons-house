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
    // Track the current interact owner so hide requests from other objects don't stomp a newer hint
    private string currentInteractOwner = null;
    // If a Show request occurs before the hintObject is registered, keep the pending message
    // so it can be shown once RegisterHintObject is called.
    private string pendingHintMessage = null;

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
        if (hintObject != null)
        {
            // ensure the visual hint is initially inactive
            hintObject.SetActive(false);

            // If an interactable previously attempted to show a hint before the
            // hintObject was registered, currentInteractOwner may be set but
            // nothing was actually displayed. Re-show the pending hint so the
            // player sees it without needing to move out/in of range.
            if (!string.IsNullOrEmpty(currentInteractOwner))
            {
                ShowInteractHint(currentInteractOwner);
            }
        }
    }

    /// <summary>
    /// Show the hint with the provided message.
    /// </summary>
    public void ShowHint(string message)
    {
        if (hintObject == null)
        {
            // store pending message for later (diagnostics/restore)
            pendingHintMessage = message;
            Debug.Log($"HintManager: ShowHint called but hintObject null; storing pending message='{message}'", this);
            return;
        }

        hintObject.SetActive(true);
        if (hintText != null) hintText.text = message;
        // generic show does not set an owner
        currentInteractOwner = null;
        // clear any pending message since we displayed it
        pendingHintMessage = null;
        Debug.Log($"HintManager: ShowHint displayed message='{message}'", this);
    }

    /// <summary>
    /// Convenience to show the standard interact hint with an object name.
    /// </summary>
    public void ShowInteractHint(string objectName)
    {
        string msg = string.IsNullOrEmpty(objectName) ? "Press E to interact" : $"Press E to interact with {objectName}";
        // Always record the owner so HideHintFor can clear pending shows as well
        currentInteractOwner = objectName;

        if (hintObject == null)
        {
            // store pending message and owner; RegisterHintObject will re-show it
            pendingHintMessage = msg;
            Debug.Log($"HintManager: ShowInteractHint('{objectName}') called but hintObject null; pending stored.", this);
            return;
        }

        ShowHint(msg);
        Debug.Log($"HintManager: ShowInteractHint displayed for '{objectName}'", this);
    }

    /// <summary>
    /// Hide the hint UI.
    /// </summary>
    public void HideHint()
    {
        if (hintObject == null) return;
        hintObject.SetActive(false);
        currentInteractOwner = null;
    }

    /// <summary>
    /// Hide the hint only if it was last shown for the provided object name.
    /// This prevents one interactable's Hide call from hiding another interactable's hint.
    /// </summary>
    public void HideHintFor(string objectName)
    {
        Debug.Log($"HintManager: HideHintFor called for '{objectName}'; currentOwner='{currentInteractOwner}' pending='{pendingHintMessage}'", this);
        if (string.IsNullOrEmpty(objectName)) { HideHint(); return; }
        if (currentInteractOwner == objectName)
        {
            HideHint();
        }
        else if (!string.IsNullOrEmpty(pendingHintMessage) && currentInteractOwner == objectName)
        {
            // if we had a pending message for this owner, clear it so it won't appear later
            pendingHintMessage = null;
        }
    }
}
