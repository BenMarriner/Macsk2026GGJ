using UnityEngine;

public class BlueObject : MaskChangeDetector
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
        _movingPlatform.transform.position = _waypointPath.GetWaypoint(_targetWaypointIndex).transform.position;
        TargetNextWaypoint();

        DisableBlueEffect();
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

    protected override void EnableBlueEffect()
    {
        _isMoving = !_effectReversed;
    }

    protected override void DisableBlueEffect()
    {
        _isMoving = _effectReversed;
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
