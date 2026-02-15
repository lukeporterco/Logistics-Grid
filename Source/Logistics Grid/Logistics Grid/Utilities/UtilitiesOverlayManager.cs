using System.Collections.Generic;
using Logistics_Grid.Components;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal static class UtilitiesOverlayManager
    {
        private static readonly List<IUtilitiesOverlayLayer> UiLayers = new List<IUtilitiesOverlayLayer>
        {
            new UtilitiesBaseFadeLayer(),
            new UtilitiesPowerConduitsUiLayer()
        };

        static UtilitiesOverlayManager()
        {
            UiLayers.Sort((left, right) => left.DrawOrder.CompareTo(right.DrawOrder));
        }

        public static void DrawUi(Map map, MapComponent_LogisticsGrid component)
        {
            if (map == null || component == null || !UtilitiesViewController.ShouldDrawForMap(map))
            {
                return;
            }

            for (int i = 0; i < UiLayers.Count; i++)
            {
                UiLayers[i].Draw(map, component);
            }
        }

    }
}
