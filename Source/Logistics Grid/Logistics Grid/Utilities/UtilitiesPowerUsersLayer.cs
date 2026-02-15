using System.Collections.Generic;
using Logistics_Grid.Components;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    [StaticConstructorOnStartup]
    internal sealed class UtilitiesPowerUsersLayer : IUtilitiesOverlayLayer
    {
        private static readonly Color PowerUsersHighlightColor = new Color(1f, 0.82f, 0.35f, 0.78f);
        private static readonly Material PowerUsersMaterial;
        private static readonly Vector3 MarkerScale = new Vector3(0.32f, 1f, 0.32f);
        private static readonly float MarkerAltitude = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.0025f;

        static UtilitiesPowerUsersLayer()
        {
            PowerUsersMaterial = SolidColorMaterials.SimpleSolidColorMaterial(PowerUsersHighlightColor, false);
        }

        public int DrawOrder => 110;

        public void Draw(Map map, MapComponent_LogisticsGrid component)
        {
            _ = map;
            if (!UtilitiesOverlaySettingsCache.ShowPowerUsersOverlay || component.PowerUserCount == 0)
            {
                return;
            }

            List<IntVec3> powerUserCells = component.PowerUserCells;
            if (powerUserCells.Count == 0)
            {
                return;
            }

            for (int i = 0; i < powerUserCells.Count; i++)
            {
                Vector3 center = powerUserCells[i].ToVector3Shifted();
                center.y = MarkerAltitude;

                Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, MarkerScale);
                Graphics.DrawMesh(MeshPool.plane10, matrix, PowerUsersMaterial, 0);
            }
        }
    }
}
