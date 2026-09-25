using System.Diagnostics;

/// <summary>
/// Drop-in replacement for Debug.Log for pure development chatter (attack states, combo
/// steps, etc.) - [Conditional] means the compiler strips every call site entirely in a
/// real Player build (UNITY_EDITOR isn't defined there), so this costs nothing at runtime
/// outside the Editor. Debug.LogWarning/LogError are untouched everywhere - those flag real
/// problems and should always run.
/// </summary>
public static class DevLog
{
    [Conditional("UNITY_EDITOR")]
    public static void Log(string message) => UnityEngine.Debug.Log(message);

    [Conditional("UNITY_EDITOR")]
    public static void Log(string message, UnityEngine.Object context) => UnityEngine.Debug.Log(message, context);
}
