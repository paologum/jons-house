using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small helper component that holds a list of FishData and lets callers request a random fish by weight.
/// Attach to a GameObject in the scene (for example FishingManager) and populate the list with ScriptableObjects.
/// </summary>
public class FishDatabase : MonoBehaviour
{
    public List<FishData> fishes = new List<FishData>();

    /// <summary>
    /// Returns a random fish chosen by selectionWeight. Returns null if list empty.
    /// </summary>
    public FishData GetRandomByWeight()
    {
        if (fishes == null || fishes.Count == 0)
            return null;

        float total = 0f;
        foreach (var f in fishes)
            total += Mathf.Max(0f, f.selectionWeight);

        if (total <= 0f)
            return fishes[Random.Range(0, fishes.Count)];

        float pick = Random.Range(0f, total);
        float soFar = 0f;
        foreach (var f in fishes)
        {
            soFar += Mathf.Max(0f, f.selectionWeight);
            if (pick <= soFar)
                return f;
        }

        return fishes[fishes.Count - 1];
    }
}
