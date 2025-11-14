using UnityEngine;

/// <summary>
/// Simple helper to flip a SpriteRenderer horizontally based on horizontal input.
/// Attach to your player GameObject and wire the SpriteRenderer (and optional Animator).
/// Option A from user: reuse one walk clip and flip the sprite at runtime.
/// </summary>
[DisallowMultipleComponent]
public class PlayerFlip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Animator animator;
    [Tooltip("Animator float parameter name used for movement speed. Leave empty to skip animator updates.")]
    [SerializeField] private string speedParameter = "Speed";

    void OnValidate()
    {
        // Auto-assign sensible defaults to make inspector setup easier
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        // read horizontal input (replace with your movement input if different)
        float moveX = Input.GetAxisRaw("Horizontal");

        if (sr != null)
        {
            // face left when moving left; face right otherwise (including zero => keep previous orientation)
            // We only flip when there's a definite direction to avoid flicker on zero input.
            if (moveX < 0f) sr.flipX = true;
            else if (moveX > 0f) sr.flipX = false;
        }

        if (animator != null && !string.IsNullOrEmpty(speedParameter))
        {
            animator.SetFloat(speedParameter, Mathf.Abs(moveX));
        }
    }
}
