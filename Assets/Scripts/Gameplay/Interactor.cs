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
    [SerializeField] private float _distance = 4.0f;
    private Camera _camera;
    private GameObject _previousHitObject;
    private GameObject _currentHitObject;
    private bool _interactionEnabled = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SetCapabilities(GameObject playerCameraObject)
    {
        if (!playerCameraObject.TryGetComponent(out Camera cam)) return;

        _camera = cam;
    }

    private void Update()
    {
        if (!_interactionEnabled) return;

        _previousHitObject = _currentHitObject;
        
        CastRay(out RaycastHit currentHit);
        if (currentHit.transform)
        {
            _currentHitObject = currentHit.transform.gameObject;
        }
        else
        {
            _currentHitObject = null;
        }

        if (_previousHitObject == _currentHitObject) return;

        // Send out unhighlighted event for previous object
        if (IsValidInteractable(_previousHitObject))
        {
            if (_previousHitObject && _previousHitObject.TryGetComponent(out IInteractable interactable))
            {
                interactable.Unhighlight();
            }
        }
        
        // Send out highlighted event for current object
        if (IsValidInteractable(_currentHitObject))
        {
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

    private bool CastRay(out RaycastHit hit)
    {
        if (!_camera)
        {
            Debug.Assert(_camera, "Failed to assign camera to Interactor script");
            hit = new();
            return false;
        }

        Vector3 startPos = _camera.transform.position;
        Vector3 forwardVec = _camera.transform.forward;
        Ray ray = new(startPos, forwardVec);

        bool success = Physics.Raycast(ray, out hit, _distance);
        Debug.DrawLine(ray.origin, hit.point, success ? Color.green : Color.red, 1.0f);

        return success;
    }

    public void InteractWithObject()
    {
        if (!_interactionEnabled) return;
        if (!_currentHitObject) return;

        if (_currentHitObject.TryGetComponent(out IInteractable interactable))
        {
            interactable.Interact();
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
