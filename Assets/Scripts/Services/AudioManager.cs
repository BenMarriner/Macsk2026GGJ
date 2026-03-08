using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    #region Variables
    [SerializeField] private KeySourcePair[] _musicSourcePair;
    [SerializeField] private AudioSource[] _audioSourceArray;
    [SerializeField] private SoundAudioClip[] _soundAudioClipArray;
    [SerializeField] private MusicAudioClip[] _musicAudioClipArray;
    [SerializeField] private AudioMixer _mixer;
    private KeySourcePair _currentPrimaryMusicSource;

    private readonly List<SoundType> _currentSoundsList = new();
    private bool _musicMuted = false;
    private float _sfxVolume = 1;
    private float _musicVolume = 1;

    private List<IEnumerator> _musicFadeCoroutines = new();
    #endregion

    #region Unity Functions

    private void OnEnable()
    {
        EventManager.RegisterEvent(EventKey.SFX, SFXEventHandler);
        EventManager.RegisterEvent(EventKey.MUSIC, MusicEventHandler);
        EventManager.RegisterEvent(EventKey.STOP_MUSIC, StopMusic);
        EventManager.RegisterEvent(EventKey.PAUSE_MUSIC, PauseMusic);
        EventManager.RegisterEvent(EventKey.MUTEMUSIC_TOGGLE, MuteMusic);
        EventManager.RegisterEvent(EventKey.SFX_VOLUME_CHANGED, SFXVolumeHandler);
        EventManager.RegisterEvent(EventKey.MUSIC_VOLUME_CHANGED, MusicVolumeHandler);
        EventManager.RegisterEvent(EventKey.OPEN_SCENE, StopMusic);
    }

    private void OnDisable()
    {
        EventManager.DeregisterEvent(EventKey.SFX, SFXEventHandler);
        EventManager.DeregisterEvent(EventKey.MUSIC, MusicEventHandler);
        EventManager.DeregisterEvent(EventKey.STOP_MUSIC, StopMusic);
        EventManager.DeregisterEvent(EventKey.PAUSE_MUSIC, PauseMusic);
        EventManager.DeregisterEvent(EventKey.MUTEMUSIC_TOGGLE, MuteMusic);
        EventManager.DeregisterEvent(EventKey.SFX_VOLUME_CHANGED, SFXVolumeHandler);
        EventManager.DeregisterEvent(EventKey.MUSIC_VOLUME_CHANGED, MusicVolumeHandler);
		EventManager.DeregisterEvent(EventKey.OPEN_SCENE, StopMusic);
        StopAllCoroutines();
    }

    private void Awake()
    {
        _currentPrimaryMusicSource = _musicSourcePair[0];
    }
    #endregion

    #region SFX Functions
    // Handles SFXEvent with incoming SFX data to play at specified cue source
    public void SFXEventHandler(object eventData)
    {
        if (eventData is not SoundType) this.LogError("Event listener recieved incorrect data type!");
        SoundType sound = (SoundType)eventData;

        //Find SoundAudioClip from array that has the same sound variable as the input
        SoundAudioClip clipSound = Array.Find(_soundAudioClipArray, x => x.sound == sound);
        if (clipSound == null)
        {
            this.LogError($"SoundAudioClip's sound not found: {sound}");
            return;
        }

        if (_currentSoundsList.Contains(sound))
        {
            RestartSound(clipSound);
            return;
        }

        //Find first AudioSource that is not playing
        AudioSource source = Array.Find(_audioSourceArray, x => x.isPlaying == false);
        if (source == null)
        {
            this.LogWarning("No audio source available to play this sound!");
            return;
        }

        source.pitch = 1f;
        if (clipSound.randomisePitch)
        {
            source.pitch = UnityEngine.Random.Range(0.7f, 1.3f);
        }
        source.clip = clipSound.audioClip;
        source.PlayOneShot(clipSound.audioClip, clipSound.volume * _sfxVolume);
        StartCoroutine(DoNotPlayMultipleOfSame(sound, clipSound.audioClip));
    }

    private void RestartSound(SoundAudioClip clipSound)
    {
        AudioSource source = Array.Find(_audioSourceArray, x => x.clip == clipSound.audioClip);
        if (source == null)
        {
            this.LogWarning("No audio source playing this sound!");
            return;
        }

        if (clipSound.randomisePitch) source.pitch = UnityEngine.Random.Range(0.7f, 1.3f);
        source.PlayOneShot(clipSound.audioClip, clipSound.volume * _sfxVolume);
    }

    private IEnumerator DoNotPlayMultipleOfSame(SoundType sound, AudioClip clip)
    {
        _currentSoundsList.Add(sound);
        yield return new WaitForSecondsRealtime(clip.length);
        _currentSoundsList.Remove(sound);
    }
    #endregion

    #region Generic Music
    public void MusicEventHandler(object eventData)
    {
        if (eventData is not MusicKey) this.LogError("Event listener recieved incorrect data type!");
        MusicKey musicKey = (MusicKey)eventData;

        if (_musicMuted) return;

        KeySourcePair mappedSource = Array.Find(_musicSourcePair, x => x.MusicKey == musicKey);
        AudioSource musicSource = mappedSource.MusicSource;

        if (musicSource == null) return;

        int musicTime = 0;
        if (_currentPrimaryMusicSource.MusicSource)
        {
            musicTime = _currentPrimaryMusicSource.MusicSource.timeSamples;
        }

        StopMusic(false);

        MusicAudioClip musicClip = Array.Find(_musicAudioClipArray, x => x.Music == musicKey);
        if (musicClip == null)
        {
            this.LogError($"MusicAudioClip's music track not found {musicKey}");
            return;
        }

        _currentPrimaryMusicSource.MusicKey = musicKey;
        _currentPrimaryMusicSource.MusicSource = musicSource;
        musicSource.clip = musicClip.AudioClip;
        musicSource.volume = musicClip.Volume * _musicVolume;
        musicSource.timeSamples = musicTime;
        musicSource.Play();
    }

    public void PauseMusic(object eventData)
    {
        if (eventData is not bool) this.LogError("Event listener recieved incorrect data type!");
        bool paused = (bool)eventData;

        if (_musicMuted) return;

        if (paused)
        {
            foreach (KeySourcePair item in _musicSourcePair)
            {
                item.MusicSource.Pause();
            }
        }
        else
        {
            foreach (KeySourcePair item in _musicSourcePair)
            {
                item.MusicSource.Play();
            }
        }
    }

    public void StopMusic(object eventData)
    {
        foreach (KeySourcePair item in _musicSourcePair)
        {
            item.MusicSource.Stop();
        }
    }

    public void MuteMusic(object eventData)
    {
        if (eventData is not bool) this.LogError("Event listener recieved incorrect data type!");
        bool muted = (bool)eventData;

        _musicMuted = muted;

        if (_musicMuted)
        {
            StopMusic(true);
        }
        else
        {
            foreach (KeySourcePair item in _musicSourcePair)
            {
                item.MusicSource.Play();
            }
        }
    }
    #endregion

    #region Music Fading
    public void FadeMusicEventHandler(object eventData)
    {
        if (eventData is not MusicFadeData) this.LogError("Event listener recieved incorrect data type!");
        MusicFadeData musicFadeData = (MusicFadeData)eventData;
        MusicKey musicKey = musicFadeData.MusicKey;

        if (_musicMuted) return;

        MusicAudioClip musicClip = Array.Find(_musicAudioClipArray, x => x.Music == musicKey);
        KeySourcePair mappedSource = Array.Find(_musicSourcePair, x => x.MusicKey == musicKey);
        AudioSource musicSource = mappedSource.MusicSource;
        float maxVolumeMultiplier = mappedSource.MaxVolumeMultiplier;
        if (musicSource == null) return;

        foreach (var item in _musicFadeCoroutines)
        {
            StopCoroutine(item);
        }

        IEnumerator tempMusicCoroutine = FadeTrack(musicSource, musicFadeData.FadeTime, musicFadeData.FinalVolume, maxVolumeMultiplier);
        SetupMusicCoutoutine(tempMusicCoroutine);
    }

    public void FadeSecondaryTracksHandler(object eventData)
    {
        if (eventData is not MusicFadeData) this.LogError("Event listener recieved incorrect data type!");
        MusicFadeData musicFadeData = (MusicFadeData)eventData;
        MusicKey musicKey = musicFadeData.MusicKey;

        if (_musicMuted) return;

        MusicAudioClip musicClip = Array.Find(_musicAudioClipArray, x => x.Music == musicKey);
        KeySourcePair mappedSource = Array.Find(_musicSourcePair, x => x.MusicKey == musicKey);
        AudioSource musicSource = mappedSource.MusicSource;
        float maxVolumeMultiplier = mappedSource.MaxVolumeMultiplier;
        if (musicSource == null) return;

        foreach (var item in _musicFadeCoroutines)
        {
            StopCoroutine(item);
        }

        IEnumerator tempMusicCoroutine = FadeTrack(musicSource, musicFadeData.FadeTime, musicFadeData.FinalVolume, maxVolumeMultiplier);
        SetupMusicCoutoutine(tempMusicCoroutine);

        // Fade out all the other tracks
        foreach (KeySourcePair item in _musicSourcePair)
        {
            if (item.MusicKey == musicKey) continue;
            if (item.MusicKey == _currentPrimaryMusicSource.MusicKey) continue;
            IEnumerator tempMusicCoroutine2 = FadeTrack(item.MusicSource, musicFadeData.FadeTime, 0, 0);
            SetupMusicCoutoutine(tempMusicCoroutine2);
        }
    }

    private IEnumerator FadeTrack(AudioSource audioSource, float fadeTime, float finalVolume, float maxVolumeMultiplier)
    {
        if (!audioSource.isPlaying && finalVolume == 0) yield break;
        if (audioSource.isPlaying && audioSource.volume == finalVolume) yield break;

        float startVolume = audioSource.volume;

        // Set track time
        int startTime = 0;
        if (_currentPrimaryMusicSource.MusicSource)
        {
            startTime = _currentPrimaryMusicSource.MusicSource.timeSamples;
        }

        audioSource.timeSamples = startTime;

        float processedFinalVolume = finalVolume * maxVolumeMultiplier;

        // Fade in or out
        if (audioSource.volume < processedFinalVolume)
        {
            while (audioSource.volume < processedFinalVolume)
            {
                audioSource.volume += processedFinalVolume * Time.deltaTime / fadeTime;
                yield return null;
            }
        }
        else if (audioSource.volume > processedFinalVolume)
        {
            while (audioSource.volume > processedFinalVolume)
            {
                audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
                yield return null;
            }
            // audioSource.Stop();
        }
        
        audioSource.volume = processedFinalVolume;
    }

    private void SetupMusicCoutoutine(IEnumerator newMusicCoroutine)
    {
        StartCoroutine(newMusicCoroutine);
        _musicFadeCoroutines.Append(newMusicCoroutine);
        _musicFadeCoroutines.RemoveAll(item => item == null);
    }
    #endregion

    #region Sound Settings
    public void SFXVolumeHandler(object eventData)
    {
        if (eventData is not float) this.LogError("Event listener recieved incorrect data type!");
        _sfxVolume = (float)eventData;

        // Update SFX volume in mixer
        float volumeDB = _sfxVolume > 0 ? Mathf.Log10(_sfxVolume) * 20 : -80f;
        _mixer.SetFloat("SFXVolume", volumeDB);
    }

    public void MusicVolumeHandler(object eventData)
    {
        if (eventData is not float) this.LogError("Event listener recieved incorrect data type!");
        _musicVolume = (float)eventData;

        // Update music volume in mixer
        float volumeDB = _musicVolume > 0 ? Mathf.Log10(_musicVolume) * 20 : -80f;
        _mixer.SetFloat("MusicVolume", volumeDB);

        foreach (KeySourcePair item in _musicSourcePair)
        {
            item.MusicSource.volume = _musicVolume;
        }
    }

    // public void SettingsRequestHandler(object eventData)
    // {
    //     if (eventData is not RequestType) this.LogError("Event listener recieved incorrect data type!");
    //     RequestType setting = (RequestType)eventData;

    //     if (setting != RequestType.AUDIO_SETTINGS) return;

    //     SettingsData tempSettings = new SettingsData(_sfxVolume, _musicVolume, 0);
    //     EventManager.TriggerEvent(EventKey.SEND_SETTING, tempSettings);
    // }
    #endregion

    #region Private Classes
    [Serializable]
    private class SoundAudioClip
    {
        public SoundType sound;
        public AudioClip audioClip;
        public bool randomisePitch = false;
        [Range(0, 1)] public float volume = 1f;
    }

    [Serializable]
    private class MusicAudioClip
    {
        public MusicKey Music;
        public AudioClip AudioClip;
        public bool RandomisePitch = false;
        [Range(0, 1)] public float Volume = 1f;
    }

    [Serializable]
    private class KeySourcePair
    {
        public MusicKey MusicKey;
        public AudioSource MusicSource;
        [HideInInspector] public float MaxVolumeMultiplier = 1f;

        public KeySourcePair(MusicKey inMusicKey, AudioSource inMusicSource, float inMaxVolume)
        {
            MusicKey = inMusicKey;
            MusicSource = inMusicSource;
            MaxVolumeMultiplier = inMaxVolume;
        }
    }
    #endregion
}