using System.Collections;
using UnityEngine;

public class BlueWaypointObject : BlueObject
{
    [SerializeField] private GameObject _movingPlatform;
    [SerializeField] private WaypointPath _waypointPath;
    [SerializeField] private bool _useWaypointRotation;
    [SerializeField] private bool _slowNearEnd;
    [SerializeField] private float _arrivalPauseTime = 0.5f;
    private int _targetWaypointIndex;

    private Transform _previousWaypoint;
    private Transform _targetWaypoint;

    private float _elapsedTime;
    private bool _platformPaused = false;

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

        if (_platformPaused)
        {
            if (_elapsedTime > _arrivalPauseTime) Unpause();
            return;
        }

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
            _platformPaused = true;
        }
    }

    private void TargetNextWaypoint()
    {
        _previousWaypoint = _waypointPath.GetWaypoint(_targetWaypointIndex);
        _targetWaypointIndex = _waypointPath.GetNextWaypointIndex(_targetWaypointIndex);
        _targetWaypoint = _waypointPath.GetWaypoint(_targetWaypointIndex);

        _elapsedTime = 0f;
    }

    private void Unpause()
    {
        _elapsedTime = 0f;
        _platformPaused = false;
    }
}
