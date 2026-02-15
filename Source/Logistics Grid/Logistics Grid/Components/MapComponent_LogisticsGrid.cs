using Verse;

namespace Logistics_Grid.Components
{
    internal sealed class MapComponent_LogisticsGrid : MapComponent
    {
        private const int TickThrottleInterval = 30;
        private const int ProofLogIntervalTicks = 300;

        public bool Dirty = true;
        public int CachedThingCount;

        private int lastProcessedTick = -1;
        private int lastProofLogTick = -1;

        public MapComponent_LogisticsGrid(Map map) : base(map)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Dirty = true;
        }

        public override void MapComponentTick()
        {
            int ticksGame = Find.TickManager.TicksGame;
            if (lastProcessedTick >= 0 && ticksGame - lastProcessedTick < TickThrottleInterval)
            {
                return;
            }

            lastProcessedTick = ticksGame;

            if (Dirty)
            {
                RebuildCaches();
            }

            if (Prefs.DevMode && ticksGame - lastProofLogTick >= ProofLogIntervalTicks)
            {
                Log.Message($"[Logistics Grid] Cache proof: map={map.Index} cachedThingCount={CachedThingCount} dirty={Dirty}");
                lastProofLogTick = ticksGame;
            }
        }

        public void MarkDirty()
        {
            Dirty = true;
        }

        public void RebuildCaches()
        {
            CachedThingCount = map.listerThings.AllThings.Count;
            Dirty = false;
        }
    }
}
