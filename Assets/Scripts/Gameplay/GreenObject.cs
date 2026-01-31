using UnityEngine;

public class GreenObject : MaskChangeDetector
{
    [SerializeField] private Material _greenMaterial;
    private Material _defaultObjectMaterial;
    private Mesh _objectMesh;

    private Renderer _objectRenderer;

    protected virtual void Start()
    {
        _objectMesh = GetComponent<Mesh>();
        _objectRenderer = GetComponent<Renderer>();
        if (_objectRenderer != null)
        {
            _defaultObjectMaterial = _objectRenderer.material;
        }

        DisableGreenEffect();
    }

    protected override void EnableGreenEffect()
    {
        _objectRenderer.material = _greenMaterial;
    }

    protected override void DisableGreenEffect()
    {
        _objectRenderer.material = _defaultObjectMaterial;
    }
}
