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
    private bool facingRight = true;
    private bool hasMoveXParam = false;
    private bool hasMoveYParam = false;
    private bool hasIsWalkingParam = false;

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
        // Get input from WASD or Arrow keys
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Normalize diagonal movement
        movement = movement.normalized;

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
            facingRight = false;
            if (spriteRenderer != null) spriteRenderer.flipX = true;
        }
        else if (movement.x > 0.01f)
        {
            facingRight = true;
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
