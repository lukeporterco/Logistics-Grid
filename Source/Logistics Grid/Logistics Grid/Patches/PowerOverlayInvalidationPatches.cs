using HarmonyLib;
using Logistics_Grid.Components;
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
}
