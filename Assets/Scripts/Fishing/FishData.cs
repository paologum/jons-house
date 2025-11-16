using UnityEngine;

[CreateAssetMenu(menuName = "Fishing/FishData", fileName = "NewFish")]
public class FishData : ScriptableObject
{
    [Header("Identity")]
    public string fishName = "New Fish";
    public Sprite icon;

    [Header("Selection")]
    [Tooltip("Weight used when randomly selecting a fish from a pool (higher = more common)")]
    public float selectionWeight = 1f;

    [Header("Catch / Minigame")]
    [Range(0f, 1f)]
    [Tooltip("Base chance modifier used when attempting to hook this fish (0-1). Higher makes it easier to hook/catch.")]
    public float baseCatchRate = 0.5f;

    [Tooltip("A difficulty multiplier used by the minigame; higher = harder (faster movement, less time).")]
    public float minigameDifficulty = 1f;

    [TextArea]
    public string description;
}
