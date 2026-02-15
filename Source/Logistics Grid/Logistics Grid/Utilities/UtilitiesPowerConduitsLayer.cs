using System.Collections.Generic;
using Logistics_Grid.Components;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    [StaticConstructorOnStartup]
    internal sealed class UtilitiesPowerConduitsLayer : IUtilitiesOverlayLayer
    {
        private static readonly Color StandardConduitPathColor = new Color(0.26f, 0.90f, 0.30f, 0.96f);
        private static readonly Color HiddenConduitPathColor = new Color(0.58f, 0.98f, 0.54f, 0.96f);
        private static readonly Color WaterproofConduitPathColor = new Color(0.10f, 0.60f, 0.16f, 0.96f);
        private static readonly Material StandardConduitPathMaterial;
        private static readonly Material HiddenConduitPathMaterial;
        private static readonly Material WaterproofConduitPathMaterial;
        private static readonly Vector3 EastWestScale = new Vector3(1f, 1f, 0.20f);
        private static readonly Vector3 NorthSouthScale = new Vector3(0.20f, 1f, 1f);
        private static readonly float PathAltitude = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0015f;
        private static readonly Vector3 EndpointScale = new Vector3(0.32f, 1f, 0.32f);
        private static readonly float EndpointAltitude = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0016f;

        static UtilitiesPowerConduitsLayer()
        {
            StandardConduitPathMaterial = SolidColorMaterials.SimpleSolidColorMaterial(StandardConduitPathColor, false);
            HiddenConduitPathMaterial = SolidColorMaterials.SimpleSolidColorMaterial(HiddenConduitPathColor, false);
            WaterproofConduitPathMaterial = SolidColorMaterials.SimpleSolidColorMaterial(WaterproofConduitPathColor, false);
        }

        public int DrawOrder => 100;

        public void Draw(Map map, MapComponent_LogisticsGrid component)
        {
            if (!UtilitiesOverlaySettingsCache.ShowPowerConduitsOverlay || component.PowerConduitCount == 0)
            {
                return;
            }

            DrawConnectedPaths(component);
        }

        private static void DrawConnectedPaths(MapComponent_LogisticsGrid component)
        {
            List<IntVec3> conduitCells = component.PowerConduitCells;
            if (conduitCells.Count == 0)
            {
                return;
            }

            for (int i = 0; i < conduitCells.Count; i++)
            {
                IntVec3 cell = conduitCells[i];
                Vector3 center = cell.ToVector3Shifted();
                Material pathMaterial = GetConduitPathMaterial(component.GetConduitTypeAt(cell));

                if (component.HasConduitAt(cell + IntVec3.East))
                {
                    DrawSegment(center + new Vector3(0.5f, 0f, 0f), EastWestScale, pathMaterial);
                }

                if (component.HasConduitAt(cell + IntVec3.North))
                {
                    DrawSegment(center + new Vector3(0f, 0f, 0.5f), NorthSouthScale, pathMaterial);
                }

                int neighborCount = 0;
                if (component.HasConduitAt(cell + IntVec3.North)) neighborCount++;
                if (component.HasConduitAt(cell + IntVec3.South)) neighborCount++;
                if (component.HasConduitAt(cell + IntVec3.East)) neighborCount++;
                if (component.HasConduitAt(cell + IntVec3.West)) neighborCount++;

                // Endpoints only: marker on dead-ends (or isolated single-cell conduits), not every conduit cell.
                if (neighborCount <= 1)
                {
                    DrawEndpoint(center, pathMaterial);
                }
            }
        }

        private static void DrawSegment(Vector3 center, Vector3 scale, Material pathMaterial)
        {
            center.y = PathAltitude;
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, pathMaterial, 0);
        }

        private static void DrawEndpoint(Vector3 center, Material pathMaterial)
        {
            center.y = EndpointAltitude;
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, EndpointScale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, pathMaterial, 0);
        }

        private static Material GetConduitPathMaterial(MapComponent_LogisticsGrid.ConduitType conduitType)
        {
            if (conduitType == MapComponent_LogisticsGrid.ConduitType.Hidden)
            {
                return HiddenConduitPathMaterial;
            }

            if (conduitType == MapComponent_LogisticsGrid.ConduitType.Waterproof)
            {
                return WaterproofConduitPathMaterial;
            }

            return StandardConduitPathMaterial;
        }
    }
}
