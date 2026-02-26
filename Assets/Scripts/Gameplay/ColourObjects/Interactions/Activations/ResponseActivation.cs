using UnityEngine;

public class ResponseActivation : AbstractActivation
{
    [SerializeField] private AnimationClip[] animationClip;
    [SerializeField] private Animator animator;
    [SerializeField] private bool _loopAnimations = true;
    private int _activationAmount = 0;
    
    
    public override void Activate()
    {
        _activated = true;
        
        if (animationClip.Length < _activationAmount)
        {
            return;
        }

        animator.Play(animationClip[_activationAmount].name);
        _activationAmount++;

        if (animationClip.Length < _activationAmount &&_loopAnimations) 
        {
            _activationAmount = 0;
        }
    }
}
