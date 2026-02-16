using HarmonyLib;
using Logistics_Grid.Components;
using RimWorld;
using Verse;

namespace Logistics_Grid.Patches
{
    internal static class PowerOverlayInvalidationSignals
    {
        public const string PowerTurnedOn = "PowerTurnedOn";
        public const string PowerTurnedOff = "PowerTurnedOff";
        public const string FlickedOn = "FlickedOn";
        public const string FlickedOff = "FlickedOff";
    }

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

    [HarmonyPatch(typeof(CompPowerTrader), nameof(CompPowerTrader.PowerOn), MethodType.Setter)]
    internal static class CompPowerTraderPowerOnSetterPowerOverlayInvalidationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CompPowerTrader __instance)
        {
            PowerOverlayInvalidationUtility.MarkDirtyForCompParent(__instance, null);
        }
    }

    [HarmonyPatch(typeof(CompPowerTrader), nameof(CompPowerTrader.ReceiveCompSignal))]
    internal static class CompPowerTraderReceiveCompSignalPowerOverlayInvalidationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CompPowerTrader __instance, string signal)
        {
            if (!PowerOverlayInvalidationUtility.IsRelevantPowerSignal(signal))
            {
                return;
            }

            PowerOverlayInvalidationUtility.MarkDirtyForCompParent(__instance, null);
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

        public static bool IsRelevantPowerSignal(string signal)
        {
            if (string.IsNullOrEmpty(signal))
            {
                return false;
            }

            return signal.Equals(PowerOverlayInvalidationSignals.PowerTurnedOn, System.StringComparison.Ordinal)
                || signal.Equals(PowerOverlayInvalidationSignals.PowerTurnedOff, System.StringComparison.Ordinal)
                || signal.Equals(PowerOverlayInvalidationSignals.FlickedOn, System.StringComparison.Ordinal)
                || signal.Equals(PowerOverlayInvalidationSignals.FlickedOff, System.StringComparison.Ordinal);
        }
    }
}
