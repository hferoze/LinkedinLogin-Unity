using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logger
{
    private static bool DEBUG = true;

    public static void Log(string msg)
    {
        if (DEBUG)
            Debug.Log(msg);
    }

    public static void LogError(string msg)
    {
        if (DEBUG)
            Debug.LogError(msg);
    }

    public static void LogWarning(string msg)
    {
        if (DEBUG)
            Debug.LogWarning(msg);
    }
}
