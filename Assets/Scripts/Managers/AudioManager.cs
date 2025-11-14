using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("=== АУДИО НАСТРОЙКИ ===")]
    public float masterVolume = 1f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 0.9f;
    public bool musicEnabled = true;
    public bool sfxEnabled = true;

    [Header("=== АУДИО ИСТОЧНИКИ ===")]
    public AudioSource musicSource;
    public AudioSource ambientSource;
    public AudioSource uiSource;

    [Header("=== МУЗЫКА ===")]
    public AudioClip mainMenuMusic;
    public AudioClip[] levelMusic;
    public AudioClip bossMusic;
    public AudioClip victoryMusic;
    public AudioClip gameOverMusic;

    [Header("=== SFX - ИГРОК ===")]
    public AudioClip playerMove;
    public AudioClip playerHack;
    public AudioClip playerShield;
    public AudioClip playerDamage;
    public AudioClip playerDeath;

    [Header("=== SFX - ВРАГИ ===")]
    public AudioClip enemySpawn;
    public AudioClip enemyDeath;
    public AudioClip turretShoot;
    public AudioClip scannerAlert;

    [Header("=== SFX - СИСТЕМЫ ===")]
    public AudioClip dataPacketCollect;
    public AudioClip uiClick;
    public AudioClip uiHover;
    public AudioClip levelComplete;

    private Dictionary<string, AudioClip> sfxLibrary = new Dictionary<string, AudioClip>();
    private Coroutine musicFadeCoroutine;
    private AudioClip currentMusic;
    private float originalMusicVolume;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSystem();
            Debug.Log("🎵 AudioManager инициализирован");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeAudioSystem()
    {
        // Инициализация аудио источников если они не назначены
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();
        if (ambientSource == null)
            ambientSource = gameObject.AddComponent<AudioSource>();
        if (uiSource == null)
            uiSource = gameObject.AddComponent<AudioSource>();

        // Настройка аудио источников
        musicSource.loop = true;
        musicSource.volume = 0f; // Начинаем с 0 для плавного появления
        ambientSource.loop = true;
        ambientSource.volume = 0.3f;
        uiSource.loop = false;
        uiSource.volume = 1f;

        originalMusicVolume = musicVolume;

        // Загрузка сохраненных настроек
        LoadAudioSettings();

        // Заполнение библиотеки звуков
        PopulateSFXLibrary();

        // Запуск фоновой музыки
        PlayMainMenuMusic();
    }

    void PopulateSFXLibrary()
    {
        // Игрок
        AddSFXToLibrary("PlayerHack", playerHack);
        AddSFXToLibrary("PlayerShield", playerShield);
        AddSFXToLibrary("PlayerDamage", playerDamage);
        AddSFXToLibrary("PlayerDeath", playerDeath);
        AddSFXToLibrary("PlayerMove", playerMove);

        // Враги
        AddSFXToLibrary("EnemySpawn", enemySpawn);
        AddSFXToLibrary("EnemyDeath", enemyDeath);
        AddSFXToLibrary("TurretShoot", turretShoot);
        AddSFXToLibrary("ScannerAlert", scannerAlert);

        // Системы
        AddSFXToLibrary("DataPacketCollect", dataPacketCollect);
        AddSFXToLibrary("UIClick", uiClick);
        AddSFXToLibrary("UIHover", uiHover);
        AddSFXToLibrary("LevelComplete", levelComplete);
    }

    void AddSFXToLibrary(string key, AudioClip clip)
    {
        if (clip != null && !sfxLibrary.ContainsKey(key))
        {
            sfxLibrary.Add(key, clip);
        }
    }

    // === СИСТЕМА МУЗЫКИ ===

    public void PlayMainMenuMusic()
    {
        PlayMusic(mainMenuMusic, 1f);
    }

    public void PlayLevelMusic(int levelIndex)
    {
        if (levelMusic != null && levelIndex >= 0 && levelIndex < levelMusic.Length)
        {
            PlayMusic(levelMusic[levelIndex], 0.8f);
        }
        else
        {
            // Музыка по умолчанию для уровня
            PlayMusic(levelMusic[0], 0.8f);
        }
    }

    public void PlayBossMusic()
    {
        PlayMusic(bossMusic, 1f);
    }

    public void PlayVictoryMusic()
    {
        PlayMusic(victoryMusic, 1f);
    }

    public void PlayGameOverMusic()
    {
        PlayMusic(gameOverMusic, 1f);
    }

    void PlayMusic(AudioClip music, float volumeMultiplier = 1f)
    {
        if (!musicEnabled || music == null) return;

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        musicFadeCoroutine = StartCoroutine(FadeMusic(music, volumeMultiplier));
    }

    IEnumerator FadeMusic(AudioClip newMusic, float volumeMultiplier)
    {
        // Плавное затухание текущей музыки
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }
            musicSource.volume = 0f;
        }

        // Смена трека
        currentMusic = newMusic;
        musicSource.clip = newMusic;
        musicSource.Play();

        // Плавное появление новой музыки
        float targetVolume = musicVolume * volumeMultiplier;
        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / 2f);
            yield return null;
        }
        musicSource.volume = targetVolume;
    }

    public void StopMusic()
    {
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        StartCoroutine(FadeOutMusic());
    }

    IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        musicSource.Stop();
        musicSource.volume = 0f;
    }

    // === СИСТЕМА SFX ===

    public void PlaySFX(string soundName, float volumeScale = 1f, float pitch = 1f)
    {
        if (!sfxEnabled || !sfxLibrary.ContainsKey(soundName)) return;

        AudioClip clip = sfxLibrary[soundName];

        // Создаем временный AudioSource для каждого SFX
        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = sfxVolume * volumeScale * masterVolume;
        tempSource.pitch = pitch;
        tempSource.Play();

        // Автоматическое удаление после воспроизведения
        Destroy(tempSource, clip.length + 0.1f);
    }

    public void PlaySFXAtPosition(string soundName, Vector3 position, float volumeScale = 1f)
    {
        if (!sfxEnabled || !sfxLibrary.ContainsKey(soundName)) return;

        AudioClip clip = sfxLibrary[soundName];
        AudioSource.PlayClipAtPoint(clip, position, sfxVolume * volumeScale * masterVolume);
    }

    public void PlayUISound(string soundName)
    {
        if (!sfxEnabled || !sfxLibrary.ContainsKey(soundName)) return;

        AudioClip clip = sfxLibrary[soundName];
        uiSource.PlayOneShot(clip, sfxVolume * masterVolume);
    }

    // === СИСТЕМА НАСТРОЕК ===

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
        SaveAudioSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateMusicVolume();
        SaveAudioSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        SaveAudioSettings();
    }

    public void ToggleMusic(bool enabled)
    {
        musicEnabled = enabled;
        if (!enabled)
        {
            StopMusic();
        }
        else if (currentMusic != null)
        {
            musicSource.Play();
        }
        SaveAudioSettings();
    }

    public void ToggleSFX(bool enabled)
    {
        sfxEnabled = enabled;
        SaveAudioSettings();
    }

    void UpdateAllVolumes()
    {
        UpdateMusicVolume();
        // SFX обновляются при каждом воспроизведении
    }

    void UpdateMusicVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
        if (ambientSource != null)
        {
            ambientSource.volume = 0.3f * masterVolume;
        }
    }

    // === СИСТЕМА СОХРАНЕНИЯ ===

    void LoadAudioSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.9f);
        musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;

        UpdateAllVolumes();
    }

    void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("MusicEnabled", musicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("SFXEnabled", sfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    // === УПРАВЛЕНИЕ АМБИЕНТНЫМИ ЗВУКАМИ ===

    public void PlayAmbientSound(AudioClip ambientClip)
    {
        if (ambientSource != null && ambientClip != null)
        {
            ambientSource.clip = ambientClip;
            ambientSource.Play();
        }
    }

    public void StopAmbientSound()
    {
        if (ambientSource != null)
        {
            ambientSource.Stop();
        }
    }

    // === БЫСТРЫЕ МЕТОДЫ ДЛЯ ЧАСТЫХ ЗВУКОВ ===

    public void PlayHackSound() => PlaySFX("PlayerHack");
    public void PlayShieldSound() => PlaySFX("PlayerShield");
    public void PlayDamageSound() => PlaySFX("PlayerDamage");
    public void PlayDataCollectSound() => PlaySFX("DataPacketCollect");
    public void PlayEnemyDeathSound() => PlaySFX("EnemyDeath");
    public void PlayUIClick() => PlayUISound("UIClick");
    public void PlayUIHover() => PlayUISound("UIHover");

    void OnDestroy()
    {
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);
    }
}