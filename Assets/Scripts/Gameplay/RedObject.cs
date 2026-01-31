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
    }

    void Update()
    {
        
    }

    protected override void EnableRedEffect()
    {
        if (!_rb || !_objectCollider || !_meshRenderer)
        {
            return;
        }

        _rb.detectCollisions = !_effectReversed;
        _objectCollider.enabled = !_effectReversed;
        _meshRenderer.enabled = !_effectReversed;
    }

    protected override void DisableRedEffect()
    {
        if (!_rb || !_objectCollider || !_meshRenderer)
        {
            return;
        }

        _rb.detectCollisions = _effectReversed;
        _objectCollider.enabled = _effectReversed;
        _meshRenderer.enabled = _effectReversed;
    }
}
