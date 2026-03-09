using UnityEngine;

public class SceneChangeInteract : GreenObject, IInteractable
{
    [SerializeField] private AnimationClip _animationClip;
    [SerializeField] private Animator _animator;
    [SerializeField] private int _nextSceneIndex = 4;
    [SerializeField] protected AudioSource _crystalAmbient;
    private bool _activated = false;

    public void Interact()
    {
        if (!_isEnabled) return;
        if (_activated) return;
        _activated = !_activated;
        
        if (_animator && _animationClip)
        {
            _animator.Play(_animationClip.name);
        }
        else
        {
            ChangeScenes();
        }
    }

    // Triggered with animation event
    public void ChangeScenes()
    {
        EventManager.TriggerEvent(EventKey.OPEN_SCENE, _nextSceneIndex);
    }

    public void SetCanBeInteracted(bool val){}

    protected override void SetMusicSyncTime(object eventData)
    {
        if (eventData is not int) this.LogError("Event listener recieved incorrect data type!");
        int sourceSyncTime = (int)eventData;
        
        if (_ambientSFXSource)
        {
            _ambientSFXSource.timeSamples = sourceSyncTime;
        }

        if (_crystalAmbient)
        {
            _crystalAmbient.timeSamples = sourceSyncTime;
        }
    }

    protected override void StartSyncedAmbientSFX(object eventData)
    {
        if (_ambientSFXSource && _ambientSFXSource.clip)
        {
            _ambientSFXSource.PlayScheduled(AudioSettings.dspTime + 1);
        }

        if (_crystalAmbient && _crystalAmbient.clip)
        {
            _crystalAmbient.PlayScheduled(AudioSettings.dspTime + 1);
        }
    }
}
