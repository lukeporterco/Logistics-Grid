using Verse;

namespace Logistics_Grid.Framework
{
    public sealed class DefModExtension_UtilityOverlay : DefModExtension
    {
        public string domainId;
        public bool powerOverlayRelevant = true;
        public bool powerConduit;
        public bool powerUser;
        public string powerConduitType;
    }
}
