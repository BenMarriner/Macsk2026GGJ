using UnityEngine;

public class BlueRotateObject : BlueObject
{
    [SerializeField] private RotateDirection _rotateDirectionMode = RotateDirection.Yaw;
    private Vector3 _rotateDirectionVector;

    protected override void Start()
    {
        base.Start();

        switch (_rotateDirectionMode)
        {
            case RotateDirection.Yaw:
                _rotateDirectionVector = Vector3.up;
            break;
            case RotateDirection.Pitch:
                _rotateDirectionVector = Vector3.back;
            break;
            case RotateDirection.Roll:
                _rotateDirectionVector = Vector3.left;
            break;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!_isMoving)
        {
            return;
        }

        gameObject.transform.Rotate(_speed * Time.deltaTime * _rotateDirectionVector);
    }

    public enum RotateDirection
    {
        Yaw = 0,
        Pitch = 1,
        Roll = 2,
    }
}


