using UnityEngine;
using Object = UnityEngine.Object;

// Custom class for making Debug.Logs
public static class DebugLogger
{
    #region Custom Logs
    // Displicate code present to reduce unecessary information in the console
    public static void Log(this Object logCaller, params object[] message)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(logCaller == null)
        {
            Log(message); 
            return;
        }

        Debug.Log($"[<color=lightblue>{logCaller.name}   {logCaller.GetType()}</color>]: {string.Join("; ", message)}\n ", logCaller);
        #endif
    }

    public static void LogWarning(this Object logCaller, params object[] message)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(logCaller == null)
        {
            LogWarning(message);
            return;
        }

        Debug.LogWarning($"[<color=lightblue>{logCaller.name}   {logCaller.GetType()}</color>]: {string.Join("; ", message)}\n ", logCaller);
        #endif
    }

    public static void LogError(this Object logCaller, params object[] message)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(logCaller == null)
        {
            LogError(message);
            return;
        }
        
        Debug.LogError($"[<color=lightblue>{logCaller.name}   {logCaller.GetType()}</color>]: {string.Join("; ", message)}\n ", logCaller);
        #endif
    }

    #region Validation Log
    public static void Validate(this Object logCaller, object variable, string name)
    {
        if (variable == null) LogError(logCaller, name, "is null");
    }
    #endregion


    #region Overloads
    // Overloads for objectless logs
    public static void Log(params object[] message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{string.Join("; ", message)}\n ");
#endif
    }

    public static void LogWarning(params object[] message)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"{string.Join("; ", message)}\n ");
        #endif
    }

    public static void LogError(params object[] message)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogError($"{string.Join("; ", message)}\n ");
        #endif
    }
    #endregion
    #endregion
}

#region DisableDebugLogs
// Disables any Debug.Log not make with the DebugLogger
public class DisableDebugLogs : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void DisableDebugLogsBuild()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.unityLogger.logEnabled = true;
        #else
        Debug.unityLogger.logEnabled = false;
        #endif
    }
}
#endregion
