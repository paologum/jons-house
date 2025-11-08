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
    
    [TextArea(4, 8)]
    public string story;
    
    public Sprite image;
    
    [Header("Visual Settings")]
    public Color glowColor = Color.yellow;
}
