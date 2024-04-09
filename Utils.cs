using ContentLibrary;
using UnityEngine;

namespace ContentLibrary;

public static class CLogger
{
    public static void Log(string message) { SendLog(message, "Log"); }
    public static void LogInfo(string message) { SendLog(message, "LogInfo"); }
    public static void LogError(string message) { SendLog(message, "LogError"); }
    public static void LogWarning(string message) { SendLog(message, "LogWarning"); }
    public static void LogDebug(string message) { SendLog(message, "LogDebug"); }
    public static void LogFatal(string message) { SendLog(message, "LogFatal"); }
    public static void LogMessage(string message) { SendLog(message, "LogMessage"); }

    internal static void SendLog(string message, string level = null)
    {
        if (!ContentPlugin.DebugState && (level == "LogDebug" || level == "LogInfo")) return;

        switch (level)
        {
            case "LogInfo": ContentPlugin.Logger.LogInfo(message); break;
            case "LogError": ContentPlugin.Logger.LogError(message); break;
            case "LogWarning": ContentPlugin.Logger.LogWarning(message); break;
            case "LogDebug": ContentPlugin.Logger.LogDebug(message); break;
            case "LogFatal": ContentPlugin.Logger.LogFatal(message); break;
            case "LogMessage": ContentPlugin.Logger.LogMessage(message); break;
            default:
                {
                    if (level != "Log") Debug.Log($"[{level}]: {message}");
                    else Debug.Log(message);
                }
                break;
        }
    }
}