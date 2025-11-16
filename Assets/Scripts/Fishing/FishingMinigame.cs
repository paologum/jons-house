using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
// (UI and layout APIs are used below)

/// <summary>
/// Simple implementation of a Stardew-like fishing minigame.
/// Wire the UI RectTransforms in the inspector: a panel (CanvasGroup), a fish icon RectTransform that moves up/down,
/// a player-bar RectTransform the player controls, and a Slider for showing the progress (fill meter).
/// Start the minigame by calling StartMinigame(fish, callback).
/// </summary>
public class FishingMinigame : MonoBehaviour
{
    [Header("UI refs")]
    public CanvasGroup panel;
    public RectTransform playArea; // area in which fish and bar move (anchor stretch recommended)
    public RectTransform fishIcon;
    [Tooltip("Optional Image component attached to the fishIcon RectTransform. If set, this will be assigned the fish sprite when the minigame starts.")]
    public Image fishIconImage;
    public RectTransform playerBar;
    public Slider progressBar;

    [Header("Gameplay")]
    [Tooltip("Base duration for the minigame in seconds (scaled by fish.minigameDifficulty)")]
    public float baseDuration = 8f;
    [Tooltip("How quickly the fish target moves (scaled by difficulty)")]
    public float baseFishSpeed = 1.6f;
    [Tooltip("Player bar move speed (units/sec relative to playArea height)")]
    public float playerMoveSpeed = 2.5f;
    [Tooltip("Gravity force applied to the player bar (normalized units/sec^2). Higher = bar falls faster when not pressing input")]
    public float playerGravity = 1.6f;
    [Tooltip("Upward force applied while holding input (normalized units/sec^2)")]
    public float playerInputForce = 3.6f;
    [Tooltip("Damping applied to player bar velocity each frame (0-1, 1=no damping)")]
    [Range(0.8f, 1f)]
    public float playerDamping = 0.98f;
    [Tooltip("How close the player bar must be to the fish to gain fill (0-1 normalized play area)")]
    public float successThreshold = 0.15f;
    [Header("Balance")]
    [Tooltip("Multiplier applied to fill gain when overlapping (higher = fills faster)")]
    public float gainMultiplier = 1.0f;
    [Tooltip("Multiplier applied to fill loss when not overlapping (higher = loses faster)")]
    public float lossMultiplier = 0.6f;

    bool _running;
    Action<bool> _onComplete;

    // normalized positions (0..1) where 0 = bottom, 1 = top within playArea
    float _playerPos = 0.5f;
    float _fishPos = 0.5f;
    float _playerVel = 0f;

    void Awake()
    {
        HidePanelImmediate();
    }

    void Update()
    {
        // allow keyboard control when running
        if (!_running)
            return;

        // Support both axis and explicit key presses. Axis may be configured differently per project.
        float axis = Input.GetAxisRaw("Vertical");
        bool upKey = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);

        // We ignore any downward input: player can only apply upward force. Gravity handles downward motion.
        float input = 0f;
        if (axis > 0.01f) input = axis; // ignore negative axis
        if (upKey) input += 1f;

        // Apply forces: upward input applies upward force, gravity pulls down (no manual down force)
        float appliedUp = Mathf.Max(0f, input);

        // Upward force increases velocity upwards (positive = up)
        _playerVel += (appliedUp * playerInputForce - playerGravity) * Time.deltaTime;
        // integrate position
        _playerPos += _playerVel * Time.deltaTime;
        // clamp and bounce lightly
        if (_playerPos < 0f)
        {
            _playerPos = 0f;
            _playerVel = 0f;
        }
        else if (_playerPos > 1f)
        {
            _playerPos = 1f;
            _playerVel = 0f;
        }

        // damping
        _playerVel *= playerDamping;
        UpdatePlayerBarPosition();
    }

    /// <summary>
    /// Starts the minigame for a fish and invokes callback with true=success, false=fail.
    /// </summary>
    public void StartMinigame(FishData fish, Action<bool> onComplete)
    {
        if (fish == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        if (_running)
            return;

        _onComplete = onComplete;
        ShowPanel();
        // Ensure layout is updated so playArea.rect has a valid size before positioning elements
        Canvas.ForceUpdateCanvases();
        if (playArea != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(playArea);

        // Assign fish sprite if available
        if (fishIconImage != null && fish.icon != null)
        {
            fishIconImage.sprite = fish.icon;
            fishIconImage.enabled = true;
        }
        StartCoroutine(RunMinigameCoroutine(fish));
    }

    IEnumerator RunMinigameCoroutine(FishData fish)
    {
        _running = true;
        // Start the progress partially filled so the player has a chance (not an immediate fail)
        progressBar.value = 0.4f;
        _playerPos = 0.5f;
        _fishPos = 0.5f;
        _playerVel = 0f;
        UpdatePlayerBarPosition();
        UpdateFishPosition();

        float duration = Mathf.Max(1f, baseDuration / Mathf.Max(0.1f, fish.minigameDifficulty));
        float elapsed = 0f;
        float fill = 0.4f;

        float fishSpeed = baseFishSpeed * fish.minigameDifficulty;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // move fish target up/down using a sin wave + small random jitter
            _fishPos = 0.5f + Mathf.Sin(elapsed * fishSpeed) * 0.45f;
            _fishPos = Mathf.Clamp01(_fishPos + (Mathf.PerlinNoise(elapsed * 0.7f, 0) - 0.5f) * 0.06f);
            UpdateFishPosition();

            // check overlap with player bar using normalized sizes so UI scale/height is considered
            float distance = Mathf.Abs(_playerPos - _fishPos);

            // compute effective threshold: base threshold plus half-heights of player bar and fish icon (normalized)
            float effectiveThreshold = successThreshold;
            if (playArea != null && playerBar != null && fishIcon != null)
            {
                float height = playArea.rect.height;
                if (height > 0f)
                {
                    float playerHalf = (playerBar.rect.height * 0.5f) / height;
                    float fishHalf = (fishIcon.rect.height * 0.5f) / height;
                    effectiveThreshold += (playerHalf + fishHalf);
                }
            }

            // overlap ratio: 0 = no overlap (distance >= effectiveThreshold), 1 = perfect center alignment (distance=0)
            float overlapRatio = 0f;
            if (effectiveThreshold > 0f)
                overlapRatio = Mathf.Clamp01((effectiveThreshold - distance) / effectiveThreshold);

            // Fill gain scales with how well the bar overlaps the fish. Loss scales with how far away it is.
            float gain = overlapRatio * Time.deltaTime * (1f / duration) * gainMultiplier;
            float loss = (1f - overlapRatio) * Time.deltaTime * (1f / duration) * lossMultiplier;

            fill += gain;
            fill -= loss;

            progressBar.value = Mathf.Clamp01(fill);

            if (fill >= 1f)
            {
                // success
                _running = false;
                HidePanel();
                _onComplete?.Invoke(true);
                yield break;
            }

            if (fill <= 0f)
            {
                // fish escapes immediately when progress depletes
                _running = false;
                HidePanel();
                _onComplete?.Invoke(false);
                yield break;
            }

            yield return null;
        }

        // time over
        _running = false;
        HidePanel();
        _onComplete?.Invoke(false);
    }

    void UpdateFishPosition()
    {
        if (playArea == null || fishIcon == null)
            return;

        float height = playArea.rect.height;
        float y = Mathf.Lerp(-height / 2f, height / 2f, _fishPos);
        fishIcon.anchoredPosition = new Vector2(fishIcon.anchoredPosition.x, y);
    }

    void UpdatePlayerBarPosition()
    {
        if (playArea == null || playerBar == null)
            return;

        float height = playArea.rect.height;
        float y = Mathf.Lerp(-height / 2f, height / 2f, _playerPos);
        playerBar.anchoredPosition = new Vector2(playerBar.anchoredPosition.x, y);
    }

    void ShowPanel()
    {
        if (panel != null)
        {
            panel.alpha = 1f;
            panel.interactable = true;
            panel.blocksRaycasts = true;
        }
    }

    void HidePanel()
    {
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
    }

    void HidePanelImmediate()
    {
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Allows external code (tests/UI) to set the normalized player bar position (0..1).
    /// </summary>
    public void SetPlayerBarNormalized(float normalized)
    {
        _playerPos = Mathf.Clamp01(normalized);
        UpdatePlayerBarPosition();
    }
}
