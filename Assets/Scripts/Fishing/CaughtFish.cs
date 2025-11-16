using System;

[Serializable]
public class CaughtFish
{
    // Basic persisted information about a caught fish. Keep this minimal and robust.
    public string fishName;
    public float minigameDifficulty;
    public string caughtAtUtcIso; // ISO timestamp for display

    public CaughtFish() { }

    public CaughtFish(string name, float difficulty)
    {
        fishName = name;
        minigameDifficulty = difficulty;
        caughtAtUtcIso = System.DateTime.UtcNow.ToString("o");
    }
}
