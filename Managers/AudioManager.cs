using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoSingleton<AudioManager>
{
    private float _wholeVolume = 1f;
    private float _bgmVolume = 0.5f;
    private float _sfxVolume = 0.2f;
    private float _originalBgVolume = 0.5f;
    private float _originalSfxVolume = 0.2f;
    private bool _isWholeSoundMute = false;
    private bool _isBGSoundMute = false;
    private bool _isSFXSoundMute = false;

    [Header("BGM")]
    public AudioSource bgmSource;

    private int _sfxPoolSize = 30;
    private Queue<AudioSource> _sfxPool;

    private Dictionary<string, AudioClip> _bgmClips = new();
    private Dictionary<string, AudioClip> _sfxClips = new();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != null)
            DontDestroyOnLoad(gameObject);

        LoadAllClips();
        InitBGM();
        InitSFXPool();
        ApplyVolumes();
    }

    private void InitBGM()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
    }

    private void InitSFXPool()
    {
        _sfxPool = new Queue<AudioSource>();
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            _sfxPool.Enqueue(source);
        }
    }

    private void LoadAllClips()
    {
        var bgms = Resources.LoadAll<AudioClip>("Sounds/BGM");
        foreach (var clip in bgms)
            _bgmClips[clip.name] = clip;

        var sfxs = Resources.LoadAll<AudioClip>("Sounds/SFX");
        foreach (var clip in sfxs)
            _sfxClips[clip.name] = clip;
    }

    public void PlayBGM(string name)
    {
        if (_bgmClips.TryGetValue(name, out var clip))
        {
            bgmSource.clip = clip;
            bgmSource.volume = _bgmVolume;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"BGM '{name}' not found");
        }
    }

    public void PlaySFX(string name)
    {
        if (_sfxClips.TryGetValue(name, out var clip))
        {
            AudioSource source = GetAvailableSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume;
            source.Play();
            StartCoroutine(ReturnToPoolWhenDone(source, clip.length));
        }
        else
        {
            Debug.LogWarning($"SFX '{name}' not found");
        }
    }

    // 효과음 여러개 재생시키고 싶을 때
    public void PlayRandomSFX(string namePrefix, int variantCount)
    {
        if (variantCount <= 0)
        {
            Debug.LogWarning($"Variant count must be greater than 0 for '{namePrefix}'");
            return;
        }

        int rand = Random.Range(1, variantCount + 1);
        string sfxName = $"{namePrefix}{rand}";

        PlaySFX(sfxName);
    }

    public void PlayMonsterSFX(string monsterKey, string action)
    {
        string clipName = $"{monsterKey}_{action}";
        PlaySFX(clipName);
    }

    private AudioSource GetAvailableSFXSource()
    {
        AudioSource source = _sfxPool.Dequeue();
        _sfxPool.Enqueue(source);
        return source;
    }

    private IEnumerator ReturnToPoolWhenDone(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        source.Stop();
        source.clip = null;
    }

    public void SetWholeVolume(float volume)
    {
        _wholeVolume = volume;
        ApplyVolumes();
    }

    public void SetWholeMute(bool isMute)
    {
        _isWholeSoundMute = isMute;
        ApplyVolumes();
    }

    public float GetWholeVolume()
    {
        return _wholeVolume;
    }

    public void SetBGMVolume(float volume)
    {
        _originalBgVolume = volume;
        ApplyVolumes();
    }

    public void SetBGMMute(bool isMute)
    {
        _isBGSoundMute = isMute;
        ApplyVolumes();
    }

    public float GetBGMVolume()
    {
        return _originalBgVolume;
    }

    public void SetSFXVolume(float volume)
    {
        _originalSfxVolume = volume;
        ApplyVolumes();
    }

    public void SetSFXMute(bool isMute)
    {
        _isSFXSoundMute = isMute;
        ApplyVolumes();
    }

    public float GetSFXVolume()
    {
        return _originalSfxVolume;
    }

    private void ApplyVolumes()
    {
        _bgmVolume = (_isWholeSoundMute || _isBGSoundMute) ? 0f : _originalBgVolume * _wholeVolume;
        _sfxVolume = (_isWholeSoundMute || _isSFXSoundMute) ? 0f : _originalSfxVolume * _wholeVolume;

        if (bgmSource != null)
            bgmSource.volume = _bgmVolume;

        foreach (var source in _sfxPool)
        {
            if (source.isPlaying)
                source.volume = _sfxVolume;
        }
    }

    public void PauseAllSound()
    {
        // BGM 일시정지
        if (bgmSource.isPlaying)
            bgmSource.Pause();

        // SFX 일시정지
        foreach (var source in _sfxPool)
        {
            if (source.isPlaying)
                source.Pause();
        }
    }

    public void ResumeAllSound()
    {
        // BGM 재생
        if (bgmSource.clip != null && !bgmSource.isPlaying)
            bgmSource.UnPause();

        // SFX 재생
        foreach (var source in _sfxPool)
        {
            if (source.clip != null && !source.isPlaying)
                source.UnPause();
        }
    }

    public void StopAllSound()
    {
        if (bgmSource.isPlaying)
            bgmSource.Stop();

        foreach (var sound in _sfxPool)
        {
            if (sound.clip != null || sound.isPlaying)
            {
                sound.Stop();
                sound.clip = null;
            }
        }
    }

    public Coroutine FadeOutBGM(float duration)
    {
        return StartCoroutine(FadeOutBGMRoutine(duration));
    }

    private IEnumerator FadeOutBGMRoutine(float duration)
    {
        if (bgmSource == null || !bgmSource.isPlaying)
            yield break;

        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();
    }
}
