using System.Collections.Generic;
using Logistics_Grid.Components;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesPowerUsersLayer : IUtilitiesOverlayLayer
    {
        private static readonly Color PowerUsersHighlightColor = new Color(1f, 0.82f, 0.35f, 0.72f);

        public int DrawOrder => 110;

        public void Draw(Map map, MapComponent_LogisticsGrid component)
        {
            _ = map;
            if (!UtilitiesOverlaySettingsCache.ShowPowerUsersOverlay || component.PowerUserCount == 0)
            {
                return;
            }

            List<IntVec3> powerUserCells = component.PowerUserCells;
            if (powerUserCells.Count == 0)
            {
                return;
            }

            float cellSize = Find.CameraDriver.CellSizePixels;
            float markerSize = Mathf.Max(2f, cellSize * 0.36f);
            float halfMarkerSize = markerSize * 0.5f;
            for (int i = 0; i < powerUserCells.Count; i++)
            {
                Vector2 screenPos = GenMapUI.LabelDrawPosFor(powerUserCells[i]);
                Rect markerRect = new Rect(screenPos.x - halfMarkerSize, screenPos.y - halfMarkerSize, markerSize, markerSize);
                Widgets.DrawBoxSolid(markerRect, PowerUsersHighlightColor);
            }
        }
    }
}
