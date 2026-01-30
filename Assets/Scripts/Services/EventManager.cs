using System;
using System.Collections.Generic;

#region Event Manager
public static class EventManager
{
    //stores event handlers, Action = value delegate
    private static Dictionary<EventKey, Action<object>> _eventDictionary = new Dictionary<EventKey, Action<object>>();

    // Tells the event manger to start sending the specified event to the 
    // object that calls this function
    public static void RegisterEvent(EventKey eventkey, Action<object> eventHandler)
    {
        // If the event is in the dictionary, add the object as a listener, otherwise add the
        // event to the dictionary and add the object as a listener
        if (_eventDictionary.ContainsKey(eventkey))
        {
            _eventDictionary[eventkey] += eventHandler; //subscibes to additional events
        }
        else
        {
            _eventDictionary[eventkey] = eventHandler; //adds new entry with eventkey as a key for eventHandler
        }
    }

    // Tells the event manager to no longer send the specified event to the
    // object that calls this function
    public static void DeregisterEvent(EventKey eventkey, Action<object> eventHandler)
    {
        // if the event is in the dict, remove the object as a listener
        if (!_eventDictionary.ContainsKey(eventkey)) return;

        _eventDictionary[eventkey] -= eventHandler;  //unsubscibes to additional events
    }

    // Function for other objects to trigger, that are sent to the listeners
    public static void TriggerEvent(EventKey eventkey, object eventData)
    {
        // If the event is in the dict
        if (!_eventDictionary.TryGetValue(eventkey, out Action<object> thisEvent)) return;
        if (thisEvent == null) return;

        thisEvent.Invoke(eventData); //send the event
    }
}
#endregion