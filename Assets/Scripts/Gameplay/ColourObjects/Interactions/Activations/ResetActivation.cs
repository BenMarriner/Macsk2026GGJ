using System.Collections.Generic;
using UnityEngine;

public class ResetActivation : AbstractActivation
{
    [SerializeField] protected List<ColouredObject> _resetColourObjectList;
    // [SerializeField] protected bool _resetOnPlayerDeath = false;

    // protected void OnEnable()
	// {
	// 	EventManager.RegisterEvent(EventKey.PLAYER_DIED, PlayerDiedhandler);
	// }

	// protected void OnDisable()
	// {
	// 	EventManager.DeregisterEvent(EventKey.PLAYER_DIED, PlayerDiedhandler);
	// }
    
    public override void Activate()
    {
        Reset();
    }

    protected virtual void PlayerDiedhandler(object eventData)
    {
        // if (!_resetOnPlayerDeath) return;
        Reset();
    }

    [ContextMenu("Reset Objects")]
    public void Reset()
    {
        foreach (ColouredObject item in _resetColourObjectList)
        {
            if (item)
            {
                item.Reset();
            }
        }
    }
}
