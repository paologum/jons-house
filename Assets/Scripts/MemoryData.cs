using UnityEngine;
using UnityEngine.Video;

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
        [Tooltip("Optional small title shown above the page (e.g. a short label)")]
        public string title;
        [Header("Video (optional)")]
        [Tooltip("Optional VideoClip to play on this page instead of a static image.")]
        public VideoClip video;
        [Tooltip("If true, the video will loop while the page is visible.")]
        public bool videoLoop = false;
        [Tooltip("If true, the video will autoplay when the page is shown.")]
        public bool videoAutoplay = true;
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
