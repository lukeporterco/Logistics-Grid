using Logistics_Grid.Domains.Power;
using Logistics_Grid.Framework;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesPowerNetsLayer : IUtilitiesOverlayLayer
    {
        private static readonly Color[] NetPalette =
        {
            // High-contrast categorical palette with full rainbow coverage.
            new Color(0.90f, 0.20f, 0.20f, 1f), // red
            new Color(0.97f, 0.48f, 0.15f, 1f), // orange
            new Color(0.95f, 0.78f, 0.14f, 1f), // yellow
            new Color(0.50f, 0.85f, 0.22f, 1f), // yellow-green
            new Color(0.14f, 0.84f, 0.32f, 1f), // green
            new Color(0.12f, 0.82f, 0.66f, 1f), // cyan
            new Color(0.18f, 0.62f, 0.95f, 1f), // blue
            new Color(0.32f, 0.43f, 0.95f, 1f), // indigo
            new Color(0.58f, 0.30f, 0.92f, 1f), // violet
            new Color(0.87f, 0.24f, 0.80f, 1f), // magenta
            new Color(0.95f, 0.34f, 0.55f, 1f), // rose
            new Color(0.60f, 0.52f, 0.20f, 1f), // olive
            new Color(0.00f, 0.62f, 0.56f, 1f), // teal
            new Color(0.00f, 0.45f, 0.78f, 1f), // cobalt
            new Color(0.40f, 0.24f, 0.78f, 1f), // deep purple
            new Color(0.78f, 0.22f, 0.58f, 1f), // fuchsia
            new Color(0.86f, 0.38f, 0.12f, 1f), // burnt orange
            new Color(0.44f, 0.64f, 0.12f, 1f), // moss
            new Color(0.72f, 0.16f, 0.22f, 1f), // maroon
            new Color(0.98f, 0.56f, 0.24f, 1f), // amber
            new Color(0.26f, 0.72f, 0.20f, 1f), // leaf
            new Color(0.16f, 0.62f, 0.74f, 1f), // steel teal
            new Color(0.22f, 0.30f, 0.72f, 1f), // royal blue
            new Color(0.66f, 0.18f, 0.70f, 1f)  // plum
        };

        private static readonly CapsuleStrokeStyle NetCapsuleStyle = new CapsuleStrokeStyle(
            strokeWidth: 0.54f,
            strokeAltitude: Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0012f,
            alpha: 0.42f,
            capSegments: 8);

        private static readonly CapsuleStrokeRenderer StrokeRenderer = new CapsuleStrokeRenderer(NetPalette.Length, NetCapsuleStyle);

        public string LayerId => "LogisticsGrid.Layer.PowerNets";

        public void Draw(UtilityOverlayContext context, UtilityOverlayChannelDef channelDef)
        {
            _ = channelDef;

            PowerDomainCache powerCache = context.Component.GetDomainCache<PowerDomainCache>(PowerDomainProvider.DomainIdValue);
            if (powerCache == null || powerCache.PowerConduitCount == 0 || powerCache.NetGroupCount == 0)
            {
                return;
            }

            StrokeRenderer.Clear();
            DrawNetPaths(context, powerCache);
            StrokeRenderer.Flush(context.Map, GetNetMaterialForBucket);
        }

        private static void DrawNetPaths(UtilityOverlayContext context, PowerDomainCache powerCache)
        {
            System.Collections.Generic.List<IntVec3> conduitCells = powerCache.PowerConduitCells;
            for (int i = 0; i < conduitCells.Count; i++)
            {
                IntVec3 cell = conduitCells[i];
                if (!context.IsCellVisible(cell))
                {
                    continue;
                }

                int netId = powerCache.GetNetIdAt(cell);
                if (netId < 0)
                {
                    continue;
                }

                int bucket = GetPaletteIndex(powerCache.GetNetColorSeed(netId));
                byte neighborMask = powerCache.GetNeighborMaskAt(cell);
                int neighborCount = PowerDomainCache.CountNeighbors(neighborMask);
                Vector3 center = cell.ToVector3Shifted();

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

        private static int GetPaletteIndex(int colorSeed)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + colorSeed;
                hash ^= hash >> 16;
                if (hash < 0)
                {
                    hash = -(hash + 1);
                }

                return hash % NetPalette.Length;
            }
        }

        private static bool ShouldDrawHub(byte neighborMask)
        {
            int neighborCount = PowerDomainCache.CountNeighbors(neighborMask);
            // Keep rounded corners while still avoiding T-junction hub overdraw.
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

        private static Material GetNetMaterialForBucket(int bucket)
        {
            if (bucket < 0 || bucket >= NetPalette.Length)
            {
                return null;
            }

            return SolidColorMaterials.SimpleSolidColorMaterial(StrokeRenderer.ApplyAlpha(NetPalette[bucket]), false);
        }
    }
}
