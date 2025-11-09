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
    [SerializeField] private float interactionRange = 1f;
    [SerializeField] private Color glowColor = Color.yellow;

    private bool playerInRange = false;
    private GameObject player;
    // Support multi-tile prefabs by caching all SpriteRenderers under this object
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private InteractionUI interactionUI;
    private Collider2D objectCollider;

    void Start()
    {
        // Cache all SpriteRenderers in this object (including children) so we can apply glow to multi-tile props
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                originalColors[i] = spriteRenderers[i].color;
            }
        }

        // Find the player and UI manager
        player = GameObject.FindGameObjectWithTag("Player");
        // Use FindAnyObjectByType to avoid deprecated API and for performance
        interactionUI = FindAnyObjectByType<InteractionUI>();
        // Cache any collider on this object (useful for multi-tile prefabs)
        objectCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (player == null) return;

        // Determine distance from the player to this object.
        // For multi-tile prefabs the transform.position may be at a corner; prefer using the collider's closest point
        // if available, otherwise use the sprite bounds center.
        Vector2 playerPos = player.transform.position;
        float distance;
        if (objectCollider != null)
        {
            // ClosestPoint returns a point on the collider perimeter (or inside it) nearest to the given point.
            Vector2 closest = objectCollider.ClosestPoint(playerPos);
            distance = Vector2.Distance(closest, playerPos);
        }
        else if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            Vector2 center = spriteRenderers[0].bounds.center;
            distance = Vector2.Distance(center, playerPos);
        }
        else
        {
            distance = Vector2.Distance(transform.position, playerPos);
        }

        playerInRange = distance <= interactionRange;

        // Apply glow effect when player is in range to all sprite renderers
        if (spriteRenderers != null && originalColors != null)
        {
            if (playerInRange)
            {
                // Pulsing glow effect
                float pulse = Mathf.Sin(Time.time * 3f) * 0.5f + 0.5f;
                float t = pulse * 0.5f;
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    var sr = spriteRenderers[i];
                    if (sr != null)
                        sr.color = Color.Lerp(originalColors[i], glowColor, t);
                }
            }
            else
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    var sr = spriteRenderers[i];
                    if (sr != null)
                        sr.color = originalColors[i];
                }
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
        // Draw interaction range in editor. Use collider or sprite center if available so gizmo matches runtime distance math.
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        var sr = GetComponent<SpriteRenderer>();
        var col = GetComponent<Collider2D>();
        if (col != null)
            center = col.bounds.center;
        else if (sr != null)
            center = sr.bounds.center;
        Gizmos.DrawWireSphere(center, interactionRange);
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
