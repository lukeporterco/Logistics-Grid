using System.Collections.Generic;
using Logistics_Grid.Components;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesPowerConduitsLayer : IUtilitiesOverlayLayer
    {
        private static readonly Color ConduitHighlightColor = new Color(0.35f, 0.85f, 1f, 0.92f);

        public int DrawOrder => 100;

        public void Draw(Map map, MapComponent_LogisticsGrid component)
        {
            _ = map;
            if (!UtilitiesOverlaySettingsCache.ShowPowerConduitsOverlay || component.PowerConduitCount == 0)
            {
                return;
            }

            List<IntVec3> conduitCells = component.PowerConduitCells;
            if (conduitCells.Count == 0)
            {
                return;
            }

            float cellSize = Find.CameraDriver.CellSizePixels;
            float markerSize = Mathf.Max(2f, cellSize * 0.40f);
            float halfMarkerSize = markerSize * 0.5f;
            for (int i = 0; i < conduitCells.Count; i++)
            {
                Vector2 screenPos = GenMapUI.LabelDrawPosFor(conduitCells[i]);
                Rect markerRect = new Rect(screenPos.x - halfMarkerSize, screenPos.y - halfMarkerSize, markerSize, markerSize);
                Widgets.DrawBoxSolid(markerRect, ConduitHighlightColor);
            }
        }
    }
}
