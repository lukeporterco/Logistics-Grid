using Logistics_Grid.Components;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal interface IUtilitiesOverlayLayer
    {
        int DrawOrder { get; }

        void Draw(Map map, MapComponent_LogisticsGrid component);
    }
}
