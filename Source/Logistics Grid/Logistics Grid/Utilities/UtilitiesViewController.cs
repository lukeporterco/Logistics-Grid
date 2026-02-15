using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal static class UtilitiesViewController
    {
        private const int PruneIntervalTicks = 250;
        private static readonly Dictionary<int, bool> mapEnabledById = new Dictionary<int, bool>();
        private static readonly List<int> staleMapIds = new List<int>();
        private static int lastPruneTick = -1;

        public static bool Enabled => IsEnabledForMap(Find.CurrentMap);

        public static void Toggle()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            int uniqueId = map.uniqueID;
            bool currentlyEnabled;
            mapEnabledById.TryGetValue(uniqueId, out currentlyEnabled);
            mapEnabledById[uniqueId] = !currentlyEnabled;
        }

        public static void HandleHotkeys()
        {
            if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || !WorldRendererUtility.DrawingMap)
            {
                return;
            }

            PruneStaleMapEntries();

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
            PruneStaleMapEntries();
            return IsEnabledForMap(map)
                && Current.ProgramState == ProgramState.Playing
                && map != null
                && WorldRendererUtility.DrawingMap;
        }

        private static bool IsEnabledForMap(Map map)
        {
            if (map == null)
            {
                return false;
            }

            bool enabled;
            return mapEnabledById.TryGetValue(map.uniqueID, out enabled) && enabled;
        }

        private static void PruneStaleMapEntries()
        {
            if (Current.ProgramState != ProgramState.Playing || Current.Game == null || Find.TickManager == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame - lastPruneTick < PruneIntervalTicks)
            {
                return;
            }

            lastPruneTick = ticksGame;
            staleMapIds.Clear();

            foreach (KeyValuePair<int, bool> entry in mapEnabledById)
            {
                bool mapStillExists = false;
                List<Map> maps = Current.Game.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    if (maps[i].uniqueID == entry.Key)
                    {
                        mapStillExists = true;
                        break;
                    }
                }

                if (!mapStillExists)
                {
                    staleMapIds.Add(entry.Key);
                }
            }

            for (int i = 0; i < staleMapIds.Count; i++)
            {
                mapEnabledById.Remove(staleMapIds[i]);
            }
        }
    }
}
