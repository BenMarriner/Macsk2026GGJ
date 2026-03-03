using UnityEngine;

public class TriggerInteract : GreenObject, IInteractable
{
    [SerializeField] 
    private AbstractActivation[] _pairedInteractables;
    
    [SerializeField]
    private AnimationClip animationClip;
    
    [SerializeField]
    private Animator animator;

    [SerializeField] 
    private bool _onlyInteractableOnce = false;
    [SerializeField] protected AudioSource _leverSwitchSource;

    private bool canBeInteracted = false;
    
    private bool isTriggered = false;
    
    public void SetCanBeInteracted(bool val)
    {
        canBeInteracted = val;
    }
    
    public void Interact()
    {
        if (!_isEnabled) return;
        if (!canBeInteracted || isTriggered)  return;
        if (_onlyInteractableOnce && isTriggered) return;
        isTriggered = !isTriggered;
        
        animator.Play(animationClip.name, 0, 0.0f);

        foreach (AbstractActivation item in _pairedInteractables)
        {
            if (!item) return;

            item.Activate();
        }

        if (_leverSwitchSource && _leverSwitchSource.clip)
        {
            _leverSwitchSource.Play();
        }

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
