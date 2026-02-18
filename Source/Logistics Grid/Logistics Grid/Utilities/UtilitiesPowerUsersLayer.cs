using System.Collections.Generic;
using Logistics_Grid.Domains.Power;
using Logistics_Grid.Framework;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesPowerUsersLayer : IUtilitiesOverlayLayer
    {
        private const float DefaultRingOuterRadius = 0.26f;
        private const float DefaultRingInnerRadius = 0.20f;
        private const float DefaultCoreRadius = 0.12f;
        private const float MinimumRingOuterRadius = 0.05f;
        private const float MinimumRingThickness = 0.015f;
        private const float FootprintOutlineThickness = 0.075f;
        private const float MinimumCoreRadius = 0.02f;
        private const float MaximumCoreToInnerRadiusRatio = 0.98f;
        private const float ScaleGrowthPerSqrtArea = 0.35f;
        private const float MinimumMarkerScale = 1f;
        private const float MaximumMarkerScale = 1.7f;
        private const int RingStripCount = 12;
        private const int CoreStripCount = 8;
        private const float DimmedRingAlphaMultiplier = 0.65f;
        private const float MinimumStorageFill = 0.125f;

        private static readonly Color ProducerRingColor = new Color(0.23f, 0.62f, 1f, 0.84f);
        private static readonly Color ConsumerRingColor = new Color(1f, 0.39f, 0.19f, 0.86f);
        private static readonly Color ProducerOutlineColor = new Color(0.07f, 0.44f, 1f, 0.98f);
        private static readonly Color ConsumerOutlineColor = new Color(1f, 0.31f, 0.11f, 0.98f);
        private static readonly Color NeutralCoreColor = new Color(0.82f, 0.82f, 0.82f, 0.30f);
        private static readonly Color FlowExportCoreColor = new Color(0.53f, 0.79f, 1f, 0.92f);
        private static readonly Color FlowImportCoreColor = new Color(1f, 0.58f, 0.38f, 0.92f);
        private static readonly Color ToggledOffCoreColor = new Color(1f, 1f, 1f, 0.98f);
        private static readonly Color FaultCoreColor = new Color(0.96f, 0.24f, 0.22f, 0.98f);
        private static readonly Color StorageCoreColor = new Color(1f, 0.92f, 0.42f, 0.98f);

        private static readonly float MarkerAltitude = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0025f;
        private static readonly float OutlineAltitude = MarkerAltitude - 0.0001f;
        private static readonly float CoreAltitude = MarkerAltitude + 0.0001f;

        private static RingStripSpec[] ringStripSpecs = BuildRingStripSpecs(DefaultRingOuterRadius, DefaultRingInnerRadius);
        private static CoreStripSpec[] coreStripSpecs = BuildCoreStripSpecs(DefaultCoreRadius);
        private static float cachedRingOuterRadius = DefaultRingOuterRadius;
        private static float cachedRingInnerRadius = DefaultRingInnerRadius;
        private static float cachedCoreRadius = DefaultCoreRadius;

        private static readonly List<Matrix4x4> ProducerRingMatrices = new List<Matrix4x4>(1024);
        private static readonly List<Matrix4x4> ConsumerRingMatrices = new List<Matrix4x4>(1024);
        private static readonly List<Matrix4x4> ProducerRingDimmedMatrices = new List<Matrix4x4>(512);
        private static readonly List<Matrix4x4> ConsumerRingDimmedMatrices = new List<Matrix4x4>(512);
        private static readonly List<Matrix4x4> ProducerOutlineMatrices = new List<Matrix4x4>(1024);
        private static readonly List<Matrix4x4> ConsumerOutlineMatrices = new List<Matrix4x4>(1024);
        private static readonly List<Matrix4x4> ProducerOutlineDimmedMatrices = new List<Matrix4x4>(512);
        private static readonly List<Matrix4x4> ConsumerOutlineDimmedMatrices = new List<Matrix4x4>(512);

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

            EnsureGeometryStyle(channelDef);

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
            DrawMatrices(ProducerOutlineMatrices, GetProducerOutlineMaterial(), context.Map);
            DrawMatrices(ConsumerOutlineMatrices, GetConsumerOutlineMaterial(), context.Map);
            DrawMatrices(ProducerOutlineDimmedMatrices, GetProducerOutlineDimmedMaterial(), context.Map);
            DrawMatrices(ConsumerOutlineDimmedMatrices, GetConsumerOutlineDimmedMaterial(), context.Map);

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

                Vector3 center = CalculateMarkerCenter(occupiedRect);
                float markerScale = CalculateMarkerScale(occupiedRect);
                AddOutline(occupiedRect, marker);
                AddRing(center, marker, markerScale);
                AddCore(center, marker, markerScale);
            }
        }

        private static void AddOutline(CellRect occupiedRect, PowerNodeMarker marker)
        {
            List<Matrix4x4> bucket = ResolveOutlineBucket(marker);
            if (bucket == null)
            {
                return;
            }

            int width = Mathf.Max(1, occupiedRect.Width);
            int height = Mathf.Max(1, occupiedRect.Height);
            float minX = occupiedRect.minX;
            float minZ = occupiedRect.minZ;
            float maxX = minX + width;
            float maxZ = minZ + height;

            Vector3 northCenter = new Vector3(minX + (width * 0.5f), OutlineAltitude, maxZ);
            Vector3 southCenter = new Vector3(minX + (width * 0.5f), OutlineAltitude, minZ);
            Vector3 eastCenter = new Vector3(maxX, OutlineAltitude, minZ + (height * 0.5f));
            Vector3 westCenter = new Vector3(minX, OutlineAltitude, minZ + (height * 0.5f));

            Vector3 horizontalScale = new Vector3(width, 1f, FootprintOutlineThickness);
            Vector3 verticalScale = new Vector3(FootprintOutlineThickness, 1f, height);
            bucket.Add(Matrix4x4.TRS(northCenter, Quaternion.identity, horizontalScale));
            bucket.Add(Matrix4x4.TRS(southCenter, Quaternion.identity, horizontalScale));
            bucket.Add(Matrix4x4.TRS(eastCenter, Quaternion.identity, verticalScale));
            bucket.Add(Matrix4x4.TRS(westCenter, Quaternion.identity, verticalScale));
        }

        private static void AddRing(Vector3 center, PowerNodeMarker marker, float markerScale)
        {
            List<Matrix4x4> bucket = ResolveRingBucket(marker);
            if (bucket == null)
            {
                return;
            }

            for (int i = 0; i < ringStripSpecs.Length; i++)
            {
                RingStripSpec spec = ringStripSpecs[i];
                Vector3 stripCenter = new Vector3(center.x + (spec.OffsetX * markerScale), MarkerAltitude, center.z + (spec.OffsetZ * markerScale));
                Vector3 stripScale = new Vector3(spec.ThicknessX * markerScale, 1f, spec.ThicknessZ * markerScale);
                bucket.Add(Matrix4x4.TRS(stripCenter, Quaternion.identity, stripScale));
            }
        }

        private static List<Matrix4x4> ResolveOutlineBucket(PowerNodeMarker marker)
        {
            bool dimmed = marker.CoreState == PowerNodeCoreState.ToggledOff;
            bool producerFamily = marker.Identity == PowerNodeIdentity.ProducerCapable || marker.Identity == PowerNodeIdentity.Storage;
            if (producerFamily)
            {
                return dimmed ? ProducerOutlineDimmedMatrices : ProducerOutlineMatrices;
            }

            return dimmed ? ConsumerOutlineDimmedMatrices : ConsumerOutlineMatrices;
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

        private static void AddCore(Vector3 center, PowerNodeMarker marker, float markerScale)
        {
            List<Matrix4x4> bucket = ResolveCoreBucket(marker.CoreState);
            if (bucket == null)
            {
                return;
            }

            float scaledCoreRadius = cachedCoreRadius * markerScale;
            float fill01 = marker.CoreState == PowerNodeCoreState.StorageCharge
                ? Mathf.Max(MinimumStorageFill, Mathf.Clamp01(marker.CoreValue01))
                : 1f;
            AddCoreFill(center, scaledCoreRadius, markerScale, fill01, bucket);
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

        private static void AddCoreFill(
            Vector3 center,
            float scaledCoreRadius,
            float markerScale,
            float fill01,
            List<Matrix4x4> bucket)
        {
            float fillTop = -scaledCoreRadius + (2f * scaledCoreRadius * Mathf.Clamp01(fill01));
            for (int i = 0; i < coreStripSpecs.Length; i++)
            {
                CoreStripSpec spec = coreStripSpecs[i];
                float scaledHalfZ = spec.HalfZ * markerScale;
                float stripMin = -scaledHalfZ;
                float stripMax = scaledHalfZ;
                float visibleMin = Mathf.Max(stripMin, -scaledCoreRadius);
                float visibleMax = Mathf.Min(stripMax, fillTop);
                float visibleThickness = visibleMax - visibleMin;
                if (visibleThickness <= 0.0005f)
                {
                    continue;
                }

                float offsetZ = (visibleMin + visibleMax) * 0.5f;
                Vector3 stripCenter = new Vector3(center.x + (spec.OffsetX * markerScale), CoreAltitude, center.z + offsetZ);
                Vector3 stripScale = new Vector3(spec.ThicknessX * markerScale, 1f, visibleThickness);
                bucket.Add(Matrix4x4.TRS(stripCenter, Quaternion.identity, stripScale));
            }
        }

        private static float CalculateMarkerScale(CellRect occupiedRect)
        {
            int width = Mathf.Max(1, occupiedRect.Width);
            int height = Mathf.Max(1, occupiedRect.Height);
            float area = width * height;
            float sqrtArea = Mathf.Sqrt(area);
            float scale = 1f + ((sqrtArea - 1f) * ScaleGrowthPerSqrtArea);
            return Mathf.Clamp(scale, MinimumMarkerScale, MaximumMarkerScale);
        }

        private static Vector3 CalculateMarkerCenter(CellRect occupiedRect)
        {
            int width = Mathf.Max(1, occupiedRect.Width);
            int height = Mathf.Max(1, occupiedRect.Height);
            float centerX = occupiedRect.minX + (width * 0.5f);
            float centerZ = occupiedRect.minZ + (height * 0.5f);
            return new Vector3(centerX, 0f, centerZ);
        }

        private static void ClearGeometryCache()
        {
            ProducerRingMatrices.Clear();
            ConsumerRingMatrices.Clear();
            ProducerRingDimmedMatrices.Clear();
            ConsumerRingDimmedMatrices.Clear();
            ProducerOutlineMatrices.Clear();
            ConsumerOutlineMatrices.Clear();
            ProducerOutlineDimmedMatrices.Clear();
            ConsumerOutlineDimmedMatrices.Clear();
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

        private static void EnsureGeometryStyle(UtilityOverlayChannelDef channelDef)
        {
            float requestedOuter = channelDef != null ? channelDef.powerUserRingOuterRadius : DefaultRingOuterRadius;
            float requestedInner = channelDef != null ? channelDef.powerUserRingInnerRadius : DefaultRingInnerRadius;
            float requestedCore = channelDef != null ? channelDef.powerUserCoreRadius : DefaultCoreRadius;

            float ringOuterRadius = Mathf.Max(MinimumRingOuterRadius, requestedOuter);
            float ringInnerRadius = Mathf.Clamp(requestedInner, MinimumRingOuterRadius * 0.5f, ringOuterRadius - MinimumRingThickness);
            float coreRadius = Mathf.Clamp(requestedCore, MinimumCoreRadius, ringInnerRadius * MaximumCoreToInnerRadiusRatio);

            if (Mathf.Abs(cachedRingOuterRadius - ringOuterRadius) <= 0.0001f
                && Mathf.Abs(cachedRingInnerRadius - ringInnerRadius) <= 0.0001f
                && Mathf.Abs(cachedCoreRadius - coreRadius) <= 0.0001f)
            {
                return;
            }

            cachedRingOuterRadius = ringOuterRadius;
            cachedRingInnerRadius = ringInnerRadius;
            cachedCoreRadius = coreRadius;
            ringStripSpecs = BuildRingStripSpecs(ringOuterRadius, ringInnerRadius);
            coreStripSpecs = BuildCoreStripSpecs(coreRadius);
            cachedGeneration = -1;
        }

        private static RingStripSpec[] BuildRingStripSpecs(float ringOuterRadius, float ringInnerRadius)
        {
            List<RingStripSpec> specs = new List<RingStripSpec>(RingStripCount * 2);
            float diameter = ringOuterRadius * 2f;
            for (int i = 0; i < RingStripCount; i++)
            {
                float x0 = -ringOuterRadius + (diameter * i / RingStripCount);
                float x1 = -ringOuterRadius + (diameter * (i + 1) / RingStripCount);
                float midX = (x0 + x1) * 0.5f;
                float halfOuterZ = Mathf.Sqrt(Mathf.Max(0f, (ringOuterRadius * ringOuterRadius) - (midX * midX)));
                float absMidX = Mathf.Abs(midX);
                float halfInnerZ = absMidX < ringInnerRadius
                    ? Mathf.Sqrt(Mathf.Max(0f, (ringInnerRadius * ringInnerRadius) - (midX * midX)))
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

        private static CoreStripSpec[] BuildCoreStripSpecs(float coreRadius)
        {
            List<CoreStripSpec> specs = new List<CoreStripSpec>(CoreStripCount);
            float diameter = coreRadius * 2f;
            for (int i = 0; i < CoreStripCount; i++)
            {
                float x0 = -coreRadius + (diameter * i / CoreStripCount);
                float x1 = -coreRadius + (diameter * (i + 1) / CoreStripCount);
                float midX = (x0 + x1) * 0.5f;
                float halfZ = Mathf.Sqrt(Mathf.Max(0f, (coreRadius * coreRadius) - (midX * midX)));
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

        private static Material GetProducerOutlineMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(ProducerOutlineColor, false);
        }

        private static Material GetConsumerOutlineMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(ConsumerOutlineColor, false);
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

        private static Material GetProducerOutlineDimmedMaterial()
        {
            Color color = ProducerOutlineColor;
            color.a *= DimmedRingAlphaMultiplier;
            return SolidColorMaterials.SimpleSolidColorMaterial(color, false);
        }

        private static Material GetConsumerOutlineDimmedMaterial()
        {
            Color color = ConsumerOutlineColor;
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
