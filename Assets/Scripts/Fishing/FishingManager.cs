using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the high-level fishing flow: cast -> wait for bite -> pick fish -> notify hooked.
/// The actual minigame is separate (FishingMinigame). Subscribe to OnFishHooked to show the minigame.
/// </summary>
public class FishingManager : MonoBehaviour
{
    [Header("Refs")]
    public FishDatabase fishDatabase;

    [Header("Timing")]
    [Tooltip("Min and max time to wait for a bite after casting (seconds)")]
    public Vector2 biteWaitRange = new Vector2(1.5f, 6f);

    [Tooltip("If true, manager will automatically simulate a bite for testing when StartFishing is called.")]
    public bool autoSimulateBite = true;

    // Events
    public event Action<FishData> OnFishHooked;
    public event Action OnNoBite;
    public event Action<FishData> OnFishCaught;
    public event Action<FishData> OnFishEscaped;

    Coroutine _fishingCoroutine;

    /// <summary>
    /// Call to start the fishing process (player cast). This will schedule a bite.
    /// </summary>
    public void StartFishing()
    {
        if (_fishingCoroutine != null)
            StopCoroutine(_fishingCoroutine);
        _fishingCoroutine = StartCoroutine(FishingRoutine());
    }

    /// <summary>
    /// Stops any ongoing fishing attempt.
    /// </summary>
    public void CancelFishing()
    {
        if (_fishingCoroutine != null)
        {
            StopCoroutine(_fishingCoroutine);
            _fishingCoroutine = null;
        }
    }

    IEnumerator FishingRoutine()
    {
        // simulate waiting for a bite
        float wait = UnityEngine.Random.Range(biteWaitRange.x, biteWaitRange.y);
        yield return new WaitForSeconds(wait);

        // pick a fish
        var fish = fishDatabase != null ? fishDatabase.GetRandomByWeight() : null;
        if (fish == null)
        {
            OnNoBite?.Invoke();
            _fishingCoroutine = null;
            yield break;
        }

        // determine whether it actually hooks (pre-minigame check)
        float roll = UnityEngine.Random.value;
        if (roll <= fish.baseCatchRate || autoSimulateBite)
        {
            OnFishHooked?.Invoke(fish);
        }
        else
        {
            OnNoBite?.Invoke();
        }

        _fishingCoroutine = null;
    }

    /// <summary>
    /// Called by the minigame when it finishes. If success==true, the fish is caught.
    /// If false, the fish escapes (you may want to add a small chance to still catch on failure).
    /// </summary>
    public void OnMinigameResult(bool success, FishData fish)
    {
        if (fish == null)
            return;

        if (success)
        {
            OnFishCaught?.Invoke(fish);
        }
        else
        {
            OnFishEscaped?.Invoke(fish);
        }
    }
}
