using UnityEngine;

public class RedObject : ColouredObject
{
    [SerializeField] protected Material _transparentColouredMaterial;
    private Rigidbody _rb;
    private Collider _objectCollider;
    private MeshRenderer _meshRenderer;

    void Start()
    {
        _rb = GetComponentInChildren<Rigidbody>();
        _objectCollider = GetComponentInChildren<Collider>();
        _meshRenderer = GetComponentInChildren<MeshRenderer>();

        Transform[] allObjectTransforms = GetComponentsInChildren<Transform>();

        _defaultMaterialList = GetDefaultMaterialList(allObjectTransforms);

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
            _rb.detectCollisions = enabled;
        }

        if (_objectCollider)
        {
            _objectCollider.enabled = enabled;
        }

        if (_meshRenderer && !_effectReversed)
        {
            _meshRenderer.enabled = enabled;
        }

        DebugLogger.Log(_defaultMaterialList.Count);

        foreach (GenericCouple<Renderer, Material> item in _defaultMaterialList)
        {
            Material newMaterial;
            
            // Note: 'enabled' may have been reversed if _effectReversed is active
            if (enabled)
            {
                newMaterial = _colouredMaterial;
            }
            else
            {
                if (_effectReversed)
                {
                    newMaterial = _transparentColouredMaterial;
                }
                else
                {
                    newMaterial = item.Second;
                }
            }
            DebugLogger.Log(newMaterial);

            item.First.material = newMaterial;
        }
    }
}
