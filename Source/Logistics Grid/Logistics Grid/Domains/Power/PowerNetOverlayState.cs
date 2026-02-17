namespace Logistics_Grid.Domains.Power
{
    internal enum PowerNetOverlayState : byte
    {
        Powered = 0,
        Transient = 1,
        FlickedOff = 2,
        Unpowered = 3,
        Unlinked = 4,
        Distressed = 5
    }
}
