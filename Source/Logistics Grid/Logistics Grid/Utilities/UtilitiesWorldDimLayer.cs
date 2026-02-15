using Logistics_Grid.Framework;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesWorldDimLayer : IUtilitiesOverlayLayer
    {
        private const float DimMarginCells = 2f;
        private static readonly float DimAltitude = Altitudes.AltitudeFor(AltitudeLayer.MapDataOverlay);

        public string LayerId => "LogisticsGrid.Layer.WorldDim";

        public void Draw(UtilityOverlayContext context, UtilityOverlayChannelDef channelDef)
        {
            _ = channelDef;

            if (Prefs.DevMode && UtilitiesOverlaySettingsCache.DebugDisableWorldDim)
            {
                return;
            }

            CellRect viewRect = context.ViewRect;
            if (!context.HasViewRect || viewRect.IsEmpty)
            {
                return;
            }

            Vector3 center = viewRect.CenterVector3;
            center.y = DimAltitude;

            float width = viewRect.Width + DimMarginCells;
            float depth = viewRect.Height + DimMarginCells;
            Material material = GetDimMaterial(UtilitiesOverlaySettingsCache.WorldDimAlpha);
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(width, 1f, depth));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            UtilityOverlayProfiler.RecordDrawSubmission(context.Map, 1);
        }

        private static Material GetDimMaterial(float alpha)
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 0f, alpha), false);
        }
    }
}
