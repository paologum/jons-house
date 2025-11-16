using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Simple runtime splash screen. Draws a centered label "For Jon..." using OnGUI and
/// loads the `targetSceneName` after `duration` seconds.
/// Attach this to an empty GameObject in a dedicated splash scene. No UI wiring required.
/// </summary>
public class SplashScreen : MonoBehaviour
{
    [Tooltip("Seconds to show the splash before loading the next scene.")]
    public float duration = 2.5f;

    [Tooltip("Name of the scene to load after the splash. Leave empty to do nothing.")]
    public string targetSceneName = "HouseScene";

    [Tooltip("Message to display on the splash screen.")]
    public string message = "For Jon...";

    [Tooltip("Optional large font size for the splash text.")]
    public int fontSize = 48;

    private float startTime;

    void Start()
    {
        startTime = Time.time;
        if (duration > 0f && !string.IsNullOrEmpty(targetSceneName))
        {
            StartCoroutine(WaitAndLoad());
        }
    }

    private IEnumerator WaitAndLoad()
    {
        yield return new WaitForSecondsRealtime(duration);
        // Load the target scene (use single to replace splash)
        try
        {
            SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"SplashScreen: failed to load scene '{targetSceneName}': {ex.Message}");
        }
    }

    void OnGUI()
    {
        // Fullscreen centered label
        var style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;

        // Background dim
        Color old = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        // Draw the message centered
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), message, style);
    }
}
