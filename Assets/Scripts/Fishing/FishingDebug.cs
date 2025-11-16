using UnityEngine;

/// <summary>
/// Small debug/test harness to exercise FishingManager and FishingMinigame from the editor/Play mode.
/// Attach an instance to a GameObject in your Fishing scene and wire the references.
/// Press 'F' to start a fishing attempt.
/// </summary>
public class FishingDebug : MonoBehaviour
{
    public FishingManager manager;
    public FishingMinigame minigame;

    void Start()
    {
        if (manager == null)
            manager = FindObjectOfType<FishingManager>();
        if (minigame == null)
            minigame = FindObjectOfType<FishingMinigame>();

        if (manager != null)
        {
            manager.OnFishHooked += OnHooked;
            manager.OnFishCaught += f => Debug.Log($"Caught: {f.fishName}");
            manager.OnFishEscaped += f => Debug.Log($"Escaped: {f.fishName}");
            manager.OnNoBite += () => Debug.Log("No bite.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("StartFishing triggered (debug)");
            manager?.StartFishing();
        }
    }

    void OnHooked(FishData fish)
    {
        Debug.Log($"Fish hooked: {fish.fishName}. Starting minigame...");
        if (minigame != null)
        {
            minigame.StartMinigame(fish, success =>
            {
                manager.OnMinigameResult(success, fish);
                // If the minigame succeeded, add to persistent inventory
                if (success)
                {
                    FishingInventory.Instance?.AddCaughtFish(fish);
                }
            });
        }
        else
        {
            // if no minigame wired, immediately treat as caught for testing
            manager.OnMinigameResult(true, fish);
            FishingInventory.Instance?.AddCaughtFish(fish);
        }
    }
}
