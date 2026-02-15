using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesBaseFadeLayer : IUtilitiesOverlayLayer
    {
        public int DrawOrder => 0;

        public void Draw(Map map)
        {
            _ = map;
            Rect screenRect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
            Widgets.DrawBoxSolid(screenRect, UtilitiesOverlaySettingsCache.TintColor);
        }
    }
}
