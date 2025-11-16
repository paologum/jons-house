using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// InputManager: a small façade that only uses the new Input System.
/// It looks up the PlayerInput in the scene (or an inspector-assigned
/// InputActionAsset) and wires the Gameplay action map only. No legacy
/// Input API is used.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Tooltip("If true, keep the InputManager across scene loads.")]
    public bool dontDestroyOnLoad = true;

    // Discrete action events (raised when the corresponding action is performed)
    public event Action InteractPerformed;
    public event Action CancelPerformed;
    public event Action NextPerformed;
    public event Action PrevPerformed;
    // Raised when the OpenJukebox action is performed (e.g. keyboard J or gamepad Start)
    public event Action OpenJukeboxPerformed;
    // Raised when the UI Submit action is performed (e.g. gamepad A / keyboard Enter)
    public event Action SubmitPerformed;
    public event Action RandomizeToggled;

    private readonly List<InteractableObject> interactables = new List<InteractableObject>();

    // Tracks whether we've wired the current actionsAsset
    private bool isWired = false;

#if ENABLE_INPUT_SYSTEM
    [Header("Inspector (optional)")]
    [Tooltip("Optional: assign an InputActionAsset here. If empty, InputManager will find the PlayerInput in the scene and use its actions.")]
    [SerializeField]
    private InputActionAsset inspectorActionsAsset;

    private InputActionAsset actionsAsset;
    private InputActionMap gameplayMap;

    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction openJukeboxAction;
    private InputAction uiSubmitAction;
    private InputAction uiNavigateAction;
    private InputAction nextAction;
    private InputAction prevAction;
    private InputAction randomizeAction;
    private InputAction uiCancelAction;

    // cached delegates so we can unsubscribe cleanly
    private Action<InputAction.CallbackContext> cbInteract;
    private Action<InputAction.CallbackContext> cbNext;
    private Action<InputAction.CallbackContext> cbPrev;
    private Action<InputAction.CallbackContext> cbRandomize;
    private Action<InputAction.CallbackContext> cbOpenJukebox;
    private Action<InputAction.CallbackContext> cbSubmit;
    private Action<InputAction.CallbackContext> cbCancel;
#endif

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        // Prefer inspector asset if present, otherwise look for PlayerInput in scene
        actionsAsset = inspectorActionsAsset;
        if (actionsAsset == null)
        {
            var pi = FindFirstObjectByType<PlayerInput>();
            if (pi != null) actionsAsset = pi.actions;
        }

        if (actionsAsset == null)
        {
            Debug.LogWarning("InputManager: no InputActionAsset found. Attach a PlayerInput or assign an asset to the inspector.", this);
            return;
        }

        // Only use the Gameplay map
        gameplayMap = actionsAsset.FindActionMap("Gameplay", true);
        if (gameplayMap == null)
        {
            Debug.LogError("InputManager: 'Gameplay' action map not found in the assigned action asset.", this);
            return;
        }

        // If an asset was assigned in the inspector or found on PlayerInput, set it up now.
        if (actionsAsset != null && !isWired)
        {
            SetActionAsset(actionsAsset);
        }
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (interactAction != null && cbInteract != null) interactAction.performed -= cbInteract;
        if (nextAction != null && cbNext != null) nextAction.performed -= cbNext;
        if (prevAction != null && cbPrev != null) prevAction.performed -= cbPrev;
        if (randomizeAction != null && cbRandomize != null) randomizeAction.performed -= cbRandomize;

        UnwireActions();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    /// <summary>
    /// Set or replace the action asset at runtime. This is called by InputBootstrap
    /// when PlayerInput is initialized. Safe to call multiple times.
    /// </summary>
    public void SetActionAsset(InputActionAsset asset)
    {
        if (asset == null) return;
        // Unwire any previously wired actions
        UnwireActions();

        actionsAsset = asset;

        // Find Gameplay map and primary actions
        gameplayMap = actionsAsset.FindActionMap("Gameplay", true);
        if (gameplayMap == null)
        {
            Debug.LogError("InputManager.SetActionAsset: 'Gameplay' action map not found in provided asset.", this);
            return;
        }

        moveAction = gameplayMap.FindAction("Move", true);
        interactAction = gameplayMap.FindAction("Interact", false);
    openJukeboxAction = gameplayMap.FindAction("OpenJukebox", false);
        nextAction = gameplayMap.FindAction("Next", false);
        prevAction = gameplayMap.FindAction("Prev", false);
        randomizeAction = gameplayMap.FindAction("Randomize", false);

    cbInteract = ctx => { InteractPerformed?.Invoke(); TriggerInteract(); };
        cbNext = ctx => NextPerformed?.Invoke();
        cbPrev = ctx => PrevPerformed?.Invoke();
        cbRandomize = ctx => RandomizeToggled?.Invoke();
        cbOpenJukebox = ctx => OpenJukeboxPerformed?.Invoke();
        cbSubmit = ctx => SubmitPerformed?.Invoke();
    cbCancel = ctx => { Debug.Log("InputManager: UI Cancel performed", this); CancelPerformed?.Invoke(); };

        if (interactAction != null) interactAction.performed += cbInteract;
    if (openJukeboxAction != null) openJukeboxAction.performed += cbOpenJukebox;
        if (nextAction != null) nextAction.performed += cbNext;
        if (prevAction != null) prevAction.performed += cbPrev;
        if (randomizeAction != null) randomizeAction.performed += cbRandomize;

        // Note: Next/Prev/Randomize are wired only from the Gameplay map above.

        // Wire UI Cancel if present
        var uiMap = actionsAsset.FindActionMap("UI", false);
        if (uiMap != null)
        {
            uiCancelAction = uiMap.FindAction("Cancel", false);
            if (uiCancelAction != null) uiCancelAction.performed += cbCancel;
            uiSubmitAction = uiMap.FindAction("Submit", false);
            if (uiSubmitAction != null) uiSubmitAction.performed += cbSubmit;
            uiNavigateAction = uiMap.FindAction("Navigate", false);
        }

        // Note: Cancel is wired only from the UI map above (no cross-map fallbacks).

        // Enable gameplay map explicitly
        try { gameplayMap.Enable(); } catch { }

        // Diagnostic log to help verify wiring at runtime
        Debug.Log($"InputManager: SetActionAsset wired. Gameplay present={gameplayMap!=null}, Move={moveAction!=null}, Interact={interactAction!=null}, UI present={uiMap!=null}, Cancel={uiCancelAction!=null}", this);

        isWired = true;
    }

    private void UnwireActions()
    {
        try
        {
            if (interactAction != null && cbInteract != null) interactAction.performed -= cbInteract;
            if (openJukeboxAction != null && cbOpenJukebox != null) openJukeboxAction.performed -= cbOpenJukebox;
            if (nextAction != null && cbNext != null) nextAction.performed -= cbNext;
            if (prevAction != null && cbPrev != null) prevAction.performed -= cbPrev;
            if (randomizeAction != null && cbRandomize != null) randomizeAction.performed -= cbRandomize;
            if (uiCancelAction != null && cbCancel != null) uiCancelAction.performed -= cbCancel;
            if (uiSubmitAction != null && cbSubmit != null) uiSubmitAction.performed -= cbSubmit;
            if (uiNavigateAction != null) { /* nothing to unsubscribe for polling */ }
        }
        catch { }

        try { gameplayMap?.Disable(); } catch { }

        // clear references
        moveAction = interactAction = openJukeboxAction = nextAction = prevAction = randomizeAction = uiCancelAction = uiSubmitAction = uiNavigateAction = null;
        gameplayMap = null;
        actionsAsset = null;
        isWired = false;
    }

    /// <summary>
    /// Read the UI Navigate Vector2 (if present) from the UI action map. Returns Vector2.zero if not present.
    /// </summary>
    public Vector2 ReadUINavigate()
    {
#if ENABLE_INPUT_SYSTEM
        if (uiNavigateAction != null) return uiNavigateAction.ReadValue<Vector2>();
#endif
        return Vector2.zero;
    }
#endif

    /// <summary>
    /// Read continuous movement from the Move action on the Gameplay map.
    /// Returns Vector2.zero if Move action is not present.
    /// </summary>
    public Vector2 ReadMove()
    {
#if ENABLE_INPUT_SYSTEM
        if (moveAction != null) return moveAction.ReadValue<Vector2>();
#endif
        return Vector2.zero;
    }

    /// <summary>
    /// Enable the UI action map so UI navigation and Cancel are active.
    /// This will disable the Gameplay map to avoid conflicting inputs.
    /// </summary>
    public void EnableUI()
    {
#if ENABLE_INPUT_SYSTEM
        try
        {
            var uiMap = actionsAsset?.FindActionMap("UI", false);
            // Enable the UI map if present. Do not disable Gameplay here so gameplay-based bindings
            // (e.g. Next/Prev on Gameplay) remain active while UI is open.
            uiMap?.Enable();
        }
        catch { }
        Debug.Log("InputManager: EnableUI called (UI map enabled)", this);
#endif
    }

    /// <summary>
    /// Re-enable Gameplay action map and disable UI action map.
    /// </summary>
    public void EnableGameplay()
    {
#if ENABLE_INPUT_SYSTEM
        try
        {
            var uiMap = actionsAsset?.FindActionMap("UI", false);
            uiMap?.Disable();
            gameplayMap?.Enable();
        }
        catch { }
        Debug.Log("InputManager: EnableGameplay called (Gameplay map enabled)", this);
#endif
    }

    public void RegisterInteractable(InteractableObject obj)
    {
        if (obj == null) return;
        if (!interactables.Contains(obj)) interactables.Add(obj);
    }

    public void UnregisterInteractable(InteractableObject obj)
    {
        if (obj == null) return;
        interactables.Remove(obj);
    }

    private InteractableObject FindNearestInteractableInRange()
    {
        InteractableObject[] candidates = interactables.Count > 0 ? interactables.ToArray() : FindObjectsByType<InteractableObject>(FindObjectsSortMode.None);
        InteractableObject best = null;
        float bestDist = float.MaxValue;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return null;
        Vector2 p = player.transform.position;
        foreach (var c in candidates)
        {
            if (c == null) continue;
            if (!c.IsPlayerInRange()) continue;
            float d = Vector2.Distance(c.transform.position, p);
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }
        return best;
    }

    // For compatibility with existing callers that expect InputManager to invoke interaction behavior
    // we keep this helper that finds the target and sends the Interact message. External code should
    // subscribe to InteractPerformed instead of calling this directly where possible.
    public void TriggerInteract()
    {
        var target = FindNearestInteractableInRange();
        if (target != null)
            target.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
    }
}

