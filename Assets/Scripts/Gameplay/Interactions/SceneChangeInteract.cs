using UnityEngine;

public class SceneChangeInteract : GreenObject, IInteractable
{
    private bool _activated = false;

    [SerializeField] private AnimationClip _animationClip;
    
    [SerializeField] private Animator _animator;
    [SerializeField] private int _nextSceneIndex = 4;
    public void Interact()
    {
        if (_activated) return;
        _activated = !_activated;
        
        _animator.Play(_animationClip.name);
    }

    // Triggered with animation event
    public void ChangeScenes()
    {
        EventManager.TriggerEvent(EventKey.OPEN_SCENE, _nextSceneIndex);
    }

    public void SetCanBeInteracted(bool val)
    {
    }
}
