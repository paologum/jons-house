using UnityEngine;

/// <summary>
/// Lightweight compatibility shim retained temporarily so old code that still
/// references InputHelper will compile. All calls should be migrated to
/// InputManager; this file can be removed once migration is complete.
/// </summary>
public static class InputHelper
{
    public static bool IsNextPressed(ref bool _)
    {
        return false;
    }

    public static bool IsPrevPressed(ref bool _)
    {
        return false;
    }

    public static bool IsNextPressed() => false;
    public static bool IsPrevPressed() => false;
    public static bool IsRandomizeTogglePressed() => false;
    public static bool IsInteractPressed() => false;
    public static bool IsCancelPressed() => false;
    public static Vector2 ReadMove() => Vector2.zero;
}
