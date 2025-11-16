using UnityEngine;

/// <summary>
/// Controls player movement and handles input for the character.
/// Supports WASD and Arrow key controls.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    // facingRight removed: sprite flipping is applied directly to SpriteRenderer.flipX
    private bool hasMoveXParam = false;
    private bool hasMoveYParam = false;
    private bool hasIsWalkingParam = false;
    private bool _warnedMissingInputManager = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Cache which animator parameters exist to avoid runtime errors when calling SetFloat/SetBool
        if (animator != null)
        {
            var pars = animator.parameters;
            for (int i = 0; i < pars.Length; i++)
            {
                var p = pars[i];
                if (p.name == "moveX") hasMoveXParam = true;
                else if (p.name == "moveY") hasMoveYParam = true;
                else if (p.name == "isWalking") hasIsWalkingParam = true;
            }
        }

        // Configure Rigidbody2D for top-down movement
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        // Read movement exclusively from InputManager. Legacy direct Input reads are deprecated;
        // log a one-time warning if the InputManager is missing so maintainers can migrate.
        Vector2 inputMove = Vector2.zero;
        if (InputManager.Instance != null)
        {
            inputMove = InputManager.Instance.ReadMove();
        }
        else
        {
            if (!_warnedMissingInputManager)
            {
                _warnedMissingInputManager = true;
                Debug.LogWarning("PlayerController: InputManager not found in scene. Legacy direct Input reads are deprecated — add an InputManager to the scene or assign PlayerInput via InputBootstrap.");
            }
            // graceful fallback to zero movement to avoid unexpected motion
            inputMove = Vector2.zero;
        }

        // Normalize diagonal movement
        movement = inputMove.normalized;

        // Animation parameters
        bool isWalking = movement.sqrMagnitude > 0.001f;
        if (animator != null && hasIsWalkingParam)
            animator.SetBool("isWalking", isWalking);

        if (animator != null)
        {
            if (hasMoveXParam) animator.SetFloat("moveX", movement.x);
            if (hasMoveYParam) animator.SetFloat("moveY", movement.y);
        }

        // Update facing based on horizontal input. Only change facing when there's horizontal movement
        if (movement.x < -0.01f)
        {
            if (spriteRenderer != null) spriteRenderer.flipX = true;
        }
        else if (movement.x > 0.01f)
        {
            if (spriteRenderer != null) spriteRenderer.flipX = false;
        }
    }

    void FixedUpdate()
    {
        // Move the player
        if (rb != null)
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }
}
