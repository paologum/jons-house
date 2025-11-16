using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Central input façade. Uses the new Input System at runtime when available
/// (looks up actions from a PlayerInput / InputActionAsset). Falls back to
/// InputHelper polling when the Input System or actions asset is not present.
///
/// Exposes events for discrete actions and a ReadMove() helper for polling movement.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Tooltip("If true, keep the InputManager across scene loads.")]
    public bool dontDestroyOnLoad = true;

    public event Action InteractPerformed;
    public event Action CancelPerformed;
    public event Action NextPerformed;
    public event Action PrevPerformed;
    public event Action RandomizeToggled;

    private readonly List<InteractableObject> interactables = new List<InteractableObject>();

    // Runtime references to actions (looked up from PlayerInput.actions)
#if ENABLE_INPUT_SYSTEM
    private InputActionAsset actionsAsset;
    private InputActionMap gameplayMap;
    private InputActionMap uiMap;

    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction cancelAction;
    private InputAction nextAction;
    private InputAction prevAction;
    private InputAction randomizeAction;

    // cached callbacks for clean unsubscription
    private Action<InputAction.CallbackContext> cbInteract;
    private Action<InputAction.CallbackContext> cbCancel;
    private Action<InputAction.CallbackContext> cbNext;
    private Action<InputAction.CallbackContext> cbPrev;
    private Action<InputAction.CallbackContext> cbRandomize;
#endif

    // Polling state for legacy fallback (always declared so compile doesn't depend on define)
    private bool lastInteract = false;
    private bool lastCancel = false;
    private bool lastNext = false;
    private bool lastPrev = false;
    private bool lastRandom = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

#if ENABLE_INPUT_SYSTEM
        // Try to find a PlayerInput in the scene to get the action asset.
        // Use newer API to avoid obsolete warning when available
        var playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            actionsAsset = playerInput.actions;
        }
#endif
    }

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        // Re-resolve the PlayerInput/actions asset in case it was added after Awake
        if (actionsAsset == null)
        {
            var pi = FindFirstObjectByType<PlayerInput>();
            if (pi != null) actionsAsset = pi.actions;
        }

        if (actionsAsset != null)
        {
            gameplayMap = actionsAsset.FindActionMap("Gameplay", true);
            uiMap = actionsAsset.FindActionMap("UI", false);

            moveAction = gameplayMap?.FindAction("Move", true);
            interactAction = gameplayMap?.FindAction("Interact", false);
            nextAction = gameplayMap?.FindAction("Next", false) ?? uiMap?.FindAction("Next", false);
            prevAction = gameplayMap?.FindAction("Prev", false) ?? uiMap?.FindAction("Prev", false);
            randomizeAction = gameplayMap?.FindAction("Randomize", false);
            cancelAction = uiMap?.FindAction("Cancel", false);

            cbInteract = ctx => HandleInteract();
            cbCancel = ctx => HandleCancel();
            cbNext = ctx => HandleNext();
            cbPrev = ctx => HandlePrev();
            cbRandomize = ctx => HandleRandomize();

            if (interactAction != null) interactAction.performed += cbInteract;
            if (cancelAction != null) cancelAction.performed += cbCancel;
            if (nextAction != null) nextAction.performed += cbNext;
            if (prevAction != null) prevAction.performed += cbPrev;
            if (randomizeAction != null) randomizeAction.performed += cbRandomize;

            actionsAsset.Enable();
        }
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (actionsAsset != null)
        {
            if (interactAction != null && cbInteract != null) interactAction.performed -= cbInteract;
            if (cancelAction != null && cbCancel != null) cancelAction.performed -= cbCancel;
            if (nextAction != null && cbNext != null) nextAction.performed -= cbNext;
            if (prevAction != null && cbPrev != null) prevAction.performed -= cbPrev;
            if (randomizeAction != null && cbRandomize != null) randomizeAction.performed -= cbRandomize;

            actionsAsset.Disable();
        }
#else
        lastInteract = lastCancel = lastNext = lastPrev = lastRandom = false;
#endif
    }

    void Update()
    {
        // If the actions asset is present and enabled the performed callbacks will handle discrete
        // actions. Otherwise fall back to legacy polling using the old Input API.
#if ENABLE_INPUT_SYSTEM
        if (actionsAsset != null) return;
#endif

        // Legacy polling fallback (edge detection)
        bool interact = LegacyIsInteractPressed();
        if (interact && !lastInteract) HandleInteract();
        lastInteract = interact;

        bool cancel = LegacyIsCancelPressed();
        if (cancel && !lastCancel) HandleCancel();
        lastCancel = cancel;

        bool next = LegacyIsNextPressed();
        if (next && !lastNext) HandleNext();
        lastNext = next;

        bool prev = LegacyIsPrevPressed();
        if (prev && !lastPrev) HandlePrev();
        lastPrev = prev;

        bool random = LegacyIsRandomizeTogglePressed();
        if (random && !lastRandom) HandleRandomize();
        lastRandom = random;
    }

    public Vector2 ReadMove()
    {
#if ENABLE_INPUT_SYSTEM
        if (moveAction != null) return moveAction.ReadValue<Vector2>();
        // If moveAction not available fall back to legacy axes
#endif
        return new Vector2(GetAxisRawSafe("Horizontal"), GetAxisRawSafe("Vertical"));
    }

    public void EnableUI()
    {
#if ENABLE_INPUT_SYSTEM
        gameplayMap?.Disable();
        uiMap?.Enable();
#endif
    }

    public void EnableGameplay()
    {
#if ENABLE_INPUT_SYSTEM
        uiMap?.Disable();
        gameplayMap?.Enable();
#endif
    }

    public void DisableAll()
    {
#if ENABLE_INPUT_SYSTEM
        actionsAsset?.Disable();
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

    private void HandleInteract()
    {
        InteractPerformed?.Invoke();
        var target = FindNearestInteractableInRange();
        if (target != null)
            target.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
    }

    private void HandleCancel() => CancelPerformed?.Invoke();
    private void HandleNext() => NextPerformed?.Invoke();
    private void HandlePrev() => PrevPerformed?.Invoke();
    private void HandleRandomize() => RandomizeToggled?.Invoke();

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

    // Legacy input helper implementations so this manager is self-contained and doesn't
    // depend on the removed InputHelper class.
    private bool LegacyIsInteractPressed()
    {
        if (Input.GetKeyDown(KeyCode.E)) return true;
        try { if (Input.GetButtonDown("Submit")) return true; } catch { }
        if (Input.GetKeyDown(KeyCode.JoystickButton0)) return true;
        return false;
    }

    private bool LegacyIsCancelPressed()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) return true;
        if (Input.GetKeyDown(KeyCode.JoystickButton1)) return true;
        return false;
    }

    private bool LegacyIsNextPressed()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) return true;
        if (Input.GetKeyDown(KeyCode.JoystickButton5)) return true;
        float h = GetAxisRawSafe("Horizontal");
        return h > 0.5f;
    }

    private bool LegacyIsPrevPressed()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) return true;
        if (Input.GetKeyDown(KeyCode.JoystickButton4)) return true;
        float h = GetAxisRawSafe("Horizontal");
        return h < -0.5f;
    }

    private bool LegacyIsRandomizeTogglePressed()
    {
        if (Input.GetKeyDown(KeyCode.JoystickButton3)) return true;
        return false;
    }

    private float GetAxisRawSafe(string axis)
    {
        try { return Input.GetAxisRaw(axis); } catch { return 0f; }
    }
}

