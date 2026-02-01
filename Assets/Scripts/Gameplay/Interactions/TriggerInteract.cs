using UnityEngine;

public class TriggerInteract : GreenObject, IInteractable
{
    private bool canBeInteracted = false;
    
    private bool isTriggered = false;
    
    [SerializeField]
    private GameObject pairedInteractable;
    
    [SerializeField]
    private AnimationClip animationClip;
    
    [SerializeField]
    private Animator animator;

    public void SetCanBeInteracted(bool val)
    {
        canBeInteracted = val;
    }
    
    public void Interact()
    {
        DebugLogger.Log("interact");
        if (canBeInteracted || isTriggered)  return;
        isTriggered = !isTriggered;
        
        animator.Play(animationClip.name, 0, 0.0f);

        if (!pairedInteractable || !pairedInteractable.TryGetComponent(out IActivate activateable)) return;
        
        activateable.Activate();
    }
}
