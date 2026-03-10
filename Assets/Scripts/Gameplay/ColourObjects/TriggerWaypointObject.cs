using System.Collections.Generic;
using UnityEngine;

public class TriggerWaypointObject : BlueWaypointObject
{
    [SerializeField] protected List<BlueWaypointObject> _resetBlueList;

    protected override void OnEnable()
	{
        base.OnEnable();
		EventManager.RegisterEvent(EventKey.PLAYER_DIED, PlayerDiedhandler);
	}

	protected override void OnDisable()
	{
        base.OnDisable();
		EventManager.DeregisterEvent(EventKey.PLAYER_DIED, PlayerDiedhandler);
	}

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

    protected virtual void PlayerDiedhandler(object eventData)
    {
        Reset();
    }

    [ContextMenu("Reset Platform 2")]
    public override void Reset()
    {
        base.Reset();

        foreach (BlueWaypointObject item in _resetBlueList)
        {
            if (item)
            {
                item.Reset();
            }
        }
    }
}
