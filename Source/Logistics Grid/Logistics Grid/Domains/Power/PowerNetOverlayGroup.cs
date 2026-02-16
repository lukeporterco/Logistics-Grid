using Verse;

namespace Logistics_Grid.Domains.Power
{
    internal sealed class PowerNetOverlayGroup
    {
        public PowerNetOverlayGroup(int netId, IntVec3 representativeCell, int cellCount, int colorSeed, PowerNetOverlayState state = PowerNetOverlayState.Powered)
        {
            NetId = netId;
            RepresentativeCell = representativeCell;
            CellCount = cellCount;
            ColorSeed = colorSeed;
            State = state;
        }

        public int NetId { get; }
        public IntVec3 RepresentativeCell { get; }
        public int CellCount { get; }
        public int ColorSeed { get; }
        public PowerNetOverlayState State { get; internal set; }
    }
}
