using System.Collections.Generic;
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

        public static void Draw(Map map)
        {
            if (map == null)
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
