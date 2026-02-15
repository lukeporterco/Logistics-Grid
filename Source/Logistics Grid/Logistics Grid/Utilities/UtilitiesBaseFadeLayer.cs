using UnityEngine;
using Logistics_Grid.Components;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesBaseFadeLayer : IUtilitiesOverlayLayer
    {
        public int DrawOrder => 0;

        public void Draw(Map map, MapComponent_LogisticsGrid component)
        {
            _ = map;
            _ = component;
            Rect screenRect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
            Widgets.DrawBoxSolid(screenRect, UtilitiesOverlaySettingsCache.TintColor);
        }
    }
}
