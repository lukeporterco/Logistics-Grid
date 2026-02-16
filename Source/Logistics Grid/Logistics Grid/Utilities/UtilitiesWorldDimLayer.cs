using Logistics_Grid.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesWorldDimLayer : IUtilitiesOverlayLayer
    {
        private const float DimMarginCells = 2f;
        private const float GridLineThickness = 0.035f;
        private const float WireframeLineThickness = 0.075f;
        private const float GridAlpha = 0.22f;

        private static readonly float BackgroundAltitude = Altitudes.AltitudeFor(AltitudeLayer.MapDataOverlay);
        private static readonly float GridAltitude = BackgroundAltitude + 0.0001f;
        private static readonly float WireframeAltitude = GridAltitude + 0.0001f;

        private static readonly Color GreyBaseColor = new Color(0.20f, 0.20f, 0.20f, 1f);
        private static readonly Color GridColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        private static readonly Color WireframeColor = new Color(0f, 0f, 0f, 0.50f);

        private static int cachedMapIndex = -1;
        private static CellRect cachedRect;
        private static bool hasCachedRect;

        private static readonly System.Collections.Generic.List<Matrix4x4> GridMatrices = new System.Collections.Generic.List<Matrix4x4>(4096);
        private static readonly System.Collections.Generic.List<Matrix4x4> WireframeMatrices = new System.Collections.Generic.List<Matrix4x4>(4096);

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

            CellRect clampedViewRect = viewRect;
            clampedViewRect.ClipInsideMap(context.Map);
            if (clampedViewRect.IsEmpty)
            {
                return;
            }

            int meshCount = 0;
            DrawGreyBackground(clampedViewRect, ref meshCount);
            EnsureCachedGeometry(context.Map, clampedViewRect);
            DrawCachedGrid(ref meshCount);
            DrawCachedWireframe(ref meshCount);
            if (meshCount > 0)
            {
                UtilityOverlayProfiler.RecordDrawSubmission(context.Map, meshCount);
            }
        }

        private static void EnsureCachedGeometry(Map map, CellRect viewRect)
        {
            if (map == null)
            {
                return;
            }

            if (hasCachedRect
                && cachedMapIndex == map.Index
                && cachedRect.minX == viewRect.minX
                && cachedRect.minZ == viewRect.minZ
                && cachedRect.maxX == viewRect.maxX
                && cachedRect.maxZ == viewRect.maxZ)
            {
                return;
            }

            cachedMapIndex = map.Index;
            cachedRect = viewRect;
            hasCachedRect = true;

            RebuildGridMatrices(viewRect);
            RebuildWireframeMatrices(map, viewRect);
        }

        private static void DrawGreyBackground(CellRect viewRect, ref int meshCount)
        {
            Vector3 center = viewRect.CenterVector3;
            center.y = BackgroundAltitude;

            float width = viewRect.Width + DimMarginCells;
            float depth = viewRect.Height + DimMarginCells;
            Material material = GetGreyBackgroundMaterial();
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(width, 1f, depth));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            meshCount++;
        }

        private static void RebuildGridMatrices(CellRect viewRect)
        {
            GridMatrices.Clear();
            int minX = viewRect.minX;
            int maxXExclusive = viewRect.maxX + 1;
            int minZ = viewRect.minZ;
            int maxZExclusive = viewRect.maxZ + 1;

            float centerZ = minZ + (maxZExclusive - minZ) * 0.5f;
            float centerX = minX + (maxXExclusive - minX) * 0.5f;
            float verticalLength = maxZExclusive - minZ;
            float horizontalLength = maxXExclusive - minX;

            for (int x = minX; x <= maxXExclusive; x++)
            {
                Vector3 center = new Vector3(x, GridAltitude, centerZ);
                Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(GridLineThickness, 1f, verticalLength));
                GridMatrices.Add(matrix);
            }

            for (int z = minZ; z <= maxZExclusive; z++)
            {
                Vector3 center = new Vector3(centerX, GridAltitude, z);
                Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(horizontalLength, 1f, GridLineThickness));
                GridMatrices.Add(matrix);
            }
        }

        private static void DrawCachedGrid(ref int meshCount)
        {
            Material material = GetGridMaterial();
            for (int i = 0; i < GridMatrices.Count; i++)
            {
                Graphics.DrawMesh(MeshPool.plane10, GridMatrices[i], material, 0);
                meshCount++;
            }
        }

        private static void RebuildWireframeMatrices(Map map, CellRect viewRect)
        {
            WireframeMatrices.Clear();
            for (int x = viewRect.minX; x <= viewRect.maxX; x++)
            {
                for (int z = viewRect.minZ; z <= viewRect.maxZ; z++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    if (!IsWallLikeOrDoor(map, cell))
                    {
                        continue;
                    }

                    AddBoundaryIfExposed(map, cell, IntVec3.North, WireframeDirection.North);
                    AddBoundaryIfExposed(map, cell, IntVec3.South, WireframeDirection.South);
                    AddBoundaryIfExposed(map, cell, IntVec3.East, WireframeDirection.East);
                    AddBoundaryIfExposed(map, cell, IntVec3.West, WireframeDirection.West);
                }
            }
        }

        private static void DrawCachedWireframe(ref int meshCount)
        {
            Material material = GetWireframeMaterial();
            for (int i = 0; i < WireframeMatrices.Count; i++)
            {
                Graphics.DrawMesh(MeshPool.plane10, WireframeMatrices[i], material, 0);
                meshCount++;
            }
        }

        private static void AddBoundaryIfExposed(
            Map map,
            IntVec3 cell,
            IntVec3 neighborOffset,
            WireframeDirection edgeDirection)
        {
            IntVec3 neighborCell = cell + neighborOffset;
            if (neighborCell.InBounds(map) && IsWallLikeOrDoor(map, neighborCell))
            {
                return;
            }

            AddBoundaryEdge(cell, edgeDirection);
        }

        private static void AddBoundaryEdge(IntVec3 cell, WireframeDirection edgeDirection)
        {
            Vector3 center = cell.ToVector3();
            Vector3 scale = default(Vector3);

            switch (edgeDirection)
            {
                case WireframeDirection.North:
                    center += new Vector3(0.5f, WireframeAltitude, 1f);
                    scale = new Vector3(1f, 1f, WireframeLineThickness);
                    break;
                case WireframeDirection.South:
                    center += new Vector3(0.5f, WireframeAltitude, 0f);
                    scale = new Vector3(1f, 1f, WireframeLineThickness);
                    break;
                case WireframeDirection.East:
                    center += new Vector3(1f, WireframeAltitude, 0.5f);
                    scale = new Vector3(WireframeLineThickness, 1f, 1f);
                    break;
                default:
                    center += new Vector3(0f, WireframeAltitude, 0.5f);
                    scale = new Vector3(WireframeLineThickness, 1f, 1f);
                    break;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, scale);
            WireframeMatrices.Add(matrix);
        }

        private static bool IsWallLikeOrDoor(Map map, IntVec3 cell)
        {
            Building edifice = cell.GetEdifice(map);
            if (edifice == null)
            {
                return false;
            }

            if (edifice is Building_Door)
            {
                return true;
            }

            ThingDef def = edifice.def;
            if (def == null)
            {
                return false;
            }

            return def.passability == Traversability.Impassable && def.Fillage == FillCategory.Full;
        }

        private static Material GetGreyBackgroundMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(GreyBaseColor, false);
        }

        private static Material GetGridMaterial()
        {
            Color color = GridColor;
            color.a = GridAlpha;
            return SolidColorMaterials.SimpleSolidColorMaterial(color, false);
        }

        private static Material GetWireframeMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(WireframeColor, false);
        }

        private enum WireframeDirection : byte
        {
            North = 0,
            South = 1,
            East = 2,
            West = 3
        }
    }
}
