using UnityEngine;

public class SceneChangeInteract : GreenObject, IInteractable
{
    private bool _activated = false;

    [SerializeField] private AnimationClip _animationClip;
    
    [SerializeField] private Animator _animator;
    [SerializeField] private int _nextSceneIndex = 4;
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

    public void SetCanBeInteracted(bool val)
    {
    }

    public override void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;

        if (_isEnabled && _greenMaskMode)
        {
            EnableGreenEffect();
        }
    }
}
