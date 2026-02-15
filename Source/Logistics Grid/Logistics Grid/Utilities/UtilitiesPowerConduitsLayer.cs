using System.Collections.Generic;
using Logistics_Grid.Domains.Power;
using Logistics_Grid.Framework;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesPowerConduitsLayer : IUtilitiesOverlayLayer
    {
        private static readonly Color StandardConduitPathColor = new Color(0.26f, 0.90f, 0.30f, 0.96f);
        private static readonly Color HiddenConduitPathColor = new Color(0.58f, 0.98f, 0.54f, 0.96f);
        private static readonly Color WaterproofConduitPathColor = new Color(0.10f, 0.60f, 0.16f, 0.96f);
        private static readonly Vector3 EastWestScale = new Vector3(1f, 1f, 0.20f);
        private static readonly Vector3 NorthSouthScale = new Vector3(0.20f, 1f, 1f);
        private static readonly float PathAltitude = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0015f;
        private static readonly Vector3 EndpointScale = new Vector3(0.32f, 1f, 0.32f);
        private static readonly float EndpointAltitude = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0016f;

        private static readonly List<Matrix4x4> StandardPathMatrices = new List<Matrix4x4>(1024);
        private static readonly List<Matrix4x4> HiddenPathMatrices = new List<Matrix4x4>(1024);
        private static readonly List<Matrix4x4> WaterproofPathMatrices = new List<Matrix4x4>(1024);

        private static readonly List<Matrix4x4> StandardEndpointMatrices = new List<Matrix4x4>(256);
        private static readonly List<Matrix4x4> HiddenEndpointMatrices = new List<Matrix4x4>(256);
        private static readonly List<Matrix4x4> WaterproofEndpointMatrices = new List<Matrix4x4>(256);

        public string LayerId => "LogisticsGrid.Layer.PowerConduits";

        public void Draw(UtilityOverlayContext context, UtilityOverlayChannelDef channelDef)
        {
            _ = channelDef;

            PowerDomainCache powerCache = context.Component.GetDomainCache<PowerDomainCache>(PowerDomainProvider.DomainIdValue);
            if (powerCache == null || powerCache.PowerConduitCount == 0)
            {
                return;
            }

            DrawConnectedPaths(context, powerCache);
            FlushPathBatches(context.Map);
        }

        private static void DrawConnectedPaths(UtilityOverlayContext context, PowerDomainCache powerCache)
        {
            ClearBatches();

            List<IntVec3> conduitCells = powerCache.PowerConduitCells;
            if (conduitCells.Count == 0)
            {
                return;
            }

            for (int i = 0; i < conduitCells.Count; i++)
            {
                IntVec3 cell = conduitCells[i];
                if (!context.IsCellVisible(cell))
                {
                    continue;
                }

                Vector3 center = cell.ToVector3Shifted();
                PowerConduitType conduitType = powerCache.GetConduitTypeAt(cell);
                byte neighborMask = powerCache.GetNeighborMaskAt(cell);

                if ((neighborMask & PowerDomainCache.NeighborEast) != 0)
                {
                    AddPathMatrix(conduitType, BuildMatrix(center + new Vector3(0.5f, 0f, 0f), EastWestScale, PathAltitude));
                }

                if ((neighborMask & PowerDomainCache.NeighborNorth) != 0)
                {
                    AddPathMatrix(conduitType, BuildMatrix(center + new Vector3(0f, 0f, 0.5f), NorthSouthScale, PathAltitude));
                }

                // Endpoints only: marker on dead-ends (or isolated single-cell conduits), not every conduit cell.
                if (PowerDomainCache.CountNeighbors(neighborMask) <= 1)
                {
                    AddEndpointMatrix(conduitType, BuildMatrix(center, EndpointScale, EndpointAltitude));
                }
            }
        }

        private static Matrix4x4 BuildMatrix(Vector3 center, Vector3 scale, float altitude)
        {
            center.y = altitude;
            return Matrix4x4.TRS(center, Quaternion.identity, scale);
        }

        private static void AddPathMatrix(PowerConduitType conduitType, Matrix4x4 matrix)
        {
            switch (conduitType)
            {
                case PowerConduitType.Hidden:
                    HiddenPathMatrices.Add(matrix);
                    break;
                case PowerConduitType.Waterproof:
                    WaterproofPathMatrices.Add(matrix);
                    break;
                default:
                    StandardPathMatrices.Add(matrix);
                    break;
            }
        }

        private static void AddEndpointMatrix(PowerConduitType conduitType, Matrix4x4 matrix)
        {
            switch (conduitType)
            {
                case PowerConduitType.Hidden:
                    HiddenEndpointMatrices.Add(matrix);
                    break;
                case PowerConduitType.Waterproof:
                    WaterproofEndpointMatrices.Add(matrix);
                    break;
                default:
                    StandardEndpointMatrices.Add(matrix);
                    break;
            }
        }

        private static void FlushPathBatches(Map map)
        {
            Material standardMaterial = GetStandardConduitPathMaterial();
            Material hiddenMaterial = GetHiddenConduitPathMaterial();
            Material waterproofMaterial = GetWaterproofConduitPathMaterial();

            DrawBatched(StandardPathMatrices, standardMaterial, map);
            DrawBatched(HiddenPathMatrices, hiddenMaterial, map);
            DrawBatched(WaterproofPathMatrices, waterproofMaterial, map);
            DrawBatched(StandardEndpointMatrices, standardMaterial, map);
            DrawBatched(HiddenEndpointMatrices, hiddenMaterial, map);
            DrawBatched(WaterproofEndpointMatrices, waterproofMaterial, map);
        }

        private static void DrawBatched(List<Matrix4x4> matrices, Material material, Map map)
        {
            for (int i = 0; i < matrices.Count; i++)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrices[i], material, 0);
                UtilityOverlayProfiler.RecordDrawSubmission(map, 1);
            }
        }

        private static void ClearBatches()
        {
            StandardPathMatrices.Clear();
            HiddenPathMatrices.Clear();
            WaterproofPathMatrices.Clear();
            StandardEndpointMatrices.Clear();
            HiddenEndpointMatrices.Clear();
            WaterproofEndpointMatrices.Clear();
        }

        private static Material GetStandardConduitPathMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(StandardConduitPathColor, false);
        }

        private static Material GetHiddenConduitPathMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(HiddenConduitPathColor, false);
        }

        private static Material GetWaterproofConduitPathMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(WaterproofConduitPathColor, false);
        }

    }
}
