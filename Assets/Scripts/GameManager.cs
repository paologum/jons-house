using UnityEngine;

/// <summary>
/// Main game manager that handles game initialization and state.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    // Serialized for editor use but currently unused by code. Keep it editable in the Inspector
    // and silence the 'assigned but never used' compiler warning.
#pragma warning disable CS0414
    [SerializeField] private bool showInstructions = true;
#pragma warning restore CS0414

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initialize game
        Debug.Log("Jon's House - Game Started!");

        // Set up any initial game state
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Handle global input (like pause menu, etc.)
        // Prefer event-driven Cancel via InputManager when available
        // Legacy polling removed; use InputManager.CancelPerformed event instead.
    }

    void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.CancelPerformed += OnCancelPerformed;
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.CancelPerformed -= OnCancelPerformed;
    }

    private void OnCancelPerformed()
    {
        // Could add pause menu here
    }
}
