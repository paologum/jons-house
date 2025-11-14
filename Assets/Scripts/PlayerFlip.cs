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

        // Safely update animator parameter only if it exists. Many animator controllers
        // don't include a 'Speed' float — calling SetFloat with a missing parameter
        // will log repeated errors in the console. We cache checks and warn once.
        if (animator != null && !string.IsNullOrEmpty(speedParameter))
        {
            // lazy-check animator parameters and cache result
            if (!_hasCheckedSpeedParam)
            {
                _hasCheckedSpeedParam = true;
                _hasSpeedParam = false;
                try
                {
                    var pars = animator.parameters;
                    for (int i = 0; i < pars.Length; i++)
                    {
                        if (pars[i].name == speedParameter && pars[i].type == UnityEngine.AnimatorControllerParameterType.Float)
                        {
                            _hasSpeedParam = true;
                            break;
                        }
                    }
                }
                catch (System.Exception)
                {
                    // In some runtimes accessing parameters can throw; treat as "not present".
                    _hasSpeedParam = false;
                }
            }

            if (_hasSpeedParam)
            {
                animator.SetFloat(speedParameter, Mathf.Abs(moveX));
            }
            else if (!_warnedMissingSpeedParam)
            {
                _warnedMissingSpeedParam = true;
                Debug.LogWarning($"PlayerFlip: Animator does not contain a float parameter named '{speedParameter}'. Set the parameter in the Animator or clear the field on the PlayerFlip component to disable animator updates.", this);
            }
        }
    }

    // cached checks to avoid per-frame reflection-like work
    private bool _hasCheckedSpeedParam = false;
    private bool _hasSpeedParam = false;
    private bool _warnedMissingSpeedParam = false;
}
