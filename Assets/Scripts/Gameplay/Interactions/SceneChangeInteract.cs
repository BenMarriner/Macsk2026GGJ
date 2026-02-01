using UnityEngine;

public class SceneChangeInteract : GreenObject, IInteractable
{
    [SerializeField] private int nextSceneIndex = 4;
    public void Interact()
    {
        EventManager.TriggerEvent(EventKey.OPEN_SCENE, nextSceneIndex);
    }

    public void SetCanBeInteracted(bool val)
    {
    }

    public void Highlight()
    {
    }

    public void Unhighlight()
    {
    }
}
