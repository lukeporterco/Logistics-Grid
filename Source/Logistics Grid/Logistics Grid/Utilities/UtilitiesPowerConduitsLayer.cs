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
        private const int BucketFlickedOff = 2;
        private const int BucketUnpowered = 3;
        private const int BucketUnlinked = 4;

        private static readonly Color PoweredStatePathColor = new Color(0.20f, 0.95f, 0.20f, 1f);
        private static readonly Color TransientStatePathColor = new Color(1f, 0.90f, 0.12f, 1f);
        private static readonly Color FlickedOffStatePathColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color UnpoweredStatePathColor = new Color(0.96f, 0.25f, 0.22f, 1f);
        private static readonly Color UnlinkedStatePathColor = new Color(1.00f, 0.58f, 0.14f, 1f);

        private static readonly CapsuleStrokeStyle ConduitCapsuleStyle = new CapsuleStrokeStyle(
            strokeWidth: 0.24f,
            strokeAltitude: Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0015f,
            alpha: 1f,
            capSegments: 8);

        private static readonly CapsuleStrokeRenderer StrokeRenderer = new CapsuleStrokeRenderer(5, ConduitCapsuleStyle);

        public string LayerId => "LogisticsGrid.Layer.PowerConduits";

        public void Draw(UtilityOverlayContext context, UtilityOverlayChannelDef channelDef)
        {
            _ = channelDef;

            PowerDomainCache powerCache = context.Component.GetDomainCache<PowerDomainCache>(PowerDomainProvider.DomainIdValue);
            if (powerCache == null || powerCache.PowerConduitCount == 0)
            {
                return;
            }

            StrokeRenderer.Clear();
            DrawConnectedPaths(context, powerCache);
            StrokeRenderer.Flush(context.Map, GetConduitMaterialForBucket);
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
                int bucket = GetBucketForNetState(powerCache.GetNetState(netId));
                byte neighborMask = powerCache.GetNeighborMaskAt(cell);
                int neighborCount = PowerDomainCache.CountNeighbors(neighborMask);

                if ((neighborMask & PowerDomainCache.NeighborEast) != 0)
                {
                    IntVec3 eastCell = cell + IntVec3.East;
                    int eastNeighborCount = PowerDomainCache.CountNeighbors(powerCache.GetNeighborMaskAt(eastCell));
                    StrokeRenderer.AddCardinalSegment(
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
                    StrokeRenderer.AddCardinalSegment(
                        center,
                        CapsuleDirection.North,
                        bucket,
                        capAtStart: neighborCount == 1,
                        capAtEnd: northNeighborCount == 1);
                }

                if (ShouldDrawHub(neighborMask))
                {
                    StrokeRenderer.AddHub(center, bucket);
                }
            }
        }

        private static bool ShouldDrawHub(byte neighborMask)
        {
            int neighborCount = PowerDomainCache.CountNeighbors(neighborMask);
            // Keep hub fill where rounded shape is visually important,
            // but avoid extra overdraw at T-junctions.
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

                // Corners get a rounded hub; straight segments do not.
                return !verticalStraight && !horizontalStraight;
            }

            return false;
        }

        private static int GetBucketForNetState(PowerNetOverlayState netState)
        {
            switch (netState)
            {
                case PowerNetOverlayState.Transient:
                    return BucketTransient;
                case PowerNetOverlayState.FlickedOff:
                    return BucketFlickedOff;
                case PowerNetOverlayState.Unpowered:
                    return BucketUnpowered;
                case PowerNetOverlayState.Unlinked:
                    return BucketUnlinked;
                default:
                    return BucketPowered;
            }
        }

        private static Material GetConduitMaterialForBucket(int bucket)
        {
            switch (bucket)
            {
                case BucketTransient:
                    return SolidColorMaterials.SimpleSolidColorMaterial(StrokeRenderer.ApplyAlpha(TransientStatePathColor), false);
                case BucketFlickedOff:
                    return SolidColorMaterials.SimpleSolidColorMaterial(StrokeRenderer.ApplyAlpha(FlickedOffStatePathColor), false);
                case BucketUnpowered:
                    return SolidColorMaterials.SimpleSolidColorMaterial(StrokeRenderer.ApplyAlpha(UnpoweredStatePathColor), false);
                case BucketUnlinked:
                    return SolidColorMaterials.SimpleSolidColorMaterial(StrokeRenderer.ApplyAlpha(UnlinkedStatePathColor), false);
                default:
                    return SolidColorMaterials.SimpleSolidColorMaterial(StrokeRenderer.ApplyAlpha(PoweredStatePathColor), false);
            }
        }
    }
}
