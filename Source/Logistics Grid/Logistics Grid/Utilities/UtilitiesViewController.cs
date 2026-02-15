using RimWorld.Planet;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal static class UtilitiesViewController
    {
        public static bool Enabled { get; private set; }

        public static void Toggle()
        {
            Enabled = !Enabled;
        }

        public static void HandleHotkeys()
        {
            if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || !WorldRendererUtility.DrawingMap)
            {
                return;
            }

            KeyBindingDef toggleKeybind = LogisticsGridKeyBindingDefOf.LogisticsGrid_ToggleUtilitiesView;
            if (toggleKeybind != null && toggleKeybind.JustPressed)
            {
                Toggle();
            }
        }

        public static bool ShouldDrawForCurrentMap()
        {
            return ShouldDrawForMap(Find.CurrentMap);
        }

        public static bool ShouldDrawForMap(Map map)
        {
            return Enabled
                && Current.ProgramState == ProgramState.Playing
                && map != null
                && WorldRendererUtility.DrawingMap;
        }
    }
}
