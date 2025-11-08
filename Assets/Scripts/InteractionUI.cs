using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the UI panel that displays memories when player interacts with objects.
/// </summary>
public class InteractionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject memoryPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private Image memoryImageDisplay;
    [SerializeField] private Button closeButton;
    
    [Header("Hint Display")]
    [SerializeField] private GameObject interactionHint;
    [SerializeField] private TextMeshProUGUI hintText;

    private InteractableObject[] allInteractables;

    void Start()
    {
        // Hide panel at start
        if (memoryPanel != null)
        {
            memoryPanel.SetActive(false);
        }
        
        if (interactionHint != null)
        {
            interactionHint.SetActive(false);
        }
        
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideMemory);
        }
        
        // Find all interactable objects
        allInteractables = FindObjectsOfType<InteractableObject>();
    }

    void Update()
    {
        // Check for ESC key to close panel
        if (memoryPanel != null && memoryPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HideMemory();
        }
        
        // Update interaction hint
        UpdateInteractionHint();
    }

    /// <summary>
    /// Shows the memory panel with the provided information.
    /// </summary>
    public void ShowMemory(string title, string story, Sprite image = null)
    {
        if (memoryPanel == null) return;
        
        memoryPanel.SetActive(true);
        
        if (titleText != null)
        {
            titleText.text = title;
        }
        
        if (storyText != null)
        {
            storyText.text = story;
        }
        
        if (memoryImageDisplay != null)
        {
            if (image != null)
            {
                memoryImageDisplay.sprite = image;
                memoryImageDisplay.gameObject.SetActive(true);
            }
            else
            {
                memoryImageDisplay.gameObject.SetActive(false);
            }
        }
        
        // Pause game time (optional)
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Hides the memory panel and resumes game.
    /// </summary>
    public void HideMemory()
    {
        if (memoryPanel != null)
        {
            memoryPanel.SetActive(false);
        }
        
        // Resume game time
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Updates the "E to interact" hint based on proximity to interactable objects.
    /// </summary>
    private void UpdateInteractionHint()
    {
        if (interactionHint == null) return;
        
        // Check if any interactable object is in range
        bool anyInRange = false;
        string objectName = "";
        
        foreach (var interactable in allInteractables)
        {
            if (interactable != null && interactable.IsPlayerInRange())
            {
                anyInRange = true;
                objectName = interactable.GetObjectName();
                break;
            }
        }
        
        interactionHint.SetActive(anyInRange);
        
        if (anyInRange && hintText != null)
        {
            hintText.text = $"Press E to interact with {objectName}";
        }
    }
}
