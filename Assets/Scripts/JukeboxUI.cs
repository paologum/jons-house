using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// UI manager for the Jukebox. Create a Canvas panel in the scene with a content RectTransform
/// (e.g. a VerticalLayoutGroup) and a Button prefab that contains an Image (banner) and a TextMeshProUGUI.
/// Assign those in the inspector and this component will populate the list from the Jukebox.tracks array.
/// </summary>
public class JukeboxUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Root panel GameObject that will be shown/hidden when the jukebox is opened.")]
    public GameObject panel;

    [Tooltip("Parent RectTransform where banner buttons will be instantiated (should have a VerticalLayoutGroup).")]
    public RectTransform contentParent;

    [Tooltip("A Button prefab to instantiate for each track. The prefab should contain an Image and a TextMeshProUGUI.")]
    public Button bannerPrefab;

    // Hint display is centralized in `HintManager`. Configure and assign the hint GameObject
    // on the `HintManager` in your scene instead of per-UI fields.

    [Header("Visuals")]
    [Tooltip("Scale applied to a banner when hovered/selected.")]
    public float hoverScale = 1.08f;

    private Jukebox jukebox;
    // Expose current jukebox target so other systems can check whether the UI is
    // currently showing a particular jukebox instance.
    public Jukebox CurrentJukebox => jukebox;
    private List<Button> banners = new List<Button>();
    // for Move axis edge detection
    private float _lastMoveY = 0f;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        // Hint display is centralized in HintManager. The manager should be configured in the scene
        // (assign the hint GameObject on the HintManager) rather than having each UI register it.
    }

    /// <summary>
    /// Show the jukebox UI and populate track banners from the supplied Jukebox component.
    /// </summary>
    public void Show(Jukebox target)
    {
        if (target == null) return;
        jukebox = target;
        BuildList();
        if (panel != null) panel.SetActive(true);
        // Enable UI action map so UI navigation and Cancel are routed to UI bindings
        if (InputManager.Instance != null) InputManager.Instance.EnableUI();
        // pause game while UI is open
        if (Application.isPlaying) Time.timeScale = 0f;
        // select first banner for gamepad navigation
        if (banners.Count > 0)
        {
            EventSystem.current.SetSelectedGameObject(banners[0].gameObject);
        }
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        // Restore gameplay action map so gameplay controls resume
        if (InputManager.Instance != null) InputManager.Instance.EnableGameplay();
        if (Application.isPlaying) Time.timeScale = 1f;
        // clear created banners
        foreach (var b in banners)
        {
            if (b != null) Destroy(b.gameObject);
        }
        banners.Clear();
    }

    // Hint display is handled exclusively by the central `HintManager`.

    /// <summary>
    /// Overload to show the jukebox UI and simultaneously show an interact hint for the
    /// named object (works with or without HintManager).
    /// </summary>
    public void Show(Jukebox target, string objectName)
    {
        Show(target);
        if (!string.IsNullOrEmpty(objectName))
        {
            if (HintManager.Instance != null) HintManager.Instance.ShowInteractHint(objectName);
            // If there's no HintManager present we do not attempt to manage per-UI hints.
        }
    }

    private void BuildList()
    {
        // clear old
        foreach (var b in banners)
        {
            if (b != null) Destroy(b.gameObject);
        }
        banners.Clear();

        if (jukebox == null || bannerPrefab == null || contentParent == null) return;

        var tracksField = typeof(Jukebox).GetField("tracks");
        if (tracksField == null) return;
        var tracks = tracksField.GetValue(jukebox) as System.Array;
        if (tracks == null) return;

        for (int i = 0; i < tracks.Length; i++)
        {
            var item = Instantiate(bannerPrefab, contentParent, false);
            banners.Add(item);

            // Text
            var tmp = item.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                // try to read displayName via reflection to avoid making a direct dependency on the Track type
                var trackObj = tracks.GetValue(i);
                var nameField = trackObj.GetType().GetField("displayName");
                string display = null;
                if (nameField != null) display = nameField.GetValue(trackObj) as string;
                if (string.IsNullOrEmpty(display))
                    display = $"Track {i + 1}";
                tmp.text = display;
            }

            // Button onClick -> play index
            int idx = i;
            item.onClick.RemoveAllListeners();
            item.onClick.AddListener(() => { PlayIndex(idx); });

            // Add a banner behavior to handle pointer hover
            var banner = item.gameObject.AddComponent<JukeboxBanner>();
            banner.Setup(item.gameObject, hoverScale);
        }
    }

    private void PlayIndex(int index)
    {
        if (jukebox != null)
        {
            jukebox.PlayIndex(index);
        }
        // Optionally close UI after selection
        Hide();
    }

    void Update()
    {
        // Allow closing with Escape / B (JoystickButton1)
        if (panel != null && panel.activeSelf)
        {
            // InputManager raises CancelPerformed events; Interaction handled elsewhere.
            // Poll the Gameplay Move vertical axis to navigate the list (edge-detect)
            if (InputManager.Instance != null)
            {
                // prefer UI Navigate bindings (dpad/arrow/leftstick) when available
                Vector2 nav = InputManager.Instance.ReadUINavigate();
                float y = nav.y;
                float dead = 0.5f;
                // rising edge up
                if (y > dead && _lastMoveY <= dead)
                {
                    MoveSelection(-1); // up
                }
                // rising edge down
                else if (y < -dead && _lastMoveY >= -dead)
                {
                    MoveSelection(1); // down
                }
                _lastMoveY = y;
            }
        }
    }

    void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.CancelPerformed += OnCancelPerformed;
            InputManager.Instance.OpenJukeboxPerformed += OnOpenJukeboxPerformed;
            InputManager.Instance.SubmitPerformed += OnSubmitPerformed;
        }
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.CancelPerformed -= OnCancelPerformed;
            InputManager.Instance.OpenJukeboxPerformed -= OnOpenJukeboxPerformed;
            InputManager.Instance.SubmitPerformed -= OnSubmitPerformed;
        }
    }

    private void OnSubmitPerformed()
    {
        if (panel == null || !panel.activeSelf) return;

        var es = EventSystem.current;
        var selected = es != null ? es.currentSelectedGameObject : null;
        if (selected == null)
        {
            // fallback to first banner
            if (banners != null && banners.Count > 0 && banners[0] != null)
            {
                banners[0].onClick.Invoke();
            }
            return;
        }

        var btn = selected.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.Invoke();
        }
    }

    private void MoveSelection(int direction)
    {
        if (banners == null || banners.Count == 0) return;

        var es = EventSystem.current;
        GameObject current = es != null ? es.currentSelectedGameObject : null;

        int currentIndex = -1;
        if (current != null)
        {
            for (int i = 0; i < banners.Count; i++)
            {
                if (banners[i] != null && banners[i].gameObject == current)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        int newIndex;
        if (currentIndex == -1)
        {
            newIndex = direction > 0 ? 0 : banners.Count - 1;
        }
        else
        {
            newIndex = Mathf.Clamp(currentIndex + direction, 0, banners.Count - 1);
        }

        if (banners[newIndex] != null && es != null)
        {
            es.SetSelectedGameObject(banners[newIndex].gameObject);
        }
    }

    private void OnOpenJukeboxPerformed()
    {
        // Toggle the jukebox UI. If closed, attempt to find a Jukebox in scene to target.
        if (panel != null && panel.activeSelf)
        {
            Hide();
            return;
        }

        // If we already have a cached jukebox target, show it. Otherwise find the first in scene.
        if (jukebox != null)
        {
            Show(jukebox);
            return;
        }

        // Try to find any Jukebox in the scene and open the UI for it.
        var found = FindObjectOfType<Jukebox>();
        if (found != null)
        {
            Show(found);
        }
        else
        {
            Debug.Log("JukeboxUI: OpenJukebox input received but no Jukebox found in scene.", this);
        }
    }

    private void OnCancelPerformed()
    {
        Debug.Log($"JukeboxUI: OnCancelPerformed invoked; panelActive={panel != null && panel.activeSelf}", this);
        if (panel != null && panel.activeSelf) Hide();
    }
}
