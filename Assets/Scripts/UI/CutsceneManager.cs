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
        if (cutsceneCanvas == null || videoPlayer == null)
        {
            Debug.LogError("❌ CutsceneManager не настроен: cutsceneCanvas или videoPlayer = null");
            onComplete?.Invoke();
            yield break;
        }

        onCutsceneFinished = onComplete;

        Time.timeScale = 0f;

        // 🔒 ДОБАВЬТЕ ПРОВЕРКУ НА УНИЧТОЖЕННЫЙ ОБЪЕКТ
        if (cutsceneCanvas == null)
        {
            Debug.LogError("❌ cutsceneCanvas уничтожен!");
            Time.timeScale = 1f;
            onComplete?.Invoke();
            yield break;
        }

        cutsceneCanvas.SetActive(true); // ← теперь безопасно

        videoPlayer.clip = videoClip;
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }
        else
        {
            Debug.LogWarning("🔇 AudioSource не найден!");
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