using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    #region Variables
    [SerializeField] private GenericCouple<MusicKey, AudioSource>[] MusicSourceMap;
    [SerializeField] private AudioSource[] AudioSourceArray;
    [SerializeField] private SoundAudioClip[] SoundAudioClipArray;
    [SerializeField] private MusicAudioClip[] MusicAudioClipArray;
    [SerializeField] private AudioMixer _mixer;
    private AudioSource _curretPrimaryMusicSource;

    private List<SoundType> CurrentSoundsList = new();
    private bool _musicMuted = false;
    private float _sfxVolume = 1;
    private float _musicVolume = 1;
    #endregion

    #region Unity Functions

    private void Start()
    {
        // EventManager.TriggerEvent(EventKey.MUSIC, SoundType.NoMask);
        MusicEventHandler(MusicKey.NoMask);
    }

    private void OnEnable()
    {
        EventManager.RegisterEvent(EventKey.SFX, SFXEventHandler);
        EventManager.RegisterEvent(EventKey.MUSIC, MusicEventHandler);
        EventManager.RegisterEvent(EventKey.FADE_MUSIC, FadeMusicEventHandler);
        EventManager.RegisterEvent(EventKey.STOP_MUSIC, StopMusic);
        EventManager.RegisterEvent(EventKey.PAUSE_MUSIC, PauseMusic);
        EventManager.RegisterEvent(EventKey.MUTEMUSIC_TOGGLE, MuteMusic);
        EventManager.RegisterEvent(EventKey.SFX_VOLUME_CHANGED, SFXVolumeHandler);
        EventManager.RegisterEvent(EventKey.MUSIC_VOLUME_CHANGED, MusicVolumeHandler);
    }

    private void OnDisable()
    {
        EventManager.DeregisterEvent(EventKey.SFX, SFXEventHandler);
        EventManager.DeregisterEvent(EventKey.MUSIC, FadeMusicEventHandler);
        EventManager.DeregisterEvent(EventKey.FADE_MUSIC, MusicEventHandler);
        EventManager.DeregisterEvent(EventKey.STOP_MUSIC, StopMusic);
        EventManager.DeregisterEvent(EventKey.PAUSE_MUSIC, PauseMusic);
        EventManager.DeregisterEvent(EventKey.MUTEMUSIC_TOGGLE, MuteMusic);
        EventManager.DeregisterEvent(EventKey.SFX_VOLUME_CHANGED, SFXVolumeHandler);
        EventManager.DeregisterEvent(EventKey.MUSIC_VOLUME_CHANGED, MusicVolumeHandler);
        StopAllCoroutines();
    }
    #endregion

    #region SFX Functions
    // Handles SFXEvent with incoming SFX data to play at specified cue source
    public void SFXEventHandler(object eventData)
    {
        if (eventData is not SoundType) this.LogError("Event listener recieved incorrect data type!");
        SoundType sound = (SoundType)eventData;

        //Find SoundAudioClip from array that has the same sound variable as the input
        SoundAudioClip clipSound = Array.Find(SoundAudioClipArray, x => x.sound == sound);
        if (clipSound == null)
        {
            this.LogError($"SoundAudioClip's sound not found: {sound}");
            return;
        }

        if (CurrentSoundsList.Contains(sound))
        {
            RestartSound(clipSound);
            return;
        }

        //Find first AudioSource that is not playing
        AudioSource source = Array.Find(AudioSourceArray, x => x.isPlaying == false);
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
        AudioSource source = Array.Find(AudioSourceArray, x => x.clip == clipSound.audioClip);
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
        CurrentSoundsList.Add(sound);
        yield return new WaitForSecondsRealtime(clip.length);
        CurrentSoundsList.Remove(sound);
    }
    #endregion

    #region Music Functions
    public void MusicEventHandler(object eventData)
    {
        if (eventData is not MusicKey) this.LogError("Event listener recieved incorrect data type!");
        MusicKey music = (MusicKey)eventData;

        if (_musicMuted) return;

        GenericCouple<MusicKey, AudioSource> mappedSource = Array.Find(MusicSourceMap, x => x.First == music);
        AudioSource musicSource = mappedSource.Second;

        if (musicSource == null) return;

        float musicTime = 0;
        if (_curretPrimaryMusicSource)
        {
            musicTime = _curretPrimaryMusicSource.time;
        }

        StopMusic(false);

        MusicAudioClip musicClip = Array.Find(MusicAudioClipArray, x => x.Music == music);
        if (musicClip == null)
        {
            this.LogError($"MusicAudioClip's music track not found {music}");
            return;
        }

        _curretPrimaryMusicSource = musicSource;
        musicSource.clip = musicClip.AudioClip;
        musicSource.volume = musicClip.Volume * _musicVolume;
        musicSource.Play();
        musicSource.time = musicTime;
    }

    public void PauseMusic(object eventData)
    {
        if (eventData is not bool) this.LogError("Event listener recieved incorrect data type!");
        bool paused = (bool)eventData;

        if (_musicMuted) return;

        if (paused)
        {
            foreach (GenericCouple<MusicKey, AudioSource> item in MusicSourceMap)
            {
                item.Second.Pause();
            }
        }
        else
        {
            foreach (GenericCouple<MusicKey, AudioSource> item in MusicSourceMap)
            {
                item.Second.Play();
            }
        }
    }

    public void StopMusic(object eventData)
    {
        foreach (GenericCouple<MusicKey, AudioSource> item in MusicSourceMap)
        {
            item.Second.Stop();
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
            foreach (GenericCouple<MusicKey, AudioSource> item in MusicSourceMap)
            {
                item.Second.Play();
            }
        }
    }

    public void FadeMusicEventHandler(object eventData)
    {
        if (eventData is not MusicKey) this.LogError("Event listener recieved incorrect data type!");
        MusicKey music = (MusicKey)eventData;

        if (_musicMuted) return;

        float MusicTime = _curretPrimaryMusicSource.time; 
        StopMusic(false);

        MusicAudioClip musicClip = Array.Find(MusicAudioClipArray, x => x.Music == music);

        // if (musicClip == null)
        // {
        //     this.LogError($"MusicAudioClip's music track not found {music}");
        //     return;
        // }

        // MusicSource.clip = musicClip.audioClip;
        // MusicSource.volume = musicClip.volume * _musicVolume;
        // MusicSource.Play();
        // MusicSource.time = MusicTime;

        StartCoroutine(CrossFade(MusicSourceMap[0].Second, musicClip.AudioClip, 1, 5));
    }

    private IEnumerator CrossFade( 
        AudioSource audioSource, 
        AudioClip newSound, 
        float finalVolume, 
        float fadeTime)
    {
        // yield return FadeOutTrack(audioSource, fadeTime);
        audioSource.clip = newSound;
        yield return FadeInTrack(audioSource, fadeTime, finalVolume);
        yield return FadeOutTrack(audioSource, fadeTime);
        yield return FadeInTrack(audioSource, fadeTime, finalVolume);
        yield return FadeOutTrack(audioSource, fadeTime);
        yield return FadeInTrack(audioSource, fadeTime, finalVolume);
    }

    private IEnumerator FadeInTrack(AudioSource audioSource, float fadeTime, float finalVolume)
    {
        audioSource.volume = 0;
        audioSource.Play();
        while (audioSource.volume < finalVolume)
        {
            audioSource.volume += finalVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
        audioSource.volume = finalVolume;
    }
    
    private IEnumerator FadeOutTrack(AudioSource audioSource, float fadeTime)
    {
        float startVolume = audioSource.volume;
        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = 0;
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

        foreach (GenericCouple<MusicKey, AudioSource> item in MusicSourceMap)
        {
            item.Second.volume = _musicVolume;
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

    #region Audio Clip Classes
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
    #endregion
}