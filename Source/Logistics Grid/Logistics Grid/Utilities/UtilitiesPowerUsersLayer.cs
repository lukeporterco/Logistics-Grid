using System.Collections.Generic;
using Logistics_Grid.Domains.Power;
using Logistics_Grid.Framework;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesPowerUsersLayer : IUtilitiesOverlayLayer
    {
        private const float RingOuterRadius = 0.26f;
        private const float RingInnerRadius = 0.16f;
        private const float CoreRadius = 0.085f;
        private const int RingStripCount = 12;
        private const int CoreStripCount = 8;
        private const float DimmedRingAlphaMultiplier = 0.65f;
        private const float MinimumStorageFill = 0.125f;

        private static readonly Color ProducerRingColor = new Color(0.23f, 0.62f, 1f, 0.84f);
        private static readonly Color ConsumerRingColor = new Color(1f, 0.39f, 0.19f, 0.86f);
        private static readonly Color NeutralCoreColor = new Color(0.82f, 0.82f, 0.82f, 0.30f);
        private static readonly Color FlowExportCoreColor = new Color(0.53f, 0.79f, 1f, 0.92f);
        private static readonly Color FlowImportCoreColor = new Color(1f, 0.58f, 0.38f, 0.92f);
        private static readonly Color ToggledOffCoreColor = new Color(1f, 1f, 1f, 0.98f);
        private static readonly Color FaultCoreColor = new Color(0.96f, 0.24f, 0.22f, 0.98f);
        private static readonly Color StorageCoreColor = new Color(1f, 0.92f, 0.42f, 0.98f);

        private static readonly float MarkerAltitude = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0025f;
        private static readonly float CoreAltitude = MarkerAltitude + 0.0001f;

        private static readonly RingStripSpec[] RingStripSpecs = BuildRingStripSpecs();
        private static readonly CoreStripSpec[] CoreStripSpecs = BuildCoreStripSpecs();

        private static readonly List<Matrix4x4> ProducerRingMatrices = new List<Matrix4x4>(1024);
        private static readonly List<Matrix4x4> ConsumerRingMatrices = new List<Matrix4x4>(1024);
        private static readonly List<Matrix4x4> ProducerRingDimmedMatrices = new List<Matrix4x4>(512);
        private static readonly List<Matrix4x4> ConsumerRingDimmedMatrices = new List<Matrix4x4>(512);

        private static readonly List<Matrix4x4> NeutralCoreMatrices = new List<Matrix4x4>(1024);
        private static readonly List<Matrix4x4> FlowExportCoreMatrices = new List<Matrix4x4>(512);
        private static readonly List<Matrix4x4> FlowImportCoreMatrices = new List<Matrix4x4>(512);
        private static readonly List<Matrix4x4> ToggledOffCoreMatrices = new List<Matrix4x4>(512);
        private static readonly List<Matrix4x4> FaultCoreMatrices = new List<Matrix4x4>(512);
        private static readonly List<Matrix4x4> StorageCoreMatrices = new List<Matrix4x4>(1024);

        private static int cachedMapIndex = -1;
        private static int cachedGeneration = -1;
        private static CellRect cachedViewRect;
        private static bool cachedHasViewRect;

        private struct RingStripSpec
        {
            public float OffsetX;
            public float OffsetZ;
            public float ThicknessX;
            public float ThicknessZ;
        }

        private struct CoreStripSpec
        {
            public float OffsetX;
            public float ThicknessX;
            public float HalfZ;
        }

        public string LayerId => "LogisticsGrid.Layer.PowerUsers";

        public void Draw(UtilityOverlayContext context, UtilityOverlayChannelDef channelDef)
        {
            _ = channelDef;
            PowerDomainCache powerCache = context.Component.GetDomainCache<PowerDomainCache>(PowerDomainProvider.DomainIdValue);
            if (powerCache == null || powerCache.PowerUserCount == 0)
            {
                return;
            }

            IReadOnlyList<PowerNodeMarker> nodeMarkers = powerCache.NodeMarkers;
            if (nodeMarkers.Count == 0)
            {
                return;
            }

            if (ShouldRebuildGeometryCache(context, powerCache))
            {
                RebuildGeometryCache(context, nodeMarkers);
                cachedMapIndex = context.Map.Index;
                cachedGeneration = powerCache.CacheGeneration;
                cachedHasViewRect = context.HasViewRect;
                cachedViewRect = context.ViewRect;
            }

            DrawMatrices(ProducerRingMatrices, GetProducerRingMaterial(), context.Map);
            DrawMatrices(ConsumerRingMatrices, GetConsumerRingMaterial(), context.Map);
            DrawMatrices(ProducerRingDimmedMatrices, GetProducerRingDimmedMaterial(), context.Map);
            DrawMatrices(ConsumerRingDimmedMatrices, GetConsumerRingDimmedMaterial(), context.Map);

            DrawMatrices(NeutralCoreMatrices, GetNeutralCoreMaterial(), context.Map);
            DrawMatrices(FlowExportCoreMatrices, GetFlowExportCoreMaterial(), context.Map);
            DrawMatrices(FlowImportCoreMatrices, GetFlowImportCoreMaterial(), context.Map);
            DrawMatrices(ToggledOffCoreMatrices, GetToggledOffCoreMaterial(), context.Map);
            DrawMatrices(FaultCoreMatrices, GetFaultCoreMaterial(), context.Map);
            DrawMatrices(StorageCoreMatrices, GetStorageCoreMaterial(), context.Map);
        }

        private static bool ShouldRebuildGeometryCache(UtilityOverlayContext context, PowerDomainCache powerCache)
        {
            if (cachedMapIndex != context.Map.Index || cachedGeneration != powerCache.CacheGeneration)
            {
                return true;
            }

            if (cachedHasViewRect != context.HasViewRect)
            {
                return true;
            }

            if (!context.HasViewRect)
            {
                return false;
            }

            CellRect currentView = context.ViewRect;
            return cachedViewRect.minX != currentView.minX
                || cachedViewRect.minZ != currentView.minZ
                || cachedViewRect.maxX != currentView.maxX
                || cachedViewRect.maxZ != currentView.maxZ;
        }

        private static void RebuildGeometryCache(UtilityOverlayContext context, IReadOnlyList<PowerNodeMarker> nodeMarkers)
        {
            ClearGeometryCache();

            CellRect visibleRect = context.ViewRect;
            bool hasVisibleRect = context.HasViewRect;
            for (int i = 0; i < nodeMarkers.Count; i++)
            {
                PowerNodeMarker marker = nodeMarkers[i];
                if (marker.Building == null)
                {
                    continue;
                }

                CellRect occupiedRect = marker.OccupiedRect;
                if (occupiedRect.IsEmpty)
                {
                    occupiedRect = CellRect.SingleCell(marker.Building.Position);
                }

                if (hasVisibleRect && !occupiedRect.Overlaps(visibleRect))
                {
                    continue;
                }

                foreach (IntVec3 cell in occupiedRect.Cells)
                {
                    if (hasVisibleRect && !visibleRect.Contains(cell))
                    {
                        continue;
                    }

                    Vector3 center = cell.ToVector3Shifted();
                    AddRing(center, marker);
                    AddCore(center, marker);
                }
            }
        }

        private static void AddRing(Vector3 center, PowerNodeMarker marker)
        {
            List<Matrix4x4> bucket = ResolveRingBucket(marker);
            if (bucket == null)
            {
                return;
            }

            for (int i = 0; i < RingStripSpecs.Length; i++)
            {
                RingStripSpec spec = RingStripSpecs[i];
                Vector3 stripCenter = new Vector3(center.x + spec.OffsetX, MarkerAltitude, center.z + spec.OffsetZ);
                Vector3 stripScale = new Vector3(spec.ThicknessX, 1f, spec.ThicknessZ);
                bucket.Add(Matrix4x4.TRS(stripCenter, Quaternion.identity, stripScale));
            }
        }

        private static List<Matrix4x4> ResolveRingBucket(PowerNodeMarker marker)
        {
            bool dimmed = marker.CoreState == PowerNodeCoreState.ToggledOff;
            bool producerFamily = marker.Identity == PowerNodeIdentity.ProducerCapable || marker.Identity == PowerNodeIdentity.Storage;
            if (producerFamily)
            {
                return dimmed ? ProducerRingDimmedMatrices : ProducerRingMatrices;
            }

            return dimmed ? ConsumerRingDimmedMatrices : ConsumerRingMatrices;
        }

        private static void AddCore(Vector3 center, PowerNodeMarker marker)
        {
            List<Matrix4x4> bucket = ResolveCoreBucket(marker.CoreState);
            if (bucket == null)
            {
                return;
            }

            float fill01 = marker.CoreState == PowerNodeCoreState.StorageCharge
                ? Mathf.Max(MinimumStorageFill, Mathf.Clamp01(marker.CoreValue01))
                : 1f;
            AddCoreFill(center, fill01, bucket);
        }

        private static List<Matrix4x4> ResolveCoreBucket(PowerNodeCoreState coreState)
        {
            switch (coreState)
            {
                case PowerNodeCoreState.FlowExport:
                    return FlowExportCoreMatrices;
                case PowerNodeCoreState.FlowImport:
                    return FlowImportCoreMatrices;
                case PowerNodeCoreState.ToggledOff:
                    return ToggledOffCoreMatrices;
                case PowerNodeCoreState.Fault:
                    return FaultCoreMatrices;
                case PowerNodeCoreState.StorageCharge:
                    return StorageCoreMatrices;
                default:
                    return NeutralCoreMatrices;
            }
        }

        private static void AddCoreFill(Vector3 center, float fill01, List<Matrix4x4> bucket)
        {
            float fillTop = -CoreRadius + (2f * CoreRadius * Mathf.Clamp01(fill01));
            for (int i = 0; i < CoreStripSpecs.Length; i++)
            {
                CoreStripSpec spec = CoreStripSpecs[i];
                float stripMin = -spec.HalfZ;
                float stripMax = spec.HalfZ;
                float visibleMin = Mathf.Max(stripMin, -CoreRadius);
                float visibleMax = Mathf.Min(stripMax, fillTop);
                float visibleThickness = visibleMax - visibleMin;
                if (visibleThickness <= 0.0005f)
                {
                    continue;
                }

                float offsetZ = (visibleMin + visibleMax) * 0.5f;
                Vector3 stripCenter = new Vector3(center.x + spec.OffsetX, CoreAltitude, center.z + offsetZ);
                Vector3 stripScale = new Vector3(spec.ThicknessX, 1f, visibleThickness);
                bucket.Add(Matrix4x4.TRS(stripCenter, Quaternion.identity, stripScale));
            }
        }

        private static void ClearGeometryCache()
        {
            ProducerRingMatrices.Clear();
            ConsumerRingMatrices.Clear();
            ProducerRingDimmedMatrices.Clear();
            ConsumerRingDimmedMatrices.Clear();
            NeutralCoreMatrices.Clear();
            FlowExportCoreMatrices.Clear();
            FlowImportCoreMatrices.Clear();
            ToggledOffCoreMatrices.Clear();
            FaultCoreMatrices.Clear();
            StorageCoreMatrices.Clear();
        }

        private static void DrawMatrices(List<Matrix4x4> matrices, Material material, Map map)
        {
            if (matrices.Count == 0 || material == null)
            {
                return;
            }

            for (int i = 0; i < matrices.Count; i++)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrices[i], material, 0);
                UtilityOverlayProfiler.RecordDrawSubmission(map, 1);
            }
        }

        private static RingStripSpec[] BuildRingStripSpecs()
        {
            List<RingStripSpec> specs = new List<RingStripSpec>(RingStripCount * 2);
            float diameter = RingOuterRadius * 2f;
            for (int i = 0; i < RingStripCount; i++)
            {
                float x0 = -RingOuterRadius + (diameter * i / RingStripCount);
                float x1 = -RingOuterRadius + (diameter * (i + 1) / RingStripCount);
                float midX = (x0 + x1) * 0.5f;
                float halfOuterZ = Mathf.Sqrt(Mathf.Max(0f, (RingOuterRadius * RingOuterRadius) - (midX * midX)));
                float absMidX = Mathf.Abs(midX);
                float halfInnerZ = absMidX < RingInnerRadius
                    ? Mathf.Sqrt(Mathf.Max(0f, (RingInnerRadius * RingInnerRadius) - (midX * midX)))
                    : 0f;

                float bandThicknessZ = halfOuterZ - halfInnerZ;
                if (bandThicknessZ <= 0.0005f)
                {
                    continue;
                }

                float stripWidthX = x1 - x0;
                float offsetZ = (halfInnerZ + halfOuterZ) * 0.5f;
                specs.Add(new RingStripSpec
                {
                    OffsetX = midX,
                    OffsetZ = offsetZ,
                    ThicknessX = stripWidthX,
                    ThicknessZ = bandThicknessZ
                });
                specs.Add(new RingStripSpec
                {
                    OffsetX = midX,
                    OffsetZ = -offsetZ,
                    ThicknessX = stripWidthX,
                    ThicknessZ = bandThicknessZ
                });
            }

            return specs.ToArray();
        }

        private static CoreStripSpec[] BuildCoreStripSpecs()
        {
            List<CoreStripSpec> specs = new List<CoreStripSpec>(CoreStripCount);
            float diameter = CoreRadius * 2f;
            for (int i = 0; i < CoreStripCount; i++)
            {
                float x0 = -CoreRadius + (diameter * i / CoreStripCount);
                float x1 = -CoreRadius + (diameter * (i + 1) / CoreStripCount);
                float midX = (x0 + x1) * 0.5f;
                float halfZ = Mathf.Sqrt(Mathf.Max(0f, (CoreRadius * CoreRadius) - (midX * midX)));
                if (halfZ <= 0.00025f)
                {
                    continue;
                }

                specs.Add(new CoreStripSpec
                {
                    OffsetX = midX,
                    ThicknessX = x1 - x0,
                    HalfZ = halfZ
                });
            }

            return specs.ToArray();
        }

        private static Material GetProducerRingMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(ProducerRingColor, false);
        }

        private static Material GetConsumerRingMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(ConsumerRingColor, false);
        }

        private static Material GetProducerRingDimmedMaterial()
        {
            Color color = ProducerRingColor;
            color.a *= DimmedRingAlphaMultiplier;
            return SolidColorMaterials.SimpleSolidColorMaterial(color, false);
        }

        private static Material GetConsumerRingDimmedMaterial()
        {
            Color color = ConsumerRingColor;
            color.a *= DimmedRingAlphaMultiplier;
            return SolidColorMaterials.SimpleSolidColorMaterial(color, false);
        }

        private static Material GetNeutralCoreMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(NeutralCoreColor, false);
        }

        private static Material GetFlowExportCoreMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(FlowExportCoreColor, false);
        }

        private static Material GetFlowImportCoreMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(FlowImportCoreColor, false);
        }

        private static Material GetToggledOffCoreMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(ToggledOffCoreColor, false);
        }

        private static Material GetFaultCoreMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(FaultCoreColor, false);
        }

        private static Material GetStorageCoreMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(StorageCoreColor, false);
        }
    }
}
