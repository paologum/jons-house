using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
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
    [Tooltip("Optional RawImage used to render a VideoClip on the left page. If set, video pages will use this instead of the Image.")]
    [SerializeField] private RawImage leftPageRawImage;
    [Tooltip("Optional RawImage used to render a VideoClip on the right page. If set, video pages will use this instead of the Image.")]
    [SerializeField] private RawImage rightPageRawImage;
    [Header("Book Controls")]
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private TextMeshProUGUI leftPageCaptionText;
    [SerializeField] private TextMeshProUGUI rightPageCaptionText;
    [SerializeField] private TextMeshProUGUI leftPageTitleText;
    [SerializeField] private TextMeshProUGUI rightPageTitleText;
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

    [Header("Caption Sizing")]
    [Tooltip("When true, TextMeshPro captions will auto-size between Min and Max font sizes to fit their Rect.")]
    [SerializeField] private bool captionAutoSize = true;
    [Tooltip("Minimum font size for caption auto-sizing.")]
    [SerializeField] private float captionFontMin = 12f;
    [Tooltip("Maximum font size for caption auto-sizing.")]
    [SerializeField] private float captionFontMax = 28f;
    [Tooltip("If >0, the caption RectTransform will expand up to this height (in pixels). Set 0 to allow unlimited height.")]
    [SerializeField] private float captionMaxHeight = 300f;
    [Tooltip("Extra padding (pixels) added when computing caption preferred height.")]
    [SerializeField] private float captionPadding = 6f;

    [Header("Title Sizing")]
    [Tooltip("When true, TextMeshPro titles will auto-size between Min and Max font sizes to fit their Rect.")]
    [SerializeField] private bool titleAutoSize = true;
    [Tooltip("Minimum font size for title auto-sizing.")]
    [SerializeField] private float titleFontMin = 12f;
    [Tooltip("Maximum font size for title auto-sizing.")]
    [SerializeField] private float titleFontMax = 20f;
    [Tooltip("If >0, the title RectTransform will expand up to this height (in pixels). Set 0 to allow unlimited height.")]
    [SerializeField] private float titleMaxHeight = 80f;
    [Tooltip("Extra padding (pixels) added when computing title preferred height.")]
    [SerializeField] private float titlePadding = 4f;
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

    [Header("Video Debug")]
    [Tooltip("If enabled, show a small debug overlay in Play mode that reports VideoPlayer state for each page side.")]
    [SerializeField]
    private bool enableVideoDebugOverlay = false;

    // runtime debug overlay (created on demand)
    private TextMeshProUGUI _videoDebugOverlay;

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

    // Video playback helpers (one per side)
    private VideoPlayer leftVideoPlayer;
    private RenderTexture leftVideoRT;
    private VideoPlayer rightVideoPlayer;
    private RenderTexture rightVideoRT;
    // Last applied rotation angles per side (for debug overlay)
    private int leftVideoRotation = 0;
    private int rightVideoRotation = 0;

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

    private Vector3 GetWorldCenter(RectTransform rt)
    {
        if (rt == null) return Vector3.zero;
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
    }

    private void EnsureVideoDebugOverlay()
    {
        if (_videoDebugOverlay != null) return;
        if (memoryPanel == null) return;

        // Try to find an existing child named _VideoDebugOverlay
        var existing = memoryPanel.transform.Find("_VideoDebugOverlay");
        if (existing != null)
        {
            _videoDebugOverlay = existing.GetComponent<TextMeshProUGUI>();
            if (_videoDebugOverlay != null) return;
        }

        // Create a small TextMeshProUGUI under the panel for debug output
        var go = new GameObject("_VideoDebugOverlay", typeof(RectTransform));
        go.hideFlags = HideFlags.DontSave;
        go.transform.SetParent(memoryPanel.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(320f, 120f);
        rt.anchoredPosition = new Vector2(-10f, 10f);

        // Add TextMeshProUGUI if available
        try
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 12f;
            tmp.enableWordWrapping = true;
            tmp.color = Color.yellow;
            tmp.alignment = TextAlignmentOptions.TopRight;
            tmp.raycastTarget = false;
            _videoDebugOverlay = tmp;
        }
        catch
        {
            // If TMP isn't available for some reason, fall back to no overlay.
            Object.DestroyImmediate(go);
            _videoDebugOverlay = null;
        }
    }

    private void UpdateVideoDebugOverlay()
    {
        if (!enableVideoDebugOverlay) return;
        if (memoryPanel == null || !memoryPanel.activeSelf) return;
        EnsureVideoDebugOverlay();
        if (_videoDebugOverlay == null) return;

        string leftStatus = "none";
        if (leftVideoPlayer != null)
        {
            leftStatus = $"clip={leftVideoPlayer.clip?.name ?? "-"} prepared={leftVideoPlayer.isPrepared} playing={leftVideoPlayer.isPlaying} rt={leftVideoRT?.width}x{leftVideoRT?.height} rot={leftVideoRotation}°";
        }

        string rightStatus = "none";
        if (rightVideoPlayer != null)
        {
            rightStatus = $"clip={rightVideoPlayer.clip?.name ?? "-"} prepared={rightVideoPlayer.isPrepared} playing={rightVideoPlayer.isPlaying} rt={rightVideoRT?.width}x{rightVideoRT?.height} rot={rightVideoRotation}°";
        }

        _videoDebugOverlay.text = "Video Debug:\n" +
            $"Left: {leftStatus}\n" +
            $"Right: {rightStatus}\n" +
            $"(Enable/disable via InteractionUI.enableVideoDebugOverlay)";
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

        // Update video debug overlay each frame when visible
        if (enableVideoDebugOverlay && memoryPanel != null && memoryPanel.activeSelf)
        {
            UpdateVideoDebugOverlay();
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

        // Stop and cleanup any active video players and render textures
        if (leftVideoPlayer != null)
        {
            try { leftVideoPlayer.Stop(); } catch { }
            Object.DestroyImmediate(leftVideoPlayer.gameObject);
            leftVideoPlayer = null;
        }
        if (leftVideoRT != null)
        {
            leftVideoRT.Release();
            Object.DestroyImmediate(leftVideoRT);
            leftVideoRT = null;
        }
        if (rightVideoPlayer != null)
        {
            try { rightVideoPlayer.Stop(); } catch { }
            Object.DestroyImmediate(rightVideoPlayer.gameObject);
            rightVideoPlayer = null;
        }
        if (rightVideoRT != null)
        {
            rightVideoRT.Release();
            Object.DestroyImmediate(rightVideoRT);
            rightVideoRT = null;
        }

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
            if (leftPageRawImage != null) leftPageRawImage.gameObject.SetActive(false);
            if (rightPageRawImage != null) rightPageRawImage.gameObject.SetActive(false);
            if (leftPageCaptionText != null) leftPageCaptionText.gameObject.SetActive(false);
            if (rightPageCaptionText != null) rightPageCaptionText.gameObject.SetActive(false);
            if (leftPageTitleText != null) leftPageTitleText.gameObject.SetActive(false);
            if (rightPageTitleText != null) rightPageTitleText.gameObject.SetActive(false);
            if (leftArrowButton != null) leftArrowButton.gameObject.SetActive(false);
            if (rightArrowButton != null) rightArrowButton.gameObject.SetActive(false);
            if (pageIndexText != null) pageIndexText.gameObject.SetActive(false);
            return;
        }
        // Left and right page indices in the flat pages array
        int leftIndex = currentSpreadIndex * 2;
        int rightIndex = leftIndex + 1;

        // Helper to display a page into a target Image and caption
        void ApplyPageTo(Image img, RawImage raw, TextMeshProUGUI caption, TextMeshProUGUI sideTitle, int pageIndex, bool isLeftSide)
        {
            if (img == null && raw == null && caption == null) return;
            if (pageIndex < 0 || pageIndex >= currentPages.Length)
            {
                if (img != null) img.gameObject.SetActive(false);
                if (raw != null) raw.gameObject.SetActive(false);
                if (caption != null) caption.gameObject.SetActive(false);
                if (sideTitle != null) sideTitle.gameObject.SetActive(false);
                return;
            }

            var p = currentPages[pageIndex];

            // If the page contains a video, prefer the RawImage + VideoPlayer path
            if (p.video != null)
            {
                if (img != null) img.gameObject.SetActive(false);

                if (raw != null)
                {
                    raw.gameObject.SetActive(true);

                    VideoPlayer vp = isLeftSide ? leftVideoPlayer : rightVideoPlayer;
                    RenderTexture rtTex = isLeftSide ? leftVideoRT : rightVideoRT;

                    // choose target size from parent rect (we'll create an RT sized to preserve the video's aspect)
                    RectTransform parentRt = raw.transform.parent as RectTransform;
                    Transform search = raw.transform.parent;
                    while ((parentRt == null || parentRt.rect.width < 2f || parentRt.rect.height < 2f) && search != null && search.parent != null)
                    {
                        search = search.parent;
                        parentRt = search as RectTransform;
                    }
                    Vector2 parentSize = parentRt != null ? parentRt.rect.size : raw.rectTransform.rect.size;

                    // available area after margins
                    float availW = Mathf.Max(1f, parentSize.x - pageMarginLeft - pageMarginRight);
                    float availH = Mathf.Max(1f, parentSize.y - pageMarginTop - pageMarginBottom);

                    // try to read clip's native pixel size; fall back to parent size
                    int nativeW = (p.video != null && p.video.width > 0) ? (int)p.video.width : Mathf.Max(16, Mathf.CeilToInt(parentSize.x));
                    int nativeH = (p.video != null && p.video.height > 0) ? (int)p.video.height : Mathf.Max(16, Mathf.CeilToInt(parentSize.y));

                    // read rotation hint from the page (designer-controlled). If rotation is 90/270,
                    // we'll treat the displayed dimensions as swapped (width<->height) and rotate the RawImage rect.
                    int rotationAngle = 0;
                    try { rotationAngle = (int)p.videoRotation; } catch { rotationAngle = 0; }
                    bool swapDims = (rotationAngle == 90 || rotationAngle == 270);

                    // Compute a base final size using the video's native orientation (no swap)
                    // This makes rotation affect only orientation, not the computed fit size.
                    float scale = Mathf.Min(availW / (float)nativeW, availH / (float)nativeH);
                    scale = Mathf.Max(0.01f, scale);
                    Vector2 baseFinal = new Vector2(nativeW * scale, nativeH * scale);

                    // If the video is rotated 90/270, swap the assigned size so the rotated
                    // visual occupies the same area (just rotated) instead of shrinking.
                    Vector2 finalSize = swapDims ? new Vector2(baseFinal.y, baseFinal.x) : baseFinal;

                    // Apply max size limits (explicit pixel limit takes precedence)
                    float maxW = (maxSizePixels.x > 0f) ? maxSizePixels.x : parentSize.x * Mathf.Clamp01(maxWidthPercent);
                    float maxH = (maxSizePixels.y > 0f) ? maxSizePixels.y : parentSize.y * Mathf.Clamp01(maxHeightPercent);
                    finalSize.x = Mathf.Min(finalSize.x, maxW);
                    finalSize.y = Mathf.Min(finalSize.y, maxH);

                    // Create the RenderTexture sized to the video's unrotated pixel dimensions (baseFinal)
                    int texW = Mathf.Max(16, Mathf.CeilToInt(baseFinal.x));
                    int texH = Mathf.Max(16, Mathf.CeilToInt(baseFinal.y));

                    if (rtTex == null || rtTex.width != texW || rtTex.height != texH)
                    {
                        if (isLeftSide)
                        {
                            if (leftVideoRT != null) { leftVideoRT.Release(); Object.DestroyImmediate(leftVideoRT); }
                            leftVideoRT = new RenderTexture(texW, texH, 0, RenderTextureFormat.ARGB32);
                            rtTex = leftVideoRT;
                        }
                        else
                        {
                            if (rightVideoRT != null) { rightVideoRT.Release(); Object.DestroyImmediate(rightVideoRT); }
                            rightVideoRT = new RenderTexture(texW, texH, 0, RenderTextureFormat.ARGB32);
                            rtTex = rightVideoRT;
                        }
                    }

                    // Update RawImage rect to preserve aspect ratio and fit inside the page area
                    var rawRt = raw.rectTransform;
                    float centerOffsetX = (pageMarginRight - pageMarginLeft) * 0.5f;
                    // Preserve visual center while changing pivot/size when rotating so the image
                    // doesn't suddenly shift. We'll compute world center before/after sizing and
                    // adjust the RectTransform position to cancel the displacement.
                    Vector3 oldCenter = Vector3.zero;
                    if (rotationAngle != 0)
                    {
                        oldCenter = GetWorldCenter(rawRt);
                    }

                    if (preserveAuthoringRect)
                    {
                        if (!originalAnchoredPositions.ContainsKey(rawRt))
                        {
                            originalAnchoredPositions[rawRt] = rawRt.anchoredPosition;
                        }
                        var baseAnch = originalAnchoredPositions[rawRt];
                        rawRt.sizeDelta = finalSize;
                        rawRt.anchoredPosition = new Vector2(baseAnch.x + centerOffsetX, baseAnch.y);
                    }
                    else
                    {
                        rawRt.anchorMin = rawRt.anchorMax = new Vector2(0.5f, 1f);
                        // when rotating, use center pivot to avoid visual offset; otherwise keep top pivot
                        rawRt.pivot = (rotationAngle != 0) ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
                        rawRt.sizeDelta = finalSize;
                        rawRt.anchoredPosition = new Vector2(centerOffsetX, -pageMarginTop);
                    }

                    // Apply rotation to the RawImage so the video appears upright.
                    rawRt.localEulerAngles = new Vector3(0f, 0f, rotationAngle);
                    if (isLeftSide) leftVideoRotation = rotationAngle; else rightVideoRotation = rotationAngle;

                    if (rotationAngle != 0)
                    {
                        // compute new center and move RectTransform so visual center is preserved
                        Vector3 newCenter = GetWorldCenter(rawRt);
                        Vector3 delta = newCenter - oldCenter;
                        if (rawRt.parent != null)
                        {
                            // convert world delta to parent local space and adjust anchoredPosition
                            Vector3 parentDelta = rawRt.parent.InverseTransformVector(delta);
                            rawRt.anchoredPosition = rawRt.anchoredPosition - (Vector2)parentDelta;
                        }
                        Debug.Log($"InteractionUI: applied rotation {rotationAngle}° to {(isLeftSide ? "left" : "right")} RawImage for page {pageIndex}", this);
                    }

                    if (vp == null)
                    {
                        var go = new GameObject(isLeftSide ? "_LeftVideoPlayer" : "_RightVideoPlayer");
                        go.hideFlags = HideFlags.HideAndDontSave;
                        go.transform.SetParent(this.transform, false);
                        vp = go.AddComponent<VideoPlayer>();
                        vp.playOnAwake = false;
                        vp.renderMode = VideoRenderMode.RenderTexture;
                        vp.skipOnDrop = true;
                        vp.audioOutputMode = VideoAudioOutputMode.None;
                        if (isLeftSide) leftVideoPlayer = vp; else rightVideoPlayer = vp;
                    }

                    vp.targetTexture = rtTex;
                    raw.texture = rtTex;
                    vp.clip = p.video;
                    // force looping and autoplay behaviour for memories
                    vp.isLooping = true;
                    vp.skipOnDrop = true;
                    vp.audioOutputMode = VideoAudioOutputMode.None;
                    vp.renderMode = VideoRenderMode.RenderTexture;

                    // ensure RenderTexture is created before playing
                    try
                    {
                        if (rtTex != null && !rtTex.IsCreated()) rtTex.Create();
                    }
                    catch { }

                    // Use Prepare/prepareCompleted to avoid black-frame issues on some platforms
                    VideoPlayer.EventHandler handler = null;
                    handler = (VideoPlayer src) =>
                    {
                        try { src.Play(); } catch { }
                        // unsubscribe
                        try { src.prepareCompleted -= handler; } catch { }
                    };

                    try
                    {
                    src:;
                        vp.prepareCompleted += handler;
                        vp.Prepare();
                        Debug.Log($"InteractionUI: preparing video for page (width={rtTex.width},height={rtTex.height}) clip={p.video}", this);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"InteractionUI: Video Prepare failed: {ex.Message}. Attempting direct Play.", this);
                        try { vp.Play(); } catch { }
                    }
                }
                else
                {
                    string sideName = isLeftSide ? "left" : "right";
                    Debug.LogWarning($"InteractionUI: page {pageIndex} (side={sideName}) contains a video but no RawImage assigned for this side. Assign '{(isLeftSide ? nameof(leftPageRawImage) : nameof(rightPageRawImage))}' on the InteractionUI component to enable video playback.", this);
                    if (enableVideoDebugOverlay)
                    {
                        EnsureVideoDebugOverlay();
                        if (_videoDebugOverlay != null)
                        {
                            _videoDebugOverlay.text = $"Side: {sideName}\nPage: {pageIndex}\nStatus: No RawImage assigned — cannot play video.\nAssign '{(isLeftSide ? "leftPageRawImage" : "rightPageRawImage")}' in the inspector.";
                        }
                    }
                }
            }
            else
            {
                // If we previously had a video player on this side, stop it and detach the render texture
                if (isLeftSide)
                {
                    if (leftVideoPlayer != null)
                    {
                        try { leftVideoPlayer.Stop(); } catch { }
                        leftVideoPlayer.targetTexture = null;
                    }
                    if (leftPageRawImage != null) leftPageRawImage.texture = null;
                }
                else
                {
                    if (rightVideoPlayer != null)
                    {
                        try { rightVideoPlayer.Stop(); } catch { }
                        rightVideoPlayer.targetTexture = null;
                    }
                    if (rightPageRawImage != null) rightPageRawImage.texture = null;
                }

                if (raw != null) raw.gameObject.SetActive(false);
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
                        RectTransform parentRt2 = img.transform.parent as RectTransform;
                        var rt2 = img.rectTransform;

                        // If the immediate parent has an invalid rect (0 size), walk up until we find a sized ancestor
                        Transform search2 = img.transform.parent;
                        while ((parentRt2 == null || parentRt2.rect.width < 2f || parentRt2.rect.height < 2f) && search2 != null && search2.parent != null)
                        {
                            search2 = search2.parent;
                            parentRt2 = search2 as RectTransform;
                        }

                        Vector2 parentSize = parentRt2 != null ? parentRt2.rect.size : rt2.rect.size;

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

                        // compute horizontal center offset inside the parent after margins
                        float centerOffsetX = (pageMarginRight - pageMarginLeft) * 0.5f;

                        if (preserveAuthoringRect)
                        {
                            if (rt2 != null)
                            {
                                if (!originalAnchoredPositions.ContainsKey(rt2))
                                {
                                    originalAnchoredPositions[rt2] = rt2.anchoredPosition;
                                }
                                Vector2 baseAnch = originalAnchoredPositions[rt2];
                                rt2.sizeDelta = finalSize;
                                rt2.anchoredPosition = new Vector2(baseAnch.x + centerOffsetX, baseAnch.y);
                            }
                            else
                            {
                                rt2.sizeDelta = finalSize;
                            }
                        }
                        else
                        {
                            rt2.anchorMin = rt2.anchorMax = new Vector2(0.5f, 1f);
                            rt2.pivot = new Vector2(0.5f, 1f);
                            rt2.sizeDelta = finalSize;
                            rt2.anchoredPosition = new Vector2(centerOffsetX, -pageMarginTop);
                        }
                    }
                    else
                    {
                        img.gameObject.SetActive(false);
                    }
                }
            }

            if (caption != null)
            {
                caption.text = p.caption ?? "";
                caption.gameObject.SetActive(!string.IsNullOrEmpty(p.caption));

                if (captionAutoSize)
                {
                    caption.enableAutoSizing = true;
                    caption.fontSizeMin = captionFontMin;
                    caption.fontSizeMax = captionFontMax;
                }
                else
                {
                    caption.enableAutoSizing = false;
                }

                // Force mesh update so preferredHeight is accurate when we adjust container size
                caption.ForceMeshUpdate();

                if (captionMaxHeight > 0f)
                {
                    float needed = caption.preferredHeight + captionPadding;
                    float clamped = Mathf.Min(needed, captionMaxHeight);
                    RectTransform crt = caption.rectTransform;
                    Vector2 size = crt.sizeDelta;
                    // Only adjust height if different (avoid layout churn)
                    if (!Mathf.Approximately(size.y, clamped))
                    {
                        crt.sizeDelta = new Vector2(size.x, clamped);
                        // If this caption is inside a LayoutGroup, force rebuild
                        var parent = crt.parent as RectTransform;
                        if (parent != null)
                        {
                            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
                        }
                    }
                }
            }

            if (sideTitle != null)
            {
                sideTitle.text = (p.title != null) ? p.title : "";
                sideTitle.gameObject.SetActive(!string.IsNullOrEmpty(sideTitle.text));

                if (titleAutoSize)
                {
                    sideTitle.enableAutoSizing = true;
                    sideTitle.fontSizeMin = titleFontMin;
                    sideTitle.fontSizeMax = titleFontMax;
                }
                else
                {
                    sideTitle.enableAutoSizing = false;
                }

                // Force update to compute preferred size
                sideTitle.ForceMeshUpdate();

                if (titleMaxHeight > 0f)
                {
                    float neededT = sideTitle.preferredHeight + titlePadding;
                    float clampedT = Mathf.Min(neededT, titleMaxHeight);
                    RectTransform trt = sideTitle.rectTransform;
                    Vector2 tsize = trt.sizeDelta;
                    if (!Mathf.Approximately(tsize.y, clampedT))
                    {
                        trt.sizeDelta = new Vector2(tsize.x, clampedT);
                        var parentT = trt.parent as RectTransform;
                        if (parentT != null)
                        {
                            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentT);
                        }
                    }
                }
            }
        }

        ApplyPageTo(leftPageImage, leftPageRawImage, leftPageCaptionText, leftPageTitleText, leftIndex, true);
        ApplyPageTo(rightPageImage, rightPageRawImage, rightPageCaptionText, rightPageTitleText, rightIndex, false);

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
