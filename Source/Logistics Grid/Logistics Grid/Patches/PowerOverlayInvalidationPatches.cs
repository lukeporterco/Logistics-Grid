using HarmonyLib;
using Logistics_Grid.Components;
using RimWorld;
using Verse;

namespace Logistics_Grid.Patches
{
    internal static class PowerOverlayInvalidationUtility
    {
        public static bool IsPowerOverlayRelevant(Thing thing)
        {
            ThingDef def = thing?.def;
            if (def == null)
            {
                return false;
            }

            ThingDef conduitDef = ThingDefOf.PowerConduit;
            if ((def.building != null && def.building.isPowerConduit)
                || (conduitDef != null && def == conduitDef)
                || def.defName == "PowerConduit")
            {
                return true;
            }

            ThingWithComps thingWithComps = thing as ThingWithComps;
            return thingWithComps != null
                && (thingWithComps.GetComp<CompPower>() != null || thingWithComps.GetComp<CompPowerTrader>() != null);
        }
    }

    [HarmonyPatch(typeof(Building), nameof(Building.SpawnSetup))]
    internal static class BuildingSpawnSetupPowerOverlayInvalidationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Building __instance, Map map)
        {
            if (!PowerOverlayInvalidationUtility.IsPowerOverlayRelevant(__instance))
            {
                return;
            }

            map?.GetComponent<MapComponent_LogisticsGrid>()?.MarkDirty();
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
            if (!PowerOverlayInvalidationUtility.IsPowerOverlayRelevant(__instance))
            {
                return;
            }

            __state?.GetComponent<MapComponent_LogisticsGrid>()?.MarkDirty();
        }
    }
}
