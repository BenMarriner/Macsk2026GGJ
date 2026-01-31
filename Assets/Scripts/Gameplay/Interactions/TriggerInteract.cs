using UnityEngine;

public class TriggerInteract : MonoBehaviour, IInteractable
{
    private bool isTriggered = false;
    
    [SerializeField]
    private GameObject pairedInteractable;
    
    [SerializeField]
    private AnimationClip animationClip;
    
    [SerializeField]
    private Animator animator;
    
    public void Interact()
    {
        if (isTriggered)  return;
        isTriggered = !isTriggered;
        
        animator.Play(animationClip.name, 0, 0.0f);

        if (!pairedInteractable || !pairedInteractable.TryGetComponent(out IActivate activateable)) return;
        
        activateable.Activate();
    }
}
