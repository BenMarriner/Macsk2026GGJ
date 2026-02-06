using UnityEngine;

public class BlueObject : ColouredObject
{
    [SerializeField] private GameObject _movingPlatform;
    [SerializeField] private WaypointPath _waypointPath;
    [SerializeField] private float _speed = 10f;

    private int _targetWaypointIndex;

    private Transform _previousWaypoint;
    private Transform _targetWaypoint;

    private float _timeToWaypoint;
    private float _elapsedTime;

    private bool _isMoving = true;

    private void Start()
    {
        _defaultMaterialList = GetDefaultMaterialList(GetComponentsInChildren<Transform>());
        _movingPlatform.transform.position = _waypointPath.GetWaypoint(_targetWaypointIndex).transform.position;
        TargetNextWaypoint();

        SetBlueEffect(false);
    }

    private void FixedUpdate()
    {
        if (!_isMoving)
        {
            return;
        }

        _elapsedTime += Time.deltaTime;

        float elapsedPercentage = _elapsedTime / _speed;
        _movingPlatform.transform.position = Vector3.Lerp(_previousWaypoint.position, _targetWaypoint.position, elapsedPercentage);

        if (elapsedPercentage >= 1)
        {
            TargetNextWaypoint();
        }
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

    private void TargetNextWaypoint()
    {
        _previousWaypoint = _waypointPath.GetWaypoint(_targetWaypointIndex);
        _targetWaypointIndex = _waypointPath.GetNextWaypointIndex(_targetWaypointIndex);
        _targetWaypoint = _waypointPath.GetWaypoint(_targetWaypointIndex);

        _elapsedTime = 0f;

        float distanceToWaypoint = Vector3.Distance(_previousWaypoint.position, _targetWaypoint.position);
        _timeToWaypoint = distanceToWaypoint / _speed;
    }
}
