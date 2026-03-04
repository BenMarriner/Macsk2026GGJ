using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class GameMusicManager : MonoBehaviour
{
    #region Variables
    [SerializeField] private KeySourcePair[] _musicSourcePair;
    [SerializeField] private MusicAudioClip[] _musicAudioClipArray;
    [SerializeField] private AudioMixer _mixer;
    private KeySourcePair _currentPrimaryMusicSource;

    private bool _musicMuted = false;
    private float _musicVolume = 1;

    private List<IEnumerator> _musicFadeCoroutines = new();
    #endregion

    #region Unity Functions

    private void OnEnable()
    {
        EventManager.RegisterEvent(EventKey.FADE_MUSIC, FadeMusicEventHandler);
        EventManager.RegisterEvent(EventKey.FADE_SECONDARY_TRACKS, FadeSecondaryTracksHandler);
        EventManager.RegisterEvent(EventKey.STOP_MUSIC, StopMusic);
        EventManager.RegisterEvent(EventKey.PAUSE_MUSIC, PauseMusic);
        EventManager.RegisterEvent(EventKey.MUTEMUSIC_TOGGLE, MuteMusic);
        EventManager.RegisterEvent(EventKey.MUSIC_VOLUME_CHANGED, MusicVolumeHandler);
		EventManager.RegisterEvent(EventKey.START_SYNCED_MUSIC, StartSyncedAmbientSFX);
		EventManager.RegisterEvent(EventKey.SYNC_MUSIC_TIME, SetMusicSyncTime);
    }

    private void OnDisable()
    {
        EventManager.DeregisterEvent(EventKey.FADE_MUSIC, FadeMusicEventHandler);
        EventManager.DeregisterEvent(EventKey.FADE_SECONDARY_TRACKS, FadeSecondaryTracksHandler);
        EventManager.DeregisterEvent(EventKey.STOP_MUSIC, StopMusic);
        EventManager.DeregisterEvent(EventKey.PAUSE_MUSIC, PauseMusic);
        EventManager.DeregisterEvent(EventKey.MUTEMUSIC_TOGGLE, MuteMusic);
        EventManager.DeregisterEvent(EventKey.MUSIC_VOLUME_CHANGED, MusicVolumeHandler);
		EventManager.DeregisterEvent(EventKey.START_SYNCED_MUSIC, StartSyncedAmbientSFX);
		EventManager.DeregisterEvent(EventKey.SYNC_MUSIC_TIME, SetMusicSyncTime);
        StopAllCoroutines();
    }

    private void Start()
    {
        EventManager.DeregisterEvent(EventKey.STOP_MUSIC, StopMusic);
        EventManager.TriggerEvent(EventKey.PAUSE_MUSIC, true);
        EventManager.RegisterEvent(EventKey.PAUSE_MUSIC, PauseMusic);
        _currentPrimaryMusicSource = _musicSourcePair[0];
        EventManager.TriggerEvent(EventKey.START_SYNCED_MUSIC, null);
    }

    private void Update()
    {
        // this.Log(_currentPrimaryMusicSource.MusicSource.timeSamples);
        // foreach (KeySourcePair item in _musicSourcePair)
        // {
        //     this.Log(item.MusicKey, item.MusicSource.timeSamples);
        // }
    }
    #endregion

    #region Music Sync

    protected virtual void SendMusicSyncTime()
    {
        if (!_currentPrimaryMusicSource.MusicSource) return;
        int startTime = _currentPrimaryMusicSource.MusicSource.timeSamples;
        EventManager.TriggerEvent(EventKey.SYNC_MUSIC_TIME, startTime);
    }

    protected virtual void SetMusicSyncTime(object eventData)
    {
        if (eventData is not int) this.LogError("Event listener recieved incorrect data type!");
        int sourceSyncTime = (int)eventData;
        
        foreach (KeySourcePair item in _musicSourcePair)
        {
            if (!item.MusicSource) continue;
            item.MusicSource.timeSamples = sourceSyncTime;
        }
    }

    protected virtual void StartSyncedAmbientSFX(object eventData)
    {
        foreach (KeySourcePair item in _musicSourcePair)
        {
            item.MaxVolumeMultiplier = item.MusicSource.volume;
            item.MusicSource.volume = 0f;
            item.MusicSource.PlayScheduled(AudioSettings.dspTime + 1);
        }
        FadeMusicEventHandler(new MusicFadeData(MusicKey.NoMask, 5, 1));
    }
    #endregion

    #region Generic Music
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

        SendMusicSyncTime();
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
        float processedFinalVolume = finalVolume * maxVolumeMultiplier;

        if (!audioSource.isPlaying)
        {
            startVolume = 0;
            audioSource.volume = startVolume;
        }

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
    #endregion

    #region Private Classes
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