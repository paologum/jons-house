using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// Small helper to ensure a PlayerInput component is present at runtime and
/// wired to the generated `PlayerControls` InputActionAsset.
///
/// Usage: add this component to an existing persistent GameObject (GameManager)
/// or create an empty GameObject called "Input" and assign the PlayerControls
/// asset in the inspector. The script is guarded by ENABLE_INPUT_SYSTEM so it
/// will compile safely when the new Input System is not enabled.
/// </summary>
public class InputBootstrap : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [Tooltip("Assign the PlayerControls input action asset (PlayerControls.asset) here.")]
    public InputActionAsset playerControlsAsset;

    [Tooltip("If true, this GameObject will persist across scene loads.")]
    public bool dontDestroyOnLoad = true;

    /// <summary>Runtime reference to the PlayerInput component on this GameObject.</summary>
    public PlayerInput PlayerInput { get; private set; }

    void Awake()
    {
        // Ensure a PlayerInput is present
        PlayerInput = GetComponent<PlayerInput>();
        if (PlayerInput == null)
        {
            PlayerInput = gameObject.AddComponent<PlayerInput>();
        }

        // Assign the actions asset if provided
        if (playerControlsAsset != null)
        {
            PlayerInput.actions = playerControlsAsset;
        }

        // Use Gameplay as a reasonable default action map if none is set
        if (string.IsNullOrEmpty(PlayerInput.defaultActionMap))
        {
            PlayerInput.defaultActionMap = "Gameplay";
        }

        // Avoid automatic control-scheme switching in single-player contexts
        PlayerInput.neverAutoSwitchControlSchemes = true;

        // Activate the component to allow input events
        PlayerInput.enabled = true;

        // If an InputManager exists, hand over the runtime action asset so it can wire callbacks.
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetActionAsset(PlayerInput.actions);
        }

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }
#if ENABLE_INPUT_SYSTEM
    void Start()
    {
        // Try again after a frame in case InputManager wasn't created yet during Awake.
        if (playerControlsAsset != null && PlayerInput != null && InputManager.Instance != null)
        {
            InputManager.Instance.SetActionAsset(PlayerInput.actions);
            Debug.Log("InputBootstrap: handed PlayerInput.actions to InputManager in Start.", this);
        }
    }
#endif
#else
    void Awake()
    {
        Debug.LogWarning("InputBootstrap: Input System is not enabled (ENABLE_INPUT_SYSTEM not defined). Add the new Input System package and enable it if you want PlayerInput support.");
    }
#endif
}
