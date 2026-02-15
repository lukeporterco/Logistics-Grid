using Logistics_Grid.Components;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesWorldDimLayer : IUtilitiesOverlayLayer
    {
        private const float DimMarginCells = 2f;
        private static readonly float DimAltitude = Altitudes.AltitudeFor(AltitudeLayer.MapDataOverlay);

        public int DrawOrder => 0;

        public void Draw(Map map, MapComponent_LogisticsGrid component)
        {
            _ = map;
            _ = component;

            if (!UtilitiesOverlaySettingsCache.ShouldDrawWorldDim)
            {
                return;
            }

            CameraDriver cameraDriver = Find.CameraDriver;
            if (cameraDriver == null)
            {
                return;
            }

            CellRect viewRect = cameraDriver.CurrentViewRect;
            if (viewRect.IsEmpty)
            {
                return;
            }

            Vector3 center = viewRect.CenterVector3;
            center.y = DimAltitude;

            float width = viewRect.Width + DimMarginCells;
            float depth = viewRect.Height + DimMarginCells;
            Color dimColor = new Color(0f, 0f, 0f, UtilitiesOverlaySettingsCache.WorldDimAlpha);
            Material dimMaterial = SolidColorMaterials.SimpleSolidColorMaterial(dimColor, false);
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(width, 1f, depth));
            Graphics.DrawMesh(MeshPool.plane10, matrix, dimMaterial, 0);
        }
    }
}
