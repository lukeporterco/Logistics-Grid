using Logistics_Grid.Components;
using Verse;

namespace Logistics_Grid.Framework
{
    internal sealed class UtilityOverlayContext
    {
        private readonly CellRect viewRect;
        private readonly bool hasViewRect;

        public UtilityOverlayContext(Map map, MapComponent_LogisticsGrid component)
        {
            Map = map;
            Component = component;

            CameraDriver cameraDriver = Find.CameraDriver;
            if (cameraDriver == null)
            {
                viewRect = default(CellRect);
                hasViewRect = false;
                return;
            }

            CellRect currentViewRect = cameraDriver.CurrentViewRect;
            if (currentViewRect.IsEmpty)
            {
                viewRect = default(CellRect);
                hasViewRect = false;
                return;
            }

            viewRect = currentViewRect.ExpandedBy(1);
            hasViewRect = true;
        }

        public Map Map { get; }
        public MapComponent_LogisticsGrid Component { get; }
        public CellRect ViewRect => viewRect;
        public bool HasViewRect => hasViewRect;

        public bool IsCellVisible(IntVec3 cell)
        {
            return !hasViewRect || viewRect.Contains(cell);
        }
    }
}
