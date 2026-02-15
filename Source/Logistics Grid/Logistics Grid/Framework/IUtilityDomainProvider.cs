using Verse;

namespace Logistics_Grid.Framework
{
    internal interface IUtilityDomainProvider
    {
        string DomainId { get; }
        int RebuildIntervalTicks { get; }

        IUtilityDomainCache CreateCache(Map map);
        void Rebuild(Map map, IUtilityDomainCache cache);
        bool IsThingRelevantForInvalidation(Thing thing);
    }
}
