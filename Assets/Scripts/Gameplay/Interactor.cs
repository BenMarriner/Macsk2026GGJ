using UnityEngine;

interface IInteractable
{
    public void Interact();
    public void SetCanBeInteracted(bool val);
    public void Highlight();
    public void Unhighlight();
}

interface IActivate
{
    public void Activate();
}

public class Interactor : MaskChangeDetector
{
    public float Distance = 1500.0f;

    Camera Camera;
    GameObject _previousHitObject;
    GameObject _currentHitObject;

    GameObject HighlightedObject;

    private bool _interactionEnabled = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SetCapabilities(GameObject playerCameraObject)
    {
        //GameObject player = transform.gameObject;
        //Camera = player.GetComponentInChildren<Camera>();
        //Camera = GameObject.Find("CameraHolder(Clone)").GetComponentInChildren<Camera>();
        if (!playerCameraObject.TryGetComponent(out Camera cam)) return;

        Camera = cam;
    }

    void Update()
    {
        if (!_interactionEnabled) return;

        _previousHitObject = _currentHitObject;
        
        CastRay(out RaycastHit currentHit);
        if (currentHit.transform)
            _currentHitObject = currentHit.transform.gameObject;

        if (_previousHitObject == _currentHitObject)
            return;

        // Send out unhighlighted event for previous object
        if (IsValidInteractable(_previousHitObject))
        {
            EventManager.TriggerEvent(EventKey.INTERACTABLE_UNHIGHLIGHTED, _previousHitObject);
            if (_previousHitObject && _previousHitObject.TryGetComponent(out IInteractable interactable))
            {
                interactable.Unhighlight();
            }
        }
        
        // Send out highlighted event for current object
        if (IsValidInteractable(_currentHitObject))
        {
            EventManager.TriggerEvent(EventKey.INTERACTABLE_HIGHLIGHTED, _currentHitObject);
            if (_currentHitObject && _currentHitObject.TryGetComponent(out IInteractable interactable))
            {
                interactable.Highlight();
            }
        }
    }

    private bool IsValidInteractable(GameObject hitObject)
    {
        if (!hitObject) return false;
        
        if (!hitObject.TryGetComponent(out IInteractable interactable)) return false;
        
        return true;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EventManager.RegisterEvent(EventKey.INTERACTABLE_HIGHLIGHTED, ObjectHighlightedHandler);
        EventManager.RegisterEvent(EventKey.INTERACTABLE_UNHIGHLIGHTED, ObjectUnhighlightedHandler);
        EventManager.RegisterEvent(EventKey.INTERACTABLE_INTERACTED, ObjectInteractedHandler);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventManager.DeregisterEvent(EventKey.INTERACTABLE_HIGHLIGHTED, ObjectHighlightedHandler);
        EventManager.DeregisterEvent(EventKey.INTERACTABLE_UNHIGHLIGHTED, ObjectUnhighlightedHandler);
        EventManager.DeregisterEvent(EventKey.INTERACTABLE_INTERACTED, ObjectInteractedHandler);
    }

    private bool CastRay(out RaycastHit hit)
    {
        if (!Camera)
        {
            Debug.Assert(Camera, "Failed to assign camera to Interactor script");
            hit = new();
            return false;
        }

        Vector3 startPos = Camera.transform.position;
        Vector3 forwardVec = Camera.transform.forward;
        Ray ray = new(startPos, forwardVec);

        bool success = Physics.Raycast(ray, out hit, Distance);
        Debug.DrawLine(ray.origin, hit.point, success ? Color.green : Color.red, 1.0f);

        return success;
    }

    void ObjectHighlightedHandler(object eventData)
    {
        HighlightedObject = eventData as GameObject;
        if (!HighlightedObject)
        {
            Debug.Assert(HighlightedObject, "Failed to handle ObjectHighlighted call");
            return;
        }
        Debug.Log(HighlightedObject.name + " highlighted");
    }

    void ObjectUnhighlightedHandler(object eventData)
    {
        GameObject unhighlightedObject = eventData as GameObject;
        if (!unhighlightedObject)
        {
            Debug.Assert(HighlightedObject, "Failed to handle ObjectUnhighlighted call");
            return;
        }
        Debug.Log(HighlightedObject.name + " unhighlighted");
    }
    
    void ObjectInteractedHandler(object eventData)
    {
        
    }

    public void InteractWithObject()
    {
        if (!_interactionEnabled) return;

        if (_currentHitObject)
        {
            if (_currentHitObject.TryGetComponent(out IInteractable interactable))
            {
                interactable.Interact();
            }
            return;
        }

        if (_currentHitObject.transform.parent)
        {
            if (_currentHitObject.transform.parent.gameObject.TryGetComponent(out IInteractable interactable))
            {
                interactable.Interact();
            }
            return;
        }
    }

    protected override void SetGreenEffect(bool greenEnabled)
    {
        _interactionEnabled = greenEnabled;

        if (!_interactionEnabled)
        {
            _previousHitObject = null;
            _currentHitObject = null;
        }
    }
}
