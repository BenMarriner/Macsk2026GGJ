using System.Collections;
using UnityEngine;

public class TriggerWaypointObject : BlueWaypointObject
{
    protected override void Start()
    {
        base.Start();
        _isMoving = true;
    }

    protected override void SetBlueEffect(bool blueEnabled){}

    protected override void TargetNextWaypoint()
    {
        _previousWaypoint = _waypointPath.GetWaypoint(0);
        _targetWaypointIndex = _waypointPath.GetNextWaypointIndex(1);
        _targetWaypoint = _waypointPath.GetWaypoint(1);

        _elapsedTime = 0f;
    }

    protected override void Unpause(){}

    [ContextMenu("Reset Platform")]
    protected virtual void Reset()
    {
        _movingPlatform.transform.position = _waypointPath.GetWaypoint(_targetWaypointIndex).transform.position;
        _elapsedTime = 0f;
        _movingPaused = false;
        UpdateAmbientSFX(_isMoving && !_movingPaused);
    }
}
