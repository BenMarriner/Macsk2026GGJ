using UnityEngine;

public class TriggerInteract : GreenObject, IInteractable
{
    [SerializeField]
    private GameObject[] _pairedInteractables;
    
    [SerializeField]
    private AnimationClip animationClip;
    
    [SerializeField]
    private Animator animator;

    [SerializeField] 
    private bool _onlyInteractableOnce = false;

    private bool canBeInteracted = false;
    
    private bool isTriggered = false;
    
    public void SetCanBeInteracted(bool val)
    {
        canBeInteracted = val;
    }
    
    public void Interact()
    {
        DebugLogger.Log("interact");
        if (!canBeInteracted || isTriggered)  return;
        if (_onlyInteractableOnce && isTriggered) return;
        isTriggered = !isTriggered;
        
        animator.Play(animationClip.name, 0, 0.0f);

        foreach (GameObject item in _pairedInteractables)
        {
            if (item == null) continue;

            if (!item || !item.TryGetComponent(out IActivate activateable)) return;

            activateable.Activate();
        }

        EventManager.TriggerEvent(EventKey.SFX, SoundType.LeverSwitch);

        if (_onlyInteractableOnce)
        {
            DisableGreenEffect();
        }
    }

    protected override void EnableGreenEffect()
    {
        if (_onlyInteractableOnce && isTriggered) return;
        base.EnableGreenEffect();
    }

    public override void Highlight()
    {
        if (_onlyInteractableOnce && isTriggered) return;
        base.Highlight();
    }
}
