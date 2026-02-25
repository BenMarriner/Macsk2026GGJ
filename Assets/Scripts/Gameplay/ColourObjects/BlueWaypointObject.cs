using UnityEngine;

public class BlueWaypointObject : BlueObject
{
    [SerializeField] private GameObject _movingPlatform;
    [SerializeField] private WaypointPath _waypointPath;
    [SerializeField] private bool _useWaypointRotation;
    [SerializeField] private bool _slowNearEnd;
    private int _targetWaypointIndex;

    private Transform _previousWaypoint;
    private Transform _targetWaypoint;

    private float _timeToWaypoint;
    private float _elapsedTime;

    protected override void Start()
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
        if (_slowNearEnd)
        {
            elapsedPercentage = Mathf.SmoothStep(0, 1, elapsedPercentage);
        }

        _movingPlatform.transform.position = Vector3.Lerp(_previousWaypoint.position, _targetWaypoint.position, elapsedPercentage);

        if (_useWaypointRotation)
        {
            _movingPlatform.transform.rotation = Quaternion.Lerp(_previousWaypoint.rotation, _targetWaypoint.rotation, elapsedPercentage);
        }

        if (elapsedPercentage >= 1)
        {
            TargetNextWaypoint();
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
