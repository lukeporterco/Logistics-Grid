using System.Collections.Generic;
using Logistics_Grid.Components;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal static class UtilitiesOverlayManager
    {
        private static readonly List<IUtilitiesOverlayLayer> Layers = new List<IUtilitiesOverlayLayer>
        {
            new UtilitiesBaseFadeLayer()
        };

        static UtilitiesOverlayManager()
        {
            Layers.Sort((left, right) => left.DrawOrder.CompareTo(right.DrawOrder));
        }

        public static void Draw(Map map, MapComponent_LogisticsGrid component)
        {
            if (map == null || component == null)
            {
                return;
            }

            for (int i = 0; i < Layers.Count; i++)
            {
                Layers[i].Draw(map);
            }
        }
    }
}
