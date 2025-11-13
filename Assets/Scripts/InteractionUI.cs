using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the UI panel that displays memories when player interacts with objects.
/// </summary>
public class InteractionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject memoryPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    // removed legacy single-page text field; MemoryData uses pages
    [Tooltip("Optional: full-screen book background image (open book sprite).")]
    [SerializeField] private Image bookBackground;
    [Tooltip("Left page image (for two-page spread).")]
    [SerializeField] private Image leftPageImage;
    [Tooltip("Right page image (for two-page spread).")]
    [SerializeField] private Image rightPageImage;
    [Header("Book Controls")]
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private TextMeshProUGUI leftPageCaptionText;
    [SerializeField] private TextMeshProUGUI rightPageCaptionText;
    [SerializeField] private TextMeshProUGUI pageIndexText;
    [SerializeField] private Button closeButton;

    [Header("Hint Display")]
    [SerializeField] private GameObject interactionHint;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("Editor Preview")]
    [Tooltip("Editor-only: select a MemoryData asset to preview in the Inspector using the Preview button.")]
    [SerializeField]
    private MemoryData editorPreviewMemory;

    [Header("Page Layout")]
    [Tooltip("Top margin in pixels for page images inside their page area.")]
    [SerializeField] private float pageMarginTop = 40f;
    [Tooltip("Bottom margin in pixels for page images inside their page area.")]
    [SerializeField] private float pageMarginBottom = 40f;
    [Tooltip("Left margin in pixels for page images inside their page area.")]
    [SerializeField] private float pageMarginLeft = 40f;
    [Tooltip("Right margin in pixels for page images inside their page area.")]
    [SerializeField] private float pageMarginRight = 40f;
    [Tooltip("If true, respect RectTransform setup you made in the Editor and only set image sizeDelta. If false, the script will force anchors/pivot/position for top-aligned layout.")]
    [SerializeField] private bool preserveAuthoringRect = true;
    [Header("Size Limits")]
    [Tooltip("Maximum fraction of the parent page area the image may fill (0..1). Applied when maxSizePixels is zero.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float maxWidthPercent = 1f;
    [Range(0f, 1f)]
    [SerializeField]
    private float maxHeightPercent = 1f;
    [Tooltip("Optional explicit maximum size in pixels (x=width, y=height). If non-zero, takes precedence over percent limits.")]
    [SerializeField]
    private Vector2 maxSizePixels = Vector2.zero;

    private InteractableObject[] allInteractables;

    [Header("Responsive")]
    [Tooltip("When enabled, the UI will automatically refresh page layout when the panel or canvas size changes (responsive to resolution/aspect).")]
    [SerializeField]
    private bool enableResponsive = true;
    [Tooltip("Sensitivity in pixels to detect layout changes before refreshing. Set to 0 for always-refresh while open.")]
    [SerializeField]
    private float responsiveThreshold = 2f;

    // cached layout state to detect changes
    private Vector2 lastLayoutSize = Vector2.zero;
    private float lastCanvasScale = -1f;

    // Book / paging state: pages are flat; we present two per spread.
    private MemoryData.MemoryPage[] currentPages = new MemoryData.MemoryPage[0];
    // currentSpreadIndex: 0 => pages[0] left, pages[1] right
    private int currentSpreadIndex = 0;

    // Keep original authored anchoredPositions so preview/show doesn't repeatedly
    // shift the images when preserveAuthoringRect is enabled.
    private Dictionary<RectTransform, Vector2> originalAnchoredPositions = new Dictionary<RectTransform, Vector2>();

    void Start()
    {
        // Hide panel at start
        if (memoryPanel != null)
        {
            memoryPanel.SetActive(false);
        }

        if (interactionHint != null)
        {
            interactionHint.SetActive(false);
        }

        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideMemory);
        }

        // Setup arrows (navigate spreads)
        if (leftArrowButton != null)
            leftArrowButton.onClick.AddListener(PrevPage);
        if (rightArrowButton != null)
            rightArrowButton.onClick.AddListener(NextPage);

        // Find all interactable objects (use newer API to avoid deprecated call)
        allInteractables = FindObjectsByType<InteractableObject>(FindObjectsSortMode.None);
    }

    void Update()
    {
        // Check for ESC key to close panel
        if (memoryPanel != null && memoryPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HideMemory();
        }

        // If the book UI is open allow left/right arrow keys to change spreads
        if (memoryPanel != null && memoryPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
                NextPage();
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
                PrevPage();
        }

        // Update interaction hint
        UpdateInteractionHint();

        // Responsive layout: if the panel is active and responsiveness is enabled,
        // detect changes in the reference rect (book background or memory panel)
        // or Canvas scale factor and refresh the page display when they change.
        if (enableResponsive && memoryPanel != null && memoryPanel.activeSelf)
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            float scale = rootCanvas ? rootCanvas.scaleFactor : 1f;

            RectTransform refRt = null;
            if (bookBackground != null) refRt = bookBackground.rectTransform;
            else refRt = memoryPanel.GetComponent<RectTransform>();

            Vector2 currentSize = refRt != null ? refRt.rect.size * scale : Vector2.zero;

            if (lastCanvasScale < 0f) lastCanvasScale = scale;

            bool sizeChanged = Vector2.Distance(currentSize, lastLayoutSize) > responsiveThreshold;
            bool scaleChanged = Mathf.Abs(scale - lastCanvasScale) > 0.001f;

            if (sizeChanged || scaleChanged)
            {
                RefreshPageDisplay();
                lastLayoutSize = currentSize;
                lastCanvasScale = scale;
            }
        }
    }

    // legacy single-page ShowMemory removed — this UI only supports MemoryData pages now

    /// <summary>
    /// Hides the memory panel and resumes game.
    /// </summary>
    public void HideMemory()
    {
        if (memoryPanel != null)
        {
            memoryPanel.SetActive(false);
        }

        // Resume game time
        if (Application.isPlaying)
            Time.timeScale = 1f;

        // Clear paging state
        currentPages = new MemoryData.MemoryPage[0];
        currentSpreadIndex = 0;

        // Restore any authored anchored positions that we modified for preview
        if (originalAnchoredPositions != null && originalAnchoredPositions.Count > 0)
        {
            foreach (var kv in originalAnchoredPositions)
            {
                if (kv.Key != null)
                {
                    kv.Key.anchoredPosition = kv.Value;
                }
            }
            originalAnchoredPositions.Clear();
        }
    }

    /// <summary>
    /// Show memory using a MemoryData ScriptableObject which may contain multiple pages.
    /// </summary>
    public void ShowMemory(MemoryData data)
    {
        if (data == null) return;

        if (memoryPanel == null) return;

        memoryPanel.SetActive(true);

        if (titleText != null)
        {
            titleText.text = data.title;
        }

        // Prepare pages
        currentPages = data.GetPages();
        currentSpreadIndex = 0;

        // Show book background if available
        if (bookBackground != null)
            bookBackground.gameObject.SetActive(true);

        // Show the first spread
        RefreshPageDisplay();

        // Pause the game while the UI is up
        if (Application.isPlaying)
            Time.timeScale = 0f;
    }

    private void RefreshPageDisplay()
    {
        if (currentPages == null || currentPages.Length == 0)
        {
            // nothing to show
            if (leftPageImage != null) leftPageImage.gameObject.SetActive(false);
            if (rightPageImage != null) rightPageImage.gameObject.SetActive(false);
            if (leftPageCaptionText != null) leftPageCaptionText.gameObject.SetActive(false);
            if (rightPageCaptionText != null) rightPageCaptionText.gameObject.SetActive(false);
            if (leftArrowButton != null) leftArrowButton.gameObject.SetActive(false);
            if (rightArrowButton != null) rightArrowButton.gameObject.SetActive(false);
            if (pageIndexText != null) pageIndexText.gameObject.SetActive(false);
            return;
        }
        // Left and right page indices in the flat pages array
        int leftIndex = currentSpreadIndex * 2;
        int rightIndex = leftIndex + 1;

        // Helper to display a page into a target Image and caption
        void ApplyPageTo(Image img, TextMeshProUGUI caption, int pageIndex)
        {
            if (img == null && caption == null) return;
            if (pageIndex < 0 || pageIndex >= currentPages.Length)
            {
                if (img != null) img.gameObject.SetActive(false);
                if (caption != null) caption.gameObject.SetActive(false);
                return;
            }
            var p = currentPages[pageIndex];
            if (img != null)
            {
                if (p.image != null)
                {
                    img.sprite = p.image;
                    img.gameObject.SetActive(true);
                    img.color = Color.white;
                    img.preserveAspect = true;
                    img.type = Image.Type.Simple;
                    img.enabled = true;
                    img.raycastTarget = false;

                    // Compute scaling so the sprite fits inside the parent page area while preserving aspect
                    RectTransform parentRt = img.transform.parent as RectTransform;
                    var rt = img.rectTransform;

                    // If the immediate parent has an invalid rect (0 size), walk up until we find a sized ancestor
                    Transform search = img.transform.parent;
                    while ((parentRt == null || parentRt.rect.width < 2f || parentRt.rect.height < 2f) && search != null && search.parent != null)
                    {
                        search = search.parent;
                        parentRt = search as RectTransform;
                    }

                    Vector2 parentSize = parentRt != null ? parentRt.rect.size : rt.rect.size;

                    // Debug info to help diagnose alignment issues
                    Debug.Log($"InteractionUI: ApplyPageTo parent='{(parentRt != null ? parentRt.name : "<none>")}' parentSize={parentSize} for image='{p.image.name}'", this);

                    // available area after margins
                    float availW = Mathf.Max(1f, parentSize.x - pageMarginLeft - pageMarginRight);
                    float availH = Mathf.Max(1f, parentSize.y - pageMarginTop - pageMarginBottom);

                    // sprite pixel size
                    var spr = p.image;
                    Vector2 spriteSize = new Vector2(spr.rect.width, spr.rect.height);

                    // scale factor in pixels
                    float scale = Mathf.Min(availW / spriteSize.x, availH / spriteSize.y);
                    Vector2 finalSize = spriteSize * scale;

                    // Apply max size limits (explicit pixel limit takes precedence)
                    float maxW = (maxSizePixels.x > 0f) ? maxSizePixels.x : parentSize.x * Mathf.Clamp01(maxWidthPercent);
                    float maxH = (maxSizePixels.y > 0f) ? maxSizePixels.y : parentSize.y * Mathf.Clamp01(maxHeightPercent);
                    finalSize.x = Mathf.Min(finalSize.x, maxW);
                    finalSize.y = Mathf.Min(finalSize.y, maxH);


                    // Apply size & position. If the author manually set up the RectTransform
                    // in the Editor and `preserveAuthoringRect` is enabled, don't override
                    // anchors/pivot/anchoredPosition — only update sizeDelta so the image
                    // fills the intended area without losing your manual layout.
                    // compute horizontal center offset inside the parent after margins
                    // positive means shift right, negative shift left.
                    float centerOffsetX = (pageMarginRight - pageMarginLeft) * 0.5f;

                    if (preserveAuthoringRect)
                    {
                        // When preserving authoring rect, sizeDelta should be set while
                        // keeping anchors/pivot so the element continues to scale nicely
                        // with its parent across different screen sizes. We apply the
                        // horizontal offset relative to the original authored anchored
                        // position to avoid cumulative shifts when ShowMemory is called
                        // multiple times (e.g. using the Inspector Preview button).
                        if (rt != null)
                        {
                            if (!originalAnchoredPositions.ContainsKey(rt))
                            {
                                originalAnchoredPositions[rt] = rt.anchoredPosition;
                            }
                            Vector2 baseAnch = originalAnchoredPositions[rt];
                            rt.sizeDelta = finalSize;
                            rt.anchoredPosition = new Vector2(baseAnch.x + centerOffsetX, baseAnch.y);
                        }
                        else
                        {
                            rt.sizeDelta = finalSize;
                        }
                    }
                    else
                    {
                        // Anchor to top-center inside parent and set size in pixels
                        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                        rt.pivot = new Vector2(0.5f, 1f);
                        rt.sizeDelta = finalSize;
                        // position: x=centerOffsetX to respect asymmetric left/right margins,
                        // y = -topMargin
                        rt.anchoredPosition = new Vector2(centerOffsetX, -pageMarginTop);
                    }

                    Debug.Log($"InteractionUI: rt.anchorMin={rt.anchorMin} anchorMax={rt.anchorMax} pivot={rt.pivot} sizeDelta={rt.sizeDelta} anchoredPos={rt.anchoredPosition}", this);
                }
                else
                {
                    img.gameObject.SetActive(false);
                }
            }
            if (caption != null)
            {
                caption.text = p.caption ?? "";
                caption.gameObject.SetActive(!string.IsNullOrEmpty(p.caption));
            }
        }

        ApplyPageTo(leftPageImage, leftPageCaptionText, leftIndex);
        ApplyPageTo(rightPageImage, rightPageCaptionText, rightIndex);

        // Arrows visible only when there is more than one page
        int totalPages = currentPages.Length;
        bool multiple = totalPages > 1;
        if (leftArrowButton != null) leftArrowButton.gameObject.SetActive(multiple);
        if (rightArrowButton != null) rightArrowButton.gameObject.SetActive(multiple);

        // Update index text to show actual page numbers in the spread (e.g. "1–2 / 6" or "3 / 5")
        if (pageIndexText != null)
        {
            int leftNum = Mathf.Min(leftIndex + 1, totalPages);
            int rightNum = (rightIndex < totalPages) ? (rightIndex + 1) : -1;
            if (rightNum == -1)
                pageIndexText.text = $"{leftNum} / {totalPages}";
            else
                pageIndexText.text = $"{leftNum}–{rightNum} / {totalPages}";
            pageIndexText.gameObject.SetActive(multiple);
        }
    }

    public void NextPage()
    {
        if (currentPages == null || currentPages.Length <= 1) return;
        // advance one spread (two pages)
        int maxSpread = Mathf.CeilToInt(currentPages.Length / 2f) - 1;
        currentSpreadIndex = Mathf.Min(currentSpreadIndex + 1, maxSpread);
        RefreshPageDisplay();
    }

    public void PrevPage()
    {
        if (currentPages == null || currentPages.Length <= 1) return;
        currentSpreadIndex = Mathf.Max(currentSpreadIndex - 1, 0);
        RefreshPageDisplay();
    }

    /// <summary>
    /// Updates the "E to interact" hint based on proximity to interactable objects.
    /// </summary>
    private void UpdateInteractionHint()
    {
        if (interactionHint == null) return;

        // Check if any interactable object is in range
        bool anyInRange = false;
        string objectName = "";

        foreach (var interactable in allInteractables)
        {
            if (interactable != null && interactable.IsPlayerInRange())
            {
                anyInRange = true;
                objectName = interactable.GetObjectName();
                break;
            }
        }

        interactionHint.SetActive(anyInRange);

        if (anyInRange && hintText != null)
        {
            hintText.text = $"Press E to interact with {objectName}";
        }
    }
}
