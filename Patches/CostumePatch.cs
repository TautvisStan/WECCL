using WECCL.Content;

namespace WECCL.Patches;

[HarmonyPatch]
public class CostumePatch
{
    [HarmonyPatch(typeof(Costume), nameof(Costume.EJKFJMMEFIK))]
    [HarmonyPostfix]
    public static void CostumePostfix(ref Color __result, Costume __instance)
    {
        if (__instance.texture[3] > VanillaCounts.Data.MaterialCounts[3])
        {
            __result *= GetSkinColor(__instance.texture[3]);
        }
        else if (__instance.texture[3] < -VanillaCounts.Data.FaceFemaleCount)
        {
            __result *= GetSkinColor(__instance.texture[3]);
        }
    }
}