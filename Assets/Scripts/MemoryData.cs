using UnityEngine;

/// <summary>
/// ScriptableObject to store memory data for interactable objects.
/// This allows easy configuration of memories in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewMemory", menuName = "Jon's House/Memory Data")]
public class MemoryData : ScriptableObject
{
    [Header("Memory Information")]
    public string title;

    [System.Serializable]
    public class MemoryPage
    {
        public Sprite image;
        [TextArea(2, 6)]
        public string caption;
    }

    [Header("Pages")]
    [Tooltip("Add one or more pages. Each page has an image and optional caption.")]
    public MemoryPage[] pages;

    [Header("Visual Settings")]
    public Color glowColor = Color.yellow;

    public MemoryPage[] GetPages()
    {
        return pages ?? new MemoryPage[0];
    }
}
