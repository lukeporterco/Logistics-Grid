using System.Collections.Generic;
using Logistics_Grid.Framework;
using Verse;

namespace Logistics_Grid.Domains.Power
{
    internal sealed class PowerDomainProvider : IUtilityDomainProvider
    {
        public const string DomainIdValue = "power";
        private const int RebuildIntervalTicksValue = 250;

        public string DomainId => DomainIdValue;
        public int RebuildIntervalTicks => RebuildIntervalTicksValue;

        public IUtilityDomainCache CreateCache(Map map)
        {
            return new PowerDomainCache(map);
        }

        public bool IsThingRelevantForInvalidation(Thing thing)
        {
            return UtilityThingClassifier.ClassifyPowerThing(thing).IsOverlayRelevant;
        }

        public void Rebuild(Map map, IUtilityDomainCache cache)
        {
            PowerDomainCache powerCache = cache as PowerDomainCache;
            if (powerCache == null || map == null)
            {
                return;
            }

            powerCache.PrepareForRebuild();

            List<Thing> allThings = map.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                Building building = allThings[i] as Building;
                if (building == null)
                {
                    continue;
                }

                PowerThingClassification classification = UtilityThingClassifier.ClassifyPowerThing(building);
                if (classification.IsPowerConduit)
                {
                    powerCache.AddConduit(building, classification.ConduitType);
                }

                if (classification.IsPowerUser)
                {
                    powerCache.AddPowerUser(building);
                }
            }

            powerCache.RebuildNeighborMasks();
            powerCache.RebuildNetGroups();
            powerCache.FinalizeRebuild();
        }
    }
}
