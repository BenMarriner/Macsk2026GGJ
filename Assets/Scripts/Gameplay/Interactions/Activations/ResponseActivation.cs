using UnityEngine;

public class ResponseActivation : MonoBehaviour, IActivate
{
    private bool activated = false;

    [SerializeField]
    private AnimationClip animationClip;
    
    [SerializeField]
    private Animator animator;
    
    public void Activate()
    {
        if (activated) return;
        activated = !activated;
        
        animator.Play(animationClip.name);
            
    }
}
