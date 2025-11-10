using UnityEngine;
using UnityEngine.Rendering;

// Simple Y-sorting helper for top-down 2D games.
// Attach to a character or object root to automatically set
// SpriteRenderer.sortingOrder (or a SortingGroup) based on world Y position.
[ExecuteAlways]
public class YSort : MonoBehaviour
{
    [Tooltip("Base sorting order added after Y-based calculation.")]
    public int sortingOrderBase = 0;

    [Tooltip("Extra manual offset (positive brings the object forward).")]
    public int manualOffset = 0;

    [Tooltip("Multiplier used to convert float Y into integer order. Higher = finer granularity.")]
    public int multiplier = 100;

    [Tooltip("If true, set the order on all SpriteRenderers in children. Otherwise set only on the root SpriteRenderer.")]
    public bool applyToChildren = true;

    [Tooltip("If true, prefer a SortingGroup on this GameObject (adds one if missing).")]
    public bool useSortingGroup = false;

    SortingGroup sortingGroup;

    void Awake()
    {
        if (useSortingGroup)
        {
            sortingGroup = GetComponent<SortingGroup>();
            if (sortingGroup == null)
                sortingGroup = gameObject.AddComponent<SortingGroup>();
        }
    }

    void LateUpdate()
    {
        UpdateSorting();
    }

    // Public so other systems can call it if you need to force an update.
    public void UpdateSorting()
    {
        // Calculate an order so that lower Y -> higher order (drawn in front).
        // We negate Y because in Unity world Y increases up; for top-down we want
        // objects with smaller Y (visually lower on screen) to be in front.
        int yOrder = -Mathf.RoundToInt(transform.position.y * multiplier);
        int order = sortingOrderBase + yOrder + manualOffset;

        if (useSortingGroup && sortingGroup != null)
        {
            sortingGroup.sortingOrder = order;
            return;
        }

        if (applyToChildren)
        {
            var rends = GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in rends)
            {
                // Preserve any additional per-renderer offset by reading existing order if necessary.
                // For simplicity we set the same order for all child renderers so the sprite stays assembled.
                r.sortingOrder = order;
            }
        }
        else
        {
            var r = GetComponent<SpriteRenderer>();
            if (r != null)
                r.sortingOrder = order;
        }
    }

    // Convenience: update in editor when values change
    void OnValidate()
    {
        // don't call heavy work in edit-time too often; it's small so it's OK here
        Awake();
        UpdateSorting();
    }
}
