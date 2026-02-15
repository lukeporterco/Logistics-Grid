using RimWorld;
using Verse;

namespace Logistics_Grid
{
    [DefOf]
    internal static class LogisticsGridKeyBindingDefOf
    {
#pragma warning disable CS0649
        public static KeyBindingDef LogisticsGrid_ToggleUtilitiesView;
#pragma warning restore CS0649

        static LogisticsGridKeyBindingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LogisticsGridKeyBindingDefOf));
        }
    }
}
