using UnityEngine;

/// <summary>
/// Attach to a world object to make it open a Jukebox UI when interacted with (E key or gamepad Submit/A).
/// Configure the `jukeboxUI` reference to point to the in-scene JukeboxUI component (a UI panel prefab instance).
/// </summary>
public class JukeboxInteractable : MonoBehaviour
{
    [Tooltip("Reference to the Jukebox UI manager (a Canvas panel set up to show track banners).")]
    public JukeboxUI jukeboxUI;

    [Tooltip("Optional: reference to a Jukebox component to control. If empty, the UI's Jukebox will be used.")]
    public Jukebox jukebox;

    [Tooltip("Interaction range in world units (overrides InteractableObject if you want custom range here)")]
    public float interactionRange = 1f;

    private GameObject player;
    private bool playerInRange = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (jukebox == null && jukeboxUI != null)
            jukebox = jukeboxUI.GetComponent<Jukebox>();
    }

    void Update()
    {
        if (player == null) return;
        float dist = Vector2.Distance(player.transform.position, transform.position);
        bool inRange = dist <= interactionRange;
        playerInRange = inRange;
        // Use the central HintManager only; do nothing if a HintManager is not present.
        if (HintManager.Instance != null)
        {
            if (inRange)
            {
                Debug.Log($"JukeboxInteractable: Show hint for '{GetObjectName()}' (dist={dist:F2})", this);
                HintManager.Instance.ShowInteractHint(GetObjectName());
            }
            else
            {
                Debug.Log($"JukeboxInteractable: Hide hint for '{GetObjectName()}'", this);
                HintManager.Instance.HideHintFor(GetObjectName());
            }
        }

        // Interaction is routed via InputManager.InteractPerformed. See OnInteractPerformed.
    }

    void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.InteractPerformed += OnInteractPerformed;
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.InteractPerformed -= OnInteractPerformed;
    }

    private void OnInteractPerformed()
    {
        if (!playerInRange) return;
        if (jukebox != null && jukeboxUI != null)
        {
            jukeboxUI.Show(jukebox, GetObjectName());
        }
        else if (jukebox != null)
        {
            if (jukeboxUI != null) jukeboxUI.Show(jukebox);
        }
    }

    // Expose the same helpers used by InteractionUI so the shared hint can query this object.
    public bool IsPlayerInRange()
    {
        return playerInRange;
    }

    public string GetObjectName()
    {
        return gameObject != null ? gameObject.name : "Jukebox";
    }

    // legacy input helper method removed; InputManager will route Interact when present

}
