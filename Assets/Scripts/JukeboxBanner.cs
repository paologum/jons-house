using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Small helper attached to instantiated banner buttons to handle pointer hover scaling and simple highlight.
/// Requires the target to have a RectTransform (UI Button) and will modify localScale on hover.
/// </summary>
public class JukeboxBanner : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject target;
    private float hoverScale = 1.08f;
    private Vector3 baseScale = Vector3.one;

    public void Setup(GameObject targetObj, float scale)
    {
        this.target = targetObj;
        this.hoverScale = Mathf.Max(1f, scale);
        if (target != null)
        {
            baseScale = target.transform.localScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (target != null)
        {
            StopAllCoroutines();
            target.transform.localScale = baseScale * hoverScale;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (target != null)
        {
            StopAllCoroutines();
            target.transform.localScale = baseScale;
        }
    }
}
