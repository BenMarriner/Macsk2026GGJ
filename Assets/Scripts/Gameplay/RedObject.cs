using UnityEngine;

public class RedObject : MaskChangeDetector
{
    private Rigidbody _rb;
    private Collider _objectCollider;
    private MeshRenderer _meshRenderer;

    void Start()
    {
        _rb = GetComponentInChildren<Rigidbody>();
        _objectCollider = GetComponentInChildren<Collider>();
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
        SetRedEffect(false);
    }

    protected override void SetRedEffect(bool enabled)
    {
        if (_effectReversed)
        {
            enabled = !enabled;
        }

        if (_rb)
        {
            _rb.detectCollisions = !enabled;
        }

        if (_objectCollider)
        {
            _objectCollider.enabled = !enabled;
        }

        if (_meshRenderer)
        {
            _meshRenderer.enabled = !enabled;
        }
    }
}
