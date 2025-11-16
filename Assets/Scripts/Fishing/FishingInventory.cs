using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

/// <summary>
/// Singleton inventory of caught fish. Persists to a JSON file under Application.persistentDataPath.
/// Automatically created on first access if not present in the scene.
/// </summary>
public class FishingInventory : MonoBehaviour
{
    const string FILE_NAME = "fishing_inventory.json";

    static FishingInventory _instance;
    public static FishingInventory Instance
    {
        get
        {
            if (_instance == null)
            {
                // Do NOT auto-create the singleton GameObject at runtime to avoid editor "not cleaned up" warnings.
                // Prefer creating and wiring a FishingInventory in the scene (or call FindInstance() explicitly).
                _instance = FindObjectOfType<FishingInventory>();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Helper to find or create the inventory explicitly. Use this only if you explicitly want a runtime-created instance.
    /// </summary>
    public static FishingInventory FindOrCreateInstance()
    {
        var inst = Instance;
        if (inst != null) return inst;
        var go = new GameObject("FishingInventory");
        inst = go.AddComponent<FishingInventory>();
        DontDestroyOnLoad(go);
        return inst;
    }

    [SerializeField]
    List<CaughtFish> _caught = new List<CaughtFish>();

    public IReadOnlyList<CaughtFish> Caught => _caught.AsReadOnly();

    public event Action OnInventoryChanged;

    string GetPath() => Path.Combine(Application.persistentDataPath, FILE_NAME);

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    void OnApplicationQuit()
    {
        Save();
    }

    public void AddCaughtFish(FishData fish)
    {
        if (fish == null) return;
        var entry = new CaughtFish(fish.fishName, fish.minigameDifficulty);
        _caught.Add(entry);
        Save();
        OnInventoryChanged?.Invoke();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _caught.Count) return;
        _caught.RemoveAt(index);
        Save();
        OnInventoryChanged?.Invoke();
    }

    public void Clear()
    {
        _caught.Clear();
        Save();
        OnInventoryChanged?.Invoke();
    }

    void Save()
    {
        try
        {
            var json = JsonUtility.ToJson(new Wrapper { items = _caught }, true);
            File.WriteAllText(GetPath(), json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save fishing inventory: {e}");
        }
    }

    void Load()
    {
        try
        {
            var path = GetPath();
            if (!File.Exists(path))
            {
                _caught = new List<CaughtFish>();
                return;
            }
            var json = File.ReadAllText(path);
            var wrapper = JsonUtility.FromJson<Wrapper>(json);
            _caught = wrapper?.items ?? new List<CaughtFish>();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load fishing inventory: {e}");
            _caught = new List<CaughtFish>();
        }
    }

    [Serializable]
    class Wrapper { public List<CaughtFish> items; }
}
