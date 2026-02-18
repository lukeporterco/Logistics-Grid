using System.Collections.Generic;
using Verse;

namespace Logistics_Grid.Framework
{
    public sealed class UtilityOverlayChannelDef : Def
    {
        public string domainId;
        public string layerId;
        public bool defaultEnabled;
        public bool showInSettings = true;
        public int drawOrder;
        public float powerUserRingOuterRadius = 0.26f;
        public float powerUserRingInnerRadius = 0.20f;
        public float powerUserCoreRadius = 0.12f;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (string.IsNullOrEmpty(layerId))
            {
                yield return "layerId is required";
            }

            if (powerUserRingOuterRadius <= 0f)
            {
                yield return "powerUserRingOuterRadius must be > 0";
            }

            if (powerUserRingInnerRadius <= 0f)
            {
                yield return "powerUserRingInnerRadius must be > 0";
            }

            if (powerUserCoreRadius <= 0f)
            {
                yield return "powerUserCoreRadius must be > 0";
            }
        }
    }
}
