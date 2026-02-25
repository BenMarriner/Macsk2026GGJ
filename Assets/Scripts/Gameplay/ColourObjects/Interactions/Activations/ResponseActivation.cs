using UnityEngine;

public class ResponseActivation : AbstractActivation
{
    [SerializeField]
    private AnimationClip animationClip;
    
    [SerializeField]
    private Animator animator;
    
    public override void Activate()
    {
        if (_activated) return;
        _activated = !_activated;
        
        animator.Play(animationClip.name);
            
    }
}
