using UnityEngine;

public class BlueObject : ColouredObject
{
    [SerializeField] protected float _speed = 10f;

    protected bool _isMoving;
    protected virtual void Start()
    {
        _defaultMaterialList = GetDefaultMaterialList(GetComponentsInChildren<Transform>());

        SetBlueEffect(false);
    }

    protected override void SetBlueEffect(bool blueEnabled)
    {
        if (_effectReversed)
        {
            _isMoving = !blueEnabled;
        }
        else
        {
            _isMoving = blueEnabled;
        }

        foreach (GenericCouple<Renderer, Material> item in _defaultMaterialList)
        {
            Material newMaterial;
            
            if (blueEnabled)
            {
                newMaterial = _colouredMaterial;
            }
            else
            {
                newMaterial = item.Second;
            }

            item.First.material = newMaterial;
        }
    }
}
