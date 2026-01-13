using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
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
        // Инициализация аудио источников
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (ambientSource == null) ambientSource = gameObject.AddComponent<AudioSource>();
        if (uiSource == null) uiSource = gameObject.AddComponent<AudioSource>();

        // Настройка источников
        musicSource.loop = true;
        musicSource.volume = 0f;
        ambientSource.loop = true;
        ambientSource.volume = 0.3f;
        uiSource.loop = false;
        uiSource.volume = 1f;

        // Загружаем настройки БЕЗ применения к источникам
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.9f);
        musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;

        PopulateSFXLibrary();

        // ❌ НЕ запускаем музыку здесь — только через OnSceneLoaded
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🎵 AudioManager: загружена сцена '{scene.name}'");

        if (scene.name == "MainMenu")
        {
            PlayMainMenuMusic();
        }
        else if (scene.name.StartsWith("Level_"))
        {
            string levelStr = scene.name.Replace("Level_", "");
            if (int.TryParse(levelStr, out int levelNum))
            {
                PlayLevelMusic(levelNum - 1); // Level_1 → index 0
            }
            else
            {
                PlayLevelMusic(0);
            }
        }
        else if (scene.name == "Tutorial")
        {
            PlayLevelMusic(0);
        }
    }

    void PopulateSFXLibrary()
    {
        AddSFXToLibrary("PlayerHack", playerHack);
        AddSFXToLibrary("PlayerShield", playerShield);
        AddSFXToLibrary("PlayerDamage", playerDamage);
        AddSFXToLibrary("PlayerDeath", playerDeath);
        AddSFXToLibrary("PlayerMove", playerMove);

        AddSFXToLibrary("EnemySpawn", enemySpawn);
        AddSFXToLibrary("EnemyDeath", enemyDeath);
        AddSFXToLibrary("TurretShoot", turretShoot);
        AddSFXToLibrary("ScannerAlert", scannerAlert);

        AddSFXToLibrary("DataPacketCollect", dataPacketCollect);
        AddSFXToLibrary("UIClick", uiClick);
        AddSFXToLibrary("UIHover", uiHover);
        AddSFXToLibrary("LevelComplete", levelComplete);

        Debug.Log($"✅ Загружено {sfxLibrary.Count} звуков в библиотеку");
    }

    void AddSFXToLibrary(string key, AudioClip clip)
    {
        if (clip != null && !sfxLibrary.ContainsKey(key))
        {
            sfxLibrary.Add(key, clip);
        }
    }

    // === МУЗЫКА ===

    public void PlayMainMenuMusic()
    {
        if (mainMenuMusic != null)
        {
            PlayMusic(mainMenuMusic, 1f);
        }
    }

    public void PlayLevelMusic(int levelIndex)
    {
        if (levelMusic != null && levelIndex >= 0 && levelIndex < levelMusic.Length)
        {
            PlayMusic(levelMusic[levelIndex], 0.8f);
        }
        else if (levelMusic != null && levelMusic.Length > 0)
        {
            PlayMusic(levelMusic[0], 0.8f);
        }
    }

    public void PlayBossMusic()
    {
        if (bossMusic != null) PlayMusic(bossMusic, 1f);
    }

    public void PlayVictoryMusic()
    {
        if (victoryMusic != null) PlayMusic(victoryMusic, 1f);
    }

    public void PlayGameOverMusic()
    {
        if (gameOverMusic != null) PlayMusic(gameOverMusic, 1f);
    }

    void PlayMusic(AudioClip music, float volumeMultiplier = 1f)
    {
        if (!musicEnabled || music == null) return;

        if (musicSource.clip == music && musicSource.isPlaying) return;

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        musicFadeCoroutine = StartCoroutine(FadeMusic(music, volumeMultiplier));
    }

    IEnumerator FadeMusic(AudioClip newMusic, float volumeMultiplier)
    {
        // Затухание текущей
        if (musicSource.isPlaying)
        {
            float startVol = musicSource.volume;
            float fadeOutDuration = 1f;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            musicSource.Stop();
        }

        // Смена трека
        currentMusic = newMusic;
        musicSource.clip = newMusic;
        musicSource.Play();

        // Появление новой
        float targetVol = musicVolume * masterVolume * volumeMultiplier;
        float fadeInDuration = 2f;
        float elapsedd = 0f;
        elapsedd = 0f;
        while (elapsedd < fadeInDuration)
        {
            elapsedd += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVol, elapsedd / fadeInDuration);
            yield return null;
        }
        musicSource.volume = targetVol;

        Debug.Log($"✅ Музыка: {newMusic?.name ?? "null"}, громкость: {targetVol:F2}");
    }

    public void StopMusic()
    {
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);
        StartCoroutine(FadeOutMusic());
    }

    IEnumerator FadeOutMusic()
    {
        float startVol = musicSource.volume;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = 0f;
    }

    // === SFX ===

    public void PlaySFX(string soundName, float volumeScale = 1f, float pitch = 1f)
    {
        if (!sfxEnabled || !sfxLibrary.TryGetValue(soundName, out AudioClip clip)) return;

        AudioSource temp = gameObject.AddComponent<AudioSource>();
        temp.clip = clip;
        temp.volume = Mathf.Clamp01(sfxVolume * masterVolume * volumeScale);
        temp.pitch = pitch;
        temp.Play();
        Destroy(temp, clip.length + 0.1f);
    }

    public void PlaySFXAtPosition(string soundName, Vector3 position, float volumeScale = 1f)
    {
        if (!sfxEnabled || !sfxLibrary.TryGetValue(soundName, out AudioClip clip)) return;
        float vol = sfxVolume * masterVolume * volumeScale;
        AudioSource.PlayClipAtPoint(clip, position, vol);
    }

    public void PlayUISound(string soundName)
    {
        if (!sfxEnabled || !sfxLibrary.TryGetValue(soundName, out AudioClip clip)) return;
        float vol = sfxVolume * masterVolume;
        uiSource.PlayOneShot(clip, vol);
    }

    // === НАСТРОЙКИ ===

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
        SaveAudioSettings();
        Debug.Log($"🔊 Master: {masterVolume:F2}");
    }


    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolumes(); // ← применяем
        SaveAudioSettings();
        Debug.Log($"🎵 Music: {musicVolume:F2}");
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        // SFX источники создаются временно, поэтому просто сохраняем — ✅
        // Но для UI-звука (uiSource) — можно обновить
        if (uiSource != null)
            uiSource.volume = Mathf.Clamp01(sfxVolume * masterVolume);
        SaveAudioSettings();
        Debug.Log($"🔊 SFX: {sfxVolume:F2}");
    }

    void ApplyVolumes()
    {
        // Обновляем музыку
        if (musicSource != null && musicSource.isPlaying)
        {
            // Сохраняем относительную громкость трека (например, victoryMusic может быть тише)
            float relativeVol = 1f;
            if (currentMusic == victoryMusic || currentMusic == gameOverMusic)
                relativeVol = 1f;
            else if (currentMusic == mainMenuMusic)
                relativeVol = 1f;
            else
                relativeVol = 0.8f; // как в PlayLevelMusic

            musicSource.volume = masterVolume * musicVolume * relativeVol;
        }

        // Обновляем ambient (если используется)
        if (ambientSource != null)
        {
            ambientSource.volume = masterVolume * 0.3f; // или как у тебя задумано
        }

        // Обновляем UI-звуки
        if (uiSource != null)
        {
            uiSource.volume = masterVolume * sfxVolume;
        }
    }

    public void ToggleMusic(bool enabled)
    {
        musicEnabled = enabled;
        if (!enabled) StopMusic();
        else if (currentMusic != null) PlayMusic(currentMusic, 1f);
        SaveAudioSettings();
    }

    public void ToggleSFX(bool enabled)
    {
        sfxEnabled = enabled;
        SaveAudioSettings();
    }

    // === СОХРАНЕНИЕ ===

    void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("MusicEnabled", musicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("SFXEnabled", sfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    // === АМБИЕНТ ===

    public void PlayAmbientSound(AudioClip clip)
    {
        if (ambientSource != null && clip != null)
        {
            ambientSource.clip = clip;
            ambientSource.Play();
        }
    }

    public void StopAmbientSound()
    {
        ambientSource?.Stop();
    }

    // === УДОБНЫЕ МЕТОДЫ ===

    public void PlayUIClick() => PlayUISound("UIClick");
    public void PlayUIHover() => PlayUISound("UIHover");
    public void PlayDataCollectSound() => PlaySFX("DataPacketCollect");
    public void PlayEnemyDeathSound() => PlaySFX("EnemyDeath");

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);
    }

    public void StopAllMusic()
    {
        StopMusic();
    }
}