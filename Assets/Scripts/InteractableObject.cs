using UnityEngine;

/// <summary>
/// Represents an interactable object in the game world.
/// Contains memory data (title, story, optional image) that displays when player interacts.
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("Memory Information")]
    [SerializeField] private string memoryTitle = "Memory Title";
    [TextArea(3, 6)]
    [SerializeField] private string memoryStory = "This is the story of this memory...";
    [SerializeField] private Sprite memoryImage;
    
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private Color glowColor = Color.yellow;
    
    private bool playerInRange = false;
    private GameObject player;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private InteractionUI interactionUI;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Find the player and UI manager
        player = GameObject.FindGameObjectWithTag("Player");
        interactionUI = FindObjectOfType<InteractionUI>();
    }

    void Update()
    {
        if (player == null) return;
        
        // Check distance to player
        float distance = Vector2.Distance(transform.position, player.transform.position);
        playerInRange = distance <= interactionRange;
        
        // Apply glow effect when player is in range
        if (spriteRenderer != null)
        {
            if (playerInRange)
            {
                // Pulsing glow effect
                float pulse = Mathf.Sin(Time.time * 3f) * 0.5f + 0.5f;
                spriteRenderer.color = Color.Lerp(originalColor, glowColor, pulse * 0.5f);
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
        
        // Handle interaction input
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    void Interact()
    {
        if (interactionUI != null)
        {
            interactionUI.ShowMemory(memoryTitle, memoryStory, memoryImage);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw interaction range in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    public bool IsPlayerInRange()
    {
        return playerInRange;
    }

    public string GetObjectName()
    {
        return gameObject.name;
    }
}
