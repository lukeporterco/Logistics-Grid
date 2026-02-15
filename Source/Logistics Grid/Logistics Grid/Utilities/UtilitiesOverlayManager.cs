using System.Collections.Generic;
using Logistics_Grid.Components;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal static class UtilitiesOverlayManager
    {
        private static readonly List<IUtilitiesOverlayLayer> WorldLayers = new List<IUtilitiesOverlayLayer>
        {
            // dim first, wires second: dim the world, then redraw utility visuals above it so they stay bright.
            new UtilitiesWorldDimLayer(),
            new UtilitiesPowerConduitsLayer(),
            new UtilitiesPowerUsersLayer()
        };

        private static int lastDrawFrame = -1;
        private static int lastDrawMapIndex = -1;

        static UtilitiesOverlayManager()
        {
            WorldLayers.Sort((left, right) => left.DrawOrder.CompareTo(right.DrawOrder));
        }

        public static void DrawWorld(Map map, MapComponent_LogisticsGrid component)
        {
            if (map == null || component == null || !UtilitiesViewController.ShouldDrawForMap(map) || map != Find.CurrentMap)
            {
                return;
            }

            int frame = Time.frameCount;
            if (frame == lastDrawFrame && map.Index == lastDrawMapIndex)
            {
                return;
            }

            lastDrawFrame = frame;
            lastDrawMapIndex = map.Index;

            for (int i = 0; i < WorldLayers.Count; i++)
            {
                WorldLayers[i].Draw(map, component);
            }
        }
    }
}
