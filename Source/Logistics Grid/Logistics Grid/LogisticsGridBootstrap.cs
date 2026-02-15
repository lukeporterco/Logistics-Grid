using HarmonyLib;
using Verse;

namespace Logistics_Grid
{
    [StaticConstructorOnStartup]
    internal static class LogisticsGridBootstrap
    {
        private const string HarmonyId = "lukeporter.logisticsgrid";

        static LogisticsGridBootstrap()
        {
            new Harmony(HarmonyId).PatchAll();
        }
    }
}
