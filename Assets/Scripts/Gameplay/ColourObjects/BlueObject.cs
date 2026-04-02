using UnityEngine;

public class BlueObject : ColouredObject
{
    [SerializeField] protected float _speed = 10f;
    protected bool _blueMaskMode = false;
    protected bool _isMoving;
    protected bool _movingPaused = false;

    protected virtual void Start()
    {
        _defaultMaterialList = GetDefaultMaterialList(GetComponentsInChildren<Transform>());

        SetBlueEffect(false);
    }

    public override void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;

        SetBlueEffect(_blueMaskMode);
    }

    protected override void SetBlueEffect(bool blueEnabled)
    {
        _blueMaskMode = blueEnabled;
        if (!_isEnabled) return;

        if (_effectReversed)
        {
            _isMoving = !_blueMaskMode;
        }
        else
        {
            _isMoving = _blueMaskMode;
        }

        UpdateAmbientSFX(_isMoving && !_movingPaused);

        if (!_colouredMaterial) return;

        foreach (GenericCouple<Renderer, Material[]> item in _defaultMaterialList)
        {
            Material newMaterial;
            
            if (_blueMaskMode)
            {
                newMaterial = _colouredMaterial;
            }
            else
            {
                newMaterial = item.Second[0]; //Currently no blue objects have more than one material
            }

            item.First.material = newMaterial;
        }
    }
}
