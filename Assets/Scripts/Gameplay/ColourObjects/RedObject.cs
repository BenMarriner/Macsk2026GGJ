using UnityEngine;

public class RedObject : ColouredObject
{
    [SerializeField] protected Material _transparentColouredMaterial;
    protected bool _redMaskMode = false;
    private Rigidbody _rb;
    private Collider _objectCollider;
    private MeshRenderer _meshRenderer;

    void Start()
    {
        _rb = GetComponentInChildren<Rigidbody>();
        _objectCollider = GetComponentInChildren<Collider>();
        _meshRenderer = GetComponentInChildren<MeshRenderer>();

        _defaultMaterialList = GetDefaultMaterialList(GetComponentsInChildren<Transform>());

        SetRedEffect(false);
    }

    protected override void SetRedEffect(bool redEnabled)
    {
        _redMaskMode = redEnabled;
        bool isTangible = redEnabled;
        if (_effectReversed)
        {
            isTangible = !redEnabled;
        }

        if (_rb)
        {
            _rb.detectCollisions = isTangible;
        }

        if (_objectCollider)
        {
            _objectCollider.enabled = isTangible;
        }

        if (_meshRenderer)
        {
            if (!_effectReversed)
            {
                _meshRenderer.enabled = isTangible;
            }
            else
            {
                _meshRenderer.enabled = true;
            }
        }

        UpdateAmbientSFX(redEnabled);

        foreach (GenericCouple<Renderer, Material[]> item in _defaultMaterialList)
        {
            Material newMaterial;

            if (redEnabled)
            {
                if (isTangible)
                {
                    newMaterial = _colouredMaterial;
                }
                else
                {
                    newMaterial = _transparentColouredMaterial;
                }
            }
            else
            {
                newMaterial = item.Second[0]; //Currently no red objects have more than one material
            }

            item.First.material = newMaterial;
        }
    }

    public override void ToggleEffectReversed()
    {
        _effectReversed = !_effectReversed;
        SetEnabled(_isEnabled);
        SetRedEffect(_redMaskMode);
    }
}
