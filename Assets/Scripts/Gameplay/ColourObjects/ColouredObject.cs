using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColouredObject : MaskChangeDetector
{
    [SerializeField] protected Material _colouredMaterial;
    [SerializeField] protected bool _isEnabled = true;
    [SerializeField] protected AudioSource _ambientSFXSource;
    [SerializeField] protected float _ambientFadeTime = 0.5f;
    protected float _maxVolumeMultiplier = 1f;
    protected List<GenericCouple<Renderer, Material>> _defaultMaterialList = new();
    protected IEnumerator _ambientSfxFade;

    protected virtual void Awake()
    {
        if (!_ambientSFXSource) return;
        _maxVolumeMultiplier = _ambientSFXSource.volume;
        _ambientSFXSource.volume = 0f;
    }

    protected override void OnEnable()
	{
        base.OnEnable();
		EventManager.RegisterEvent(EventKey.SYNC_MUSIC_TIME, SetMusicSyncTime);
	}

	protected override void OnDisable()
	{
        base.OnDisable();
		EventManager.DeregisterEvent(EventKey.SYNC_MUSIC_TIME, SetMusicSyncTime);
	}

    public virtual void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
    }

    public virtual void ToggleEnabled()
    {
        SetEnabled(!_isEnabled);
    }

    protected virtual void SetMusicSyncTime(object eventData)
    {
        if (eventData is not float) this.LogError("Event listener recieved incorrect data type!");
        float sourceSyncTime = (float)eventData;
        
        if (!_ambientSFXSource) return;
        _ambientSFXSource.Play();
        _ambientSFXSource.time = sourceSyncTime;
    }

    // Loop through all children of the gameobject, getting the renderers and 
    // their default material, then adding them to a list
    //
    // not the most performant, but easier for designers to add it to an object
    protected List<GenericCouple<Renderer, Material>> GetDefaultMaterialList(Transform[] transformArray)
    {
        List<GenericCouple<Renderer, Material>> defaultMaterialList = new();
        
        foreach (Transform item in transformArray)
        {
            if (item.TryGetComponent(out Renderer renderer) && renderer.enabled)
            {
                Material objectMaterial = renderer.material;
                defaultMaterialList.Add(new GenericCouple<Renderer, Material>(renderer, objectMaterial));
            }
        }

        return defaultMaterialList;
    } 

    protected virtual IEnumerator FadeAmbientMusic(float fadeTime, float finalVolume, float maxVolumeMultiplier)
    {
        if (!_ambientSFXSource.isPlaying && finalVolume == 0) yield break;
        if (_ambientSFXSource.isPlaying && _ambientSFXSource.volume == finalVolume) yield break;

        float startVolume = _ambientSFXSource.volume;

        if (!_ambientSFXSource.isPlaying)
        {
            _ambientSFXSource.Play();
            startVolume = 0;
            _ambientSFXSource.volume = startVolume;
        }

        float processedFinalVolume = finalVolume * maxVolumeMultiplier;

        // Fade in or out
        if (_ambientSFXSource.volume < processedFinalVolume)
        {
            while (_ambientSFXSource.volume < processedFinalVolume)
            {
                _ambientSFXSource.volume += processedFinalVolume * Time.deltaTime / fadeTime;
                yield return null;
            }
        }
        else if (_ambientSFXSource.volume > processedFinalVolume)
        {
            while (_ambientSFXSource.volume > processedFinalVolume)
            {
                _ambientSFXSource.volume -= startVolume * Time.deltaTime / fadeTime;
                yield return null;
            }
            // audioSource.Stop();
        }
        
        _ambientSFXSource.volume = processedFinalVolume;
    }

    protected virtual void UpdateAmbientSFX(bool isActive)
    {
        if (!_ambientSFXSource) return;

        if (isActive)
        {
            if (_ambientSfxFade != null)
            {
                StopCoroutine(_ambientSfxFade);
            }
            StartCoroutine(FadeAmbientMusic(_ambientFadeTime, 1, _maxVolumeMultiplier));
        }
        else
        {
            if (_ambientSfxFade != null)
            {
                StopCoroutine(_ambientSfxFade);
            }
            StartCoroutine(FadeAmbientMusic(_ambientFadeTime, 0, _maxVolumeMultiplier));
        }
    }
}
