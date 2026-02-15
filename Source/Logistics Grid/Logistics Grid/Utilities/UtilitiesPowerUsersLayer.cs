using System.Collections.Generic;
using Logistics_Grid.Domains.Power;
using Logistics_Grid.Framework;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal sealed class UtilitiesPowerUsersLayer : IUtilitiesOverlayLayer
    {
        private static readonly Color PowerUsersHighlightColor = new Color(1f, 0.82f, 0.35f, 0.78f);
        private static readonly Vector3 MarkerScale = new Vector3(0.32f, 1f, 0.32f);
        private static readonly float MarkerAltitude = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0025f;
        private static readonly List<Matrix4x4> MarkerMatrices = new List<Matrix4x4>(512);

        public string LayerId => "LogisticsGrid.Layer.PowerUsers";

        public void Draw(UtilityOverlayContext context, UtilityOverlayChannelDef channelDef)
        {
            _ = channelDef;
            Material powerUsersMaterial = GetPowerUsersMaterial();

            PowerDomainCache powerCache = context.Component.GetDomainCache<PowerDomainCache>(PowerDomainProvider.DomainIdValue);
            if (powerCache == null || powerCache.PowerUserCount == 0)
            {
                return;
            }

            List<IntVec3> powerUserCells = powerCache.PowerUserCells;
            if (powerUserCells.Count == 0)
            {
                return;
            }

            MarkerMatrices.Clear();
            for (int i = 0; i < powerUserCells.Count; i++)
            {
                if (!context.IsCellVisible(powerUserCells[i]))
                {
                    continue;
                }

                Vector3 center = powerUserCells[i].ToVector3Shifted();
                center.y = MarkerAltitude;

                Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, MarkerScale);
                MarkerMatrices.Add(matrix);
            }

            for (int i = 0; i < MarkerMatrices.Count; i++)
            {
                Graphics.DrawMesh(MeshPool.plane10, MarkerMatrices[i], powerUsersMaterial, 0);
                UtilityOverlayProfiler.RecordDrawSubmission(context.Map, 1);
            }
        }

        private static Material GetPowerUsersMaterial()
        {
            return SolidColorMaterials.SimpleSolidColorMaterial(PowerUsersHighlightColor, false);
        }
    }
}
