using HarmonyLib;
using RimWorld;
using Logistics_Grid.Utilities;

namespace Logistics_Grid.Patches
{
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootUpdate))]
    internal static class UIRootPlayPatches
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            UtilitiesViewController.HandleHotkeys();
        }
    }
}
