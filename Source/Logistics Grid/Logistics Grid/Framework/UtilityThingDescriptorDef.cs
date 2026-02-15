using System.Collections.Generic;
using Verse;

namespace Logistics_Grid.Framework
{
    public sealed class UtilityThingDescriptorDef : Def
    {
        public string targetDefName;
        public string domainId;
        public bool powerOverlayRelevant = true;
        public bool powerConduit;
        public bool powerUser;
        public string powerConduitType;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (string.IsNullOrEmpty(targetDefName))
            {
                yield return "targetDefName is required";
            }
        }
    }
}
