using UnityEngine;

/// <summary>
/// Main game manager that handles game initialization and state.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    [SerializeField] private bool showInstructions = true;
    
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Could add pause menu here
        }
    }
}
