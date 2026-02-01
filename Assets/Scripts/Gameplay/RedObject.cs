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
        DisableRedEffect();
    }

    protected override void EnableRedEffect()
    {
        if (_rb)
        {
            _rb.detectCollisions = !_effectReversed;
        }

        if (_objectCollider)
        {
            _objectCollider.enabled = !_effectReversed;
        }

        if (_meshRenderer)
        {
            _meshRenderer.enabled = !_effectReversed;
        }
    }

    protected override void DisableRedEffect()
    {
        if (_rb)
        {
            _rb.detectCollisions = _effectReversed;
        }

        if (_objectCollider)
        {
            _objectCollider.enabled = _effectReversed;
        }

        if (_meshRenderer)
        {
            _meshRenderer.enabled = _effectReversed;
        }
    }
}
