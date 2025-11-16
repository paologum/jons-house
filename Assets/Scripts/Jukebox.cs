using UnityEngine;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// Jukebox that supports both VideoClip (mp4) and AudioClip (mp3/wav/ogg) tracks.
/// Create tracks in the Inspector and the jukebox will play Video audio or AudioClips as appropriate.
/// </summary>
public class Jukebox : MonoBehaviour
{
    [System.Serializable]
    public class Track
    {
        [Tooltip("Optional VideoClip (mp4). If set, the VideoPlayer will be used and its audio routed to the AudioSource.")]
        public VideoClip video;

        [Tooltip("Optional AudioClip (mp3/wav/ogg). Used when no VideoClip is set for this track.")]
        public AudioClip audio;

        [Tooltip("Optional display name for the track.")]
        public string displayName;
    }

    [Tooltip("List of tracks; each entry may contain either a VideoClip or an AudioClip (or both). VideoClip takes priority if present.")]
    public Track[] tracks;

    [Tooltip("Start playing on Awake if true.")]
    public bool playOnStart = true;

    [Tooltip("If true picks a random track when starting and between tracks.")]
    public bool randomize = false;

    [Tooltip("Loop the current track when finished.")]
    public bool loop = false;

    private VideoPlayer vp;
    private AudioSource audioSource;
    private int currentIndex = 0;
    private Coroutine audioWaitCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        vp = GetComponent<VideoPlayer>();
        if (vp == null)
        {
            vp = gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
        }

        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.SetTargetAudioSource(0, audioSource);
        vp.renderMode = VideoRenderMode.APIOnly; // default: don't render video unless user attaches a target
        vp.isLooping = loop;
        vp.skipOnDrop = true;

        vp.loopPointReached += OnVideoFinished;
    }

    void Start()
    {
        if (playOnStart && tracks != null && tracks.Length > 0)
        {
            if (randomize)
                currentIndex = Random.Range(0, tracks.Length);
            PlayIndex(currentIndex);
        }
    }

    void OnEnable()
    {
        // Jukebox no longer listens to global Next/Prev input; UI is responsible for changing tracks.
    }

    void OnDisable()
    {
        // No subscriptions to remove.
    }

    private void OnDestroy()
    {
        if (vp != null) vp.loopPointReached -= OnVideoFinished;
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        if (loop) return;
        if (randomize) PlayRandom(); else PlayNext();
    }

    private IEnumerator WaitForAudioAndFinish(AudioClip clip)
    {
        if (clip == null) yield break;
        yield return new WaitForSecondsRealtime(clip.length);
        if (loop) yield break;
        if (randomize) PlayRandom(); else PlayNext();
    }

    public void PlayIndex(int index)
    {
        StopCurrentPlayback();

        if (tracks == null || tracks.Length == 0) return;
        if (index < 0 || index >= tracks.Length) return;
        currentIndex = index;

        var t = tracks[currentIndex];
        if (t == null)
            return;

        // Prefer VideoClip if present
        if (t.video != null)
        {
            vp.clip = t.video;
            try
            {
                vp.Prepare();
                vp.prepareCompleted += OnVideoPreparedThenPlay;
            }
            catch { try { vp.Play(); } catch { } }
        }
        else if (t.audio != null)
        {
            audioSource.clip = t.audio;
            audioSource.Play();
            // start coroutine to wait for clip length
            audioWaitCoroutine = StartCoroutine(WaitForAudioAndFinish(t.audio));
        }
        else
        {
            Debug.LogWarning("Jukebox: track contains no video or audio.");
        }
    }

    private void OnVideoPreparedThenPlay(VideoPlayer src)
    {
        try { src.Play(); } catch { }
        try { src.prepareCompleted -= OnVideoPreparedThenPlay; } catch { }
    }

    private void StopCurrentPlayback()
    {
        // stop video
        try { if (vp != null && vp.isPlaying) vp.Stop(); } catch { }
        // stop audio
        try { if (audioSource != null && audioSource.isPlaying) audioSource.Stop(); } catch { }
        // stop coroutines
        if (audioWaitCoroutine != null) { StopCoroutine(audioWaitCoroutine); audioWaitCoroutine = null; }
    }

    public void PlayNext()
    {
        if (tracks == null || tracks.Length == 0) return;
        currentIndex = (currentIndex + 1) % tracks.Length;
        PlayIndex(currentIndex);
    }

    public void PlayPrev()
    {
        if (tracks == null || tracks.Length == 0) return;
        currentIndex = (currentIndex - 1 + tracks.Length) % tracks.Length;
        PlayIndex(currentIndex);
    }

    public void PlayRandom()
    {
        if (tracks == null || tracks.Length == 0) return;
        currentIndex = Random.Range(0, tracks.Length);
        PlayIndex(currentIndex);
    }

    public void Stop()
    {
        StopCurrentPlayback();
    }

    private void OnRandomizeToggled()
    {
        randomize = !randomize;
    }

    void Update()
    {
        // Input is handled via InputManager events; no legacy polling here.

        // For audio-only playback we don't rely on loopPointReached; ensure we handle playback finish if needed
        if (!loop && audioSource != null && !audioSource.isPlaying && audioWaitCoroutine == null && tracks != null && tracks.Length > 0)
        {
            // If current track was an audio clip and it finished, start next
            var t = tracks[currentIndex];
            if (t != null && t.audio != null && t.video == null)
            {
                if (randomize) PlayRandom(); else PlayNext();
            }
        }
    }
}
