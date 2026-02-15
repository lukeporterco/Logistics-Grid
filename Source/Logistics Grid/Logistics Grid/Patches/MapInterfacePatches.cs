using HarmonyLib;
using RimWorld;
using Verse;
using Logistics_Grid.Utilities;

namespace Logistics_Grid.Patches
{
    [HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_BeforeMainTabs))]
    internal static class MapInterfaceBeforeMainTabsPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!UtilitiesViewController.ShouldDrawForCurrentMap() || UtilitiesOverlaySettingsCache.FadeWorldAndUi)
            {
                return;
            }

            UtilitiesOverlayManager.Draw(Find.CurrentMap);
        }
    }

    [HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_AfterMainTabs))]
    internal static class MapInterfaceAfterMainTabsPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!UtilitiesViewController.ShouldDrawForCurrentMap() || !UtilitiesOverlaySettingsCache.FadeWorldAndUi)
            {
                return;
            }

            UtilitiesOverlayManager.Draw(Find.CurrentMap);
        }
    }
}
