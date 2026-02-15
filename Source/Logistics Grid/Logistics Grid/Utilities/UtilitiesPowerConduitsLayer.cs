using Logistics_Grid.Domains.Power;
using Logistics_Grid.Framework;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesPowerConduitsLayer : IUtilitiesOverlayLayer
    {
        private const int BucketStandard = 0;
        private const int BucketHidden = 1;
        private const int BucketWaterproof = 2;

        private static readonly Color StandardConduitPathColor = new Color(0.26f, 0.90f, 0.30f, 1f);
        private static readonly Color HiddenConduitPathColor = new Color(0.58f, 0.98f, 0.54f, 1f);
        private static readonly Color WaterproofConduitPathColor = new Color(0.10f, 0.60f, 0.16f, 1f);

        private static readonly CapsuleStrokeStyle ConduitCapsuleStyle = new CapsuleStrokeStyle(
            strokeWidth: 0.24f,
            strokeAltitude: Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0015f,
            alpha: 1f,
            capSegments: 8);

        private static readonly CapsuleStrokeRenderer StrokeRenderer = new CapsuleStrokeRenderer(3, ConduitCapsuleStyle);

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
                PowerConduitType conduitType = powerCache.GetConduitTypeAt(cell);
                int bucket = GetBucketForConduitType(conduitType);
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

        private static int GetBucketForConduitType(PowerConduitType conduitType)
        {
            switch (conduitType)
            {
                case PowerConduitType.Hidden:
                    return BucketHidden;
                case PowerConduitType.Waterproof:
                    return BucketWaterproof;
                default:
                    return BucketStandard;
            }
        }

        private static Material GetConduitMaterialForBucket(int bucket)
        {
            switch (bucket)
            {
                case BucketHidden:
                    return SolidColorMaterials.SimpleSolidColorMaterial(StrokeRenderer.ApplyAlpha(HiddenConduitPathColor), false);
                case BucketWaterproof:
                    return SolidColorMaterials.SimpleSolidColorMaterial(StrokeRenderer.ApplyAlpha(WaterproofConduitPathColor), false);
                default:
                    return SolidColorMaterials.SimpleSolidColorMaterial(StrokeRenderer.ApplyAlpha(StandardConduitPathColor), false);
            }
        }
    }
}
