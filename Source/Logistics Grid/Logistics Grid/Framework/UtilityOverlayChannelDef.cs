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
        }
    }
}
