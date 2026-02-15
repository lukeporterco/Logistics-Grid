using HarmonyLib;
using Logistics_Grid.Components;
using Logistics_Grid.Utilities;
using RimWorld.Planet;
using Verse;

namespace Logistics_Grid.Patches
{
    [HarmonyPatch(typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings))]
    internal static class DynamicDrawManagerDrawDynamicThingsPatch
    {
        private static readonly AccessTools.FieldRef<DynamicDrawManager, Map> MapFieldRef =
            AccessTools.FieldRefAccess<DynamicDrawManager, Map>("map");

        [HarmonyPostfix]
        private static void Postfix(DynamicDrawManager __instance)
        {
            if (!UtilitiesViewController.Enabled || !WorldRendererUtility.DrawingMap)
            {
                return;
            }

            Map renderedMap = Find.CurrentMap;
            if (renderedMap == null)
            {
                return;
            }

            Map drawMap = MapFieldRef(__instance);
            if (drawMap == null || drawMap != renderedMap)
            {
                return;
            }

            MapComponent_LogisticsGrid component = drawMap.GetComponent<MapComponent_LogisticsGrid>();
            if (component == null)
            {
                return;
            }

            UtilitiesOverlayManager.DrawWorld(drawMap, component);
        }
    }
}
