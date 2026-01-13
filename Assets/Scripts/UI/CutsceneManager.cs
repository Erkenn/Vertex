using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject cutsceneCanvas;
    public RawImage videoImage;
    public VideoPlayer videoPlayer;

    private System.Action onCutsceneFinished;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (cutsceneCanvas != null)
            cutsceneCanvas.SetActive(false);
    }

    public IEnumerator PlayCutscene(VideoClip videoClip, System.Action onComplete = null)
    {
        onCutsceneFinished = onComplete;

        Time.timeScale = 0f;

        cutsceneCanvas.SetActive(true);

        videoPlayer.clip = videoClip;
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource); // канал 0
        }
        else
        {
            Debug.LogWarning("🔇 AudioSource не найден! Звук из видео не будет воспроизведён.");
        }

        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;

        videoPlayer.Play();

        while (videoPlayer.isPlaying)
            yield return null;

        FinishCutscene();
    }

    void FinishCutscene()
    {
        cutsceneCanvas.SetActive(false);
        Time.timeScale = 1f;
        onCutsceneFinished?.Invoke();
        onCutsceneFinished = null;
    }

    public static void DestroyInstance()
    {
        if (Instance != null)
        {
            Debug.Log("🗑️ CutsceneManager уничтожен после интро");
            Destroy(Instance.gameObject);
        }
    }
}