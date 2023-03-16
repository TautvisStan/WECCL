﻿using HarmonyLib;
using UnityEngine;

namespace WECCL.Patches;

[HarmonyPatch]
public class LogPatch
{
    /*
     * This patch is used to enable the game's unity logger if the user has enabled it in the config.
     */
    [HarmonyPatch(typeof(KILNEHBPDGI), "LJPKMABIAME")]
    [HarmonyPostfix]
    public static void KILNEHBPDGI_LJPKMABIAME()
    {
        Debug.unityLogger.logEnabled = Plugin.Instance.EnableGameUnityLog.Value;
        Debug.unityLogger.filterLogType = Plugin.Instance.GameUnityLogLevel.Value.ToLower() == "error" ? LogType.Error :
            Plugin.Instance.GameUnityLogLevel.Value.ToLower() == "warning" ? LogType.Warning : LogType.Log;
    }
}