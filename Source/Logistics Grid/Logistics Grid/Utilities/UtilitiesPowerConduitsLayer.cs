using Logistics_Grid.Domains.Power;
using Logistics_Grid.Framework;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesPowerConduitsLayer : IUtilitiesOverlayLayer
    {
        private const int BucketPowered = 0;
        private const int BucketTransient = 1;
        private const int BucketUnpoweredGrey = 2;
        private const int BucketUnpoweredAlert = 3;
        private const int BucketUnlinked = 4;
        private const int BucketDistressed = 5;
        private const int BucketCount = 6;

        private static readonly Color PoweredInnerColor = new Color(0.20f, 0.95f, 0.20f, 0.96f);
        private static readonly Color DisconnectedGreyInnerColor = new Color(0.38f, 0.38f, 0.38f, 0.68f);
        private static readonly Color UnpoweredAlertInnerColor = new Color(0.95f, 0.24f, 0.22f, 0.90f);

        private static readonly CapsuleStrokeStyle OuterCapsuleStyle = new CapsuleStrokeStyle(
            strokeWidth: 0.30f,
            strokeAltitude: Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.00135f,
            alpha: 1f,
            capSegments: 8);

        private static readonly CapsuleStrokeStyle InnerCapsuleStyle = new CapsuleStrokeStyle(
            strokeWidth: 0.18f,
            strokeAltitude: Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0015f,
            alpha: 1f,
            capSegments: 8);

        private static readonly CapsuleStrokeRenderer OuterRenderer = new CapsuleStrokeRenderer(BucketCount, OuterCapsuleStyle);
        private static readonly CapsuleStrokeRenderer InnerRenderer = new CapsuleStrokeRenderer(BucketCount, InnerCapsuleStyle);

        public string LayerId => "LogisticsGrid.Layer.PowerConduits";

        public void Draw(UtilityOverlayContext context, UtilityOverlayChannelDef channelDef)
        {
            _ = channelDef;

            PowerDomainCache powerCache = context.Component.GetDomainCache<PowerDomainCache>(PowerDomainProvider.DomainIdValue);
            if (powerCache == null || powerCache.PowerConduitCount == 0)
            {
                return;
            }

            OuterRenderer.Clear();
            InnerRenderer.Clear();
            DrawConnectedPaths(context, powerCache);
            OuterRenderer.Flush(context.Map, GetOuterMaterialForBucket);
            InnerRenderer.Flush(context.Map, GetInnerMaterialForBucket);
        }

        private static void DrawConnectedPaths(UtilityOverlayContext context, PowerDomainCache powerCache)
        {
            System.Collections.Generic.List<IntVec3> conduitCells = powerCache.PowerConduitCells;
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
                int netId = powerCache.GetNetIdAt(cell);
                PowerNetOverlayState state = powerCache.GetNetState(netId);
                byte neighborMask = powerCache.GetNeighborMaskAt(cell);
                int neighborCount = PowerDomainCache.CountNeighbors(neighborMask);

                if ((neighborMask & PowerDomainCache.NeighborEast) != 0)
                {
                    IntVec3 eastCell = cell + IntVec3.East;
                    int eastNeighborCount = PowerDomainCache.CountNeighbors(powerCache.GetNeighborMaskAt(eastCell));
                    int bucket = ResolveSegmentBucket(state, cell, CapsuleDirection.East);
                    OuterRenderer.AddCardinalSegment(
                        center,
                        CapsuleDirection.East,
                        bucket,
                        capAtStart: neighborCount == 1,
                        capAtEnd: eastNeighborCount == 1);
                    InnerRenderer.AddCardinalSegment(
                        center,
                        CapsuleDirection.East,
                        bucket,
                        capAtStart: neighborCount == 1,
                        capAtEnd: eastNeighborCount == 1);
                }

                if ((neighborMask & PowerDomainCache.NeighborNorth) != 0)
                {
                    IntVec3 northCell = cell + IntVec3.North;
                    int northNeighborCount = PowerDomainCache.CountNeighbors(powerCache.GetNeighborMaskAt(northCell));
                    int bucket = ResolveSegmentBucket(state, cell, CapsuleDirection.North);
                    OuterRenderer.AddCardinalSegment(
                        center,
                        CapsuleDirection.North,
                        bucket,
                        capAtStart: neighborCount == 1,
                        capAtEnd: northNeighborCount == 1);
                    InnerRenderer.AddCardinalSegment(
                        center,
                        CapsuleDirection.North,
                        bucket,
                        capAtStart: neighborCount == 1,
                        capAtEnd: northNeighborCount == 1);
                }

                if (ShouldDrawHub(neighborMask))
                {
                    int hubBucket = ResolveHubBucket(state);
                    OuterRenderer.AddHub(center, hubBucket);
                    InnerRenderer.AddHub(center, hubBucket);
                }
            }
        }

        private static int ResolveSegmentBucket(PowerNetOverlayState state, IntVec3 cell, CapsuleDirection direction)
        {
            switch (state)
            {
                case PowerNetOverlayState.Unpowered:
                    return IsZebraAlertChunk(cell, direction) ? BucketUnpoweredAlert : BucketUnpoweredGrey;
                case PowerNetOverlayState.Distressed:
                    return BucketDistressed;
                case PowerNetOverlayState.Unlinked:
                    return BucketUnlinked;
                case PowerNetOverlayState.FlickedOff:
                    // Kept for enum compatibility; switched-off zebra is intentionally omitted until a true net-level gate model exists.
                    return BucketUnlinked;
                case PowerNetOverlayState.Transient:
                    return BucketTransient;
                default:
                    return BucketPowered;
            }
        }

        private static int ResolveHubBucket(PowerNetOverlayState state)
        {
            switch (state)
            {
                case PowerNetOverlayState.Unpowered:
                    return BucketUnpoweredGrey;
                case PowerNetOverlayState.Distressed:
                    return BucketDistressed;
                case PowerNetOverlayState.Unlinked:
                    return BucketUnlinked;
                case PowerNetOverlayState.FlickedOff:
                    return BucketUnlinked;
                case PowerNetOverlayState.Transient:
                    return BucketTransient;
                default:
                    return BucketPowered;
            }
        }

        private static bool IsZebraAlertChunk(IntVec3 cell, CapsuleDirection direction)
        {
            int paritySeed = cell.x + cell.z;
            if (direction == CapsuleDirection.North || direction == CapsuleDirection.South)
            {
                paritySeed++;
            }

            return (paritySeed & 1) != 0;
        }

        private static bool ShouldDrawHub(byte neighborMask)
        {
            int neighborCount = PowerDomainCache.CountNeighbors(neighborMask);
            if (neighborCount == 0 || neighborCount == 4)
            {
                return true;
            }

            if (neighborCount == 2)
            {
                bool verticalStraight =
                    (neighborMask & PowerDomainCache.NeighborNorth) != 0
                    && (neighborMask & PowerDomainCache.NeighborSouth) != 0;
                bool horizontalStraight =
                    (neighborMask & PowerDomainCache.NeighborEast) != 0
                    && (neighborMask & PowerDomainCache.NeighborWest) != 0;
                return !verticalStraight && !horizontalStraight;
            }

            return false;
        }

        private static Color GetInnerColorForBucket(int bucket)
        {
            switch (bucket)
            {
                case BucketUnpoweredGrey:
                case BucketUnlinked:
                    return DisconnectedGreyInnerColor;
                case BucketUnpoweredAlert:
                    return UnpoweredAlertInnerColor;
                case BucketDistressed:
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 6f);
                    Color pulsingColor = Color.Lerp(new Color(1f, 0.90f, 0.18f, 1f), new Color(0.96f, 0.16f, 0.12f, 1f), pulse);
                    pulsingColor.a = Mathf.Lerp(0.82f, 0.96f, pulse);
                    return pulsingColor;
                }
                case BucketTransient:
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 6f);
                    Color pulsingColor = Color.Lerp(new Color(0.20f, 0.95f, 0.20f, 1f), new Color(0.80f, 1f, 0.22f, 1f), pulse);
                    pulsingColor.a = Mathf.Lerp(0.20f, 1f, pulse);
                    return pulsingColor;
                }
                default:
                    return PoweredInnerColor;
            }
        }

        private static Color GetOuterColorForBucket(int bucket)
        {
            Color inner = GetInnerColorForBucket(bucket);
            Color outer = Color.Lerp(inner, Color.black, 0.70f);
            outer.a = Mathf.Min(inner.a * 0.45f, 0.50f);
            return outer;
        }

        private static Material GetOuterMaterialForBucket(int bucket)
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(OuterRenderer.ApplyAlpha(GetOuterColorForBucket(bucket)), false);
        }

        private static Material GetInnerMaterialForBucket(int bucket)
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(InnerRenderer.ApplyAlpha(GetInnerColorForBucket(bucket)), false);
        }
    }
}
