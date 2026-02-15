using HarmonyLib;
using Logistics_Grid.Components;
using RimWorld;
using Verse;

namespace Logistics_Grid.Patches
{
    [HarmonyPatch(typeof(Building), nameof(Building.SpawnSetup))]
    internal static class BuildingSpawnSetupPowerOverlayInvalidationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Building __instance, Map map)
        {
            if (__instance == null || map == null)
            {
                return;
            }

            map.GetComponent<MapComponent_LogisticsGrid>()?.MarkDirtyForThing(__instance);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
    internal static class ThingDeSpawnPowerOverlayInvalidationPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Thing __instance, out Map __state)
        {
            __state = __instance.MapHeld;
        }

        [HarmonyPostfix]
        private static void Postfix(Thing __instance, Map __state)
        {
            if (__instance == null || __state == null)
            {
                return;
            }

            __state.GetComponent<MapComponent_LogisticsGrid>()?.MarkDirtyForThing(__instance);
        }
    }

    [HarmonyPatch(typeof(CompPower), nameof(CompPower.PostSpawnSetup))]
    internal static class CompPowerPostSpawnSetupPowerOverlayInvalidationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CompPower __instance)
        {
            PowerOverlayInvalidationUtility.MarkDirtyForCompParent(__instance, null);
        }
    }

    [HarmonyPatch(typeof(CompPower), nameof(CompPower.PostDeSpawn))]
    internal static class CompPowerPostDeSpawnPowerOverlayInvalidationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CompPower __instance, Map map)
        {
            PowerOverlayInvalidationUtility.MarkDirtyForCompParent(__instance, map);
        }
    }

    internal static class PowerOverlayInvalidationUtility
    {
        public static void MarkDirtyForCompParent(CompPower compPower, Map fallbackMap)
        {
            if (compPower == null)
            {
                return;
            }

            ThingWithComps parent = compPower.parent;
            Map map = parent != null ? parent.MapHeld : null;
            if (map == null)
            {
                // PostDeSpawn can run after the parent loses MapHeld; use the method arg as fallback.
                map = fallbackMap;
            }

            if (parent == null || map == null)
            {
                return;
            }

            map.GetComponent<MapComponent_LogisticsGrid>()?.MarkDirtyForThing(parent);
        }
    }
}
