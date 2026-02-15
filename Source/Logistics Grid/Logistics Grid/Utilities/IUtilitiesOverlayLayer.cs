using Logistics_Grid.Framework;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal interface IUtilitiesOverlayLayer
    {
        string LayerId { get; }

        void Draw(UtilityOverlayContext context, UtilityOverlayChannelDef channelDef);
    }
}
