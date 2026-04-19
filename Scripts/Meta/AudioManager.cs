using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource dealSource;
    [SerializeField] private AudioSource transitionSource; // Новый источник для звуков переходов
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip betSound;
    [SerializeField] private AudioClip raiseSound;
    [SerializeField] private AudioClip foldSound;
    [SerializeField] private AudioClip callSound;
    [SerializeField] private AudioClip dealCardsSound;
    [SerializeField] private AudioClip chipAddSound;
    
    [Header("Win Sounds")]
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip playerWinSound;
    [SerializeField] private AudioClip npcWinSound;
    
    [Header("Transition Sounds")]
    [SerializeField] private AudioClip levelCompleteSound;
    [SerializeField] private AudioClip transitionStartSound;
    [SerializeField] private AudioClip transitionEndSound;
    [SerializeField] private AudioClip transitionWooshSound; // Дополнительный звук "вжух"
    
    [Header("Ambient")]
    [SerializeField] private AudioClip ambientLoop;
    [SerializeField] private float ambientVolume = 0.3f;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float transitionVolume = 0.8f;
    
    private Coroutine dealCardsCoroutine;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("AudioManager инициализирован");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeAudioSources();
    }
    
    void Start()
    {
        PlayAmbient();
    }
    
    private void InitializeAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = musicVolume * masterVolume;
        }
        
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume * masterVolume;
        }
        
        if (dealSource == null)
        {
            dealSource = gameObject.AddComponent<AudioSource>();
            dealSource.loop = false;
            dealSource.volume = sfxVolume * masterVolume;
        }
        
        if (transitionSource == null)
        {
            transitionSource = gameObject.AddComponent<AudioSource>();
            transitionSource.loop = false;
            transitionSource.volume = transitionVolume * masterVolume;
        }
    }
    
    #region Public Methods
    
    public void PlayBetSound()
    {
        PlaySFX(betSound);
    }
    
    public void PlayRaiseSound()
    {
        PlaySFX(raiseSound);
    }
    
    public void PlayFoldSound()
    {
        PlaySFX(foldSound);
    }
    
    public void PlayCallSound()
    {
        PlaySFX(callSound);
    }
    
    public void PlayChipAddSound()
    {
        PlaySFX(chipAddSound);
        Debug.Log("PlayChipAddSound вызван");
    }
    
    public void PlayDealCardsSound()
    {
        if (dealCardsSound == null || dealSource == null) return;
        
        if (dealSource.isPlaying)
        {
            dealSource.Stop();
        }
        
        dealSource.PlayOneShot(dealCardsSound);
        Debug.Log("PlayDealCardsSound вызван");
    }
    
    public void PlayDealCardsSequence(int cardCount)
    {
        if (dealCardsCoroutine != null)
            StopCoroutine(dealCardsCoroutine);
        
        dealCardsCoroutine = StartCoroutine(DealCardsSequence(cardCount));
    }
    
    private IEnumerator DealCardsSequence(int cardCount)
    {
        for (int i = 0; i < cardCount; i++)
        {
            PlayDealCardsSound();
            yield return new WaitForSeconds(0.15f);
        }
        dealCardsCoroutine = null;
    }
    
    public void PlayAmbient()
    {
        if (ambientLoop != null && musicSource != null)
        {
            musicSource.clip = ambientLoop;
            musicSource.loop = true;
            musicSource.volume = ambientVolume * masterVolume * musicVolume;
            musicSource.Play();
            Debug.Log("Ambient запущен");
        }
    }
    
    public void StopAmbient()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            StartCoroutine(FadeOutMusic(musicSource, 0.5f));
        }
    }
    
    /// <summary>
    /// Воспроизводит стандартный звук победы
    /// </summary>
    public void PlayWinSound()
    {
        PlaySFX(winSound, 0.8f);
        Debug.Log("PlayWinSound вызван");
    }
    
    /// <summary>
    /// Воспроизводит звук победы игрока (триумфальный)
    /// </summary>
    public void PlayPlayerWinSound()
    {
        if (playerWinSound != null)
        {
            PlaySFX(playerWinSound, 0.9f);
            Debug.Log("PlayPlayerWinSound вызван");
        }
        else
        {
            // Если нет отдельного звука для игрока, используем стандартный
            PlayWinSound();
        }
    }
    
    /// <summary>
    /// Воспроизводит звук победы NPC
    /// </summary>
    public void PlayNPCWinSound()
    {
        if (npcWinSound != null)
        {
            PlaySFX(npcWinSound, 0.6f);
            Debug.Log("PlayNPCWinSound вызван");
        }
        else
        {
            // Если нет отдельного звука для NPC, используем стандартный
            PlayWinSound();
        }
    }
    
    #region Transition Sounds
    
    /// <summary>
    /// Воспроизводит звук завершения уровня
    /// </summary>
    public void PlayLevelCompleteSound()
    {
        if (levelCompleteSound != null && transitionSource != null)
        {
            PlayTransitionSound(levelCompleteSound, 0.9f);
            Debug.Log("PlayLevelCompleteSound вызван");
        }
        else if (levelCompleteSound == null)
        {
            Debug.Log("levelCompleteSound не назначен");
        }
    }
    
    /// <summary>
    /// Воспроизводит звук начала перехода
    /// </summary>
    public void PlayTransitionStartSound()
    {
        if (transitionStartSound != null && transitionSource != null)
        {
            PlayTransitionSound(transitionStartSound, 0.7f);
            Debug.Log("PlayTransitionStartSound вызван");
        }
        else if (transitionStartSound == null)
        {
            Debug.Log("transitionStartSound не назначен");
        }
    }
    
    /// <summary>
    /// Воспроизводит звук окончания перехода
    /// </summary>
    public void PlayTransitionEndSound()
    {
        if (transitionEndSound != null && transitionSource != null)
        {
            PlayTransitionSound(transitionEndSound, 0.7f);
            Debug.Log("PlayTransitionEndSound вызван");
        }
        else if (transitionEndSound == null)
        {
            Debug.Log("transitionEndSound не назначен");
        }
    }
    
    /// <summary>
    /// Воспроизводит звук "вжух" для перехода
    /// </summary>
    public void PlayTransitionWooshSound()
    {
        if (transitionWooshSound != null && transitionSource != null)
        {
            PlayTransitionSound(transitionWooshSound, 0.6f);
            Debug.Log("PlayTransitionWooshSound вызван");
        }
    }
    
    /// <summary>
    /// Воспроизводит звук перехода на отдельном источнике
    /// </summary>
    private void PlayTransitionSound(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            Debug.LogWarning($"Transition AudioClip null, звук не воспроизведен");
            return;
        }
        
        if (transitionSource == null)
        {
            Debug.LogWarning("transitionSource null");
            return;
        }
        
        transitionSource.PlayOneShot(clip, volume * transitionVolume * masterVolume);
        Debug.Log($"PlayTransitionSound: {clip.name} с громкостью {volume}");
    }
    
    #endregion
    
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning($"AudioClip null, звук не воспроизведен");
            return;
        }
        
        if (sfxSource == null)
        {
            Debug.LogWarning("sfxSource null");
            return;
        }
        
        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        Debug.Log($"PlaySFX: {clip.name}");
    }
    
    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            Debug.LogWarning($"AudioClip null, звук не воспроизведен");
            return;
        }
        
        if (sfxSource == null)
        {
            Debug.LogWarning("sfxSource null");
            return;
        }
        
        sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
        Debug.Log($"PlaySFX: {clip.name} с громкостью {volume}");
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    
    public void SetTransitionVolume(float volume)
    {
        transitionVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    
    private void UpdateVolumes()
    {
        if (musicSource != null)
            musicSource.volume = ambientVolume * masterVolume * musicVolume;
        
        if (sfxSource != null)
            sfxSource.volume = sfxVolume * masterVolume;
        
        if (dealSource != null)
            dealSource.volume = sfxVolume * masterVolume;
        
        if (transitionSource != null)
            transitionSource.volume = transitionVolume * masterVolume;
    }
    
    #endregion
    
    #region Private Methods
    
    private IEnumerator FadeOutMusic(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
            yield return null;
        }
        
        source.Stop();
        source.volume = startVolume;
    }
    
    #endregion
}