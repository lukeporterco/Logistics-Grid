using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Logistics_Grid.Framework;
using Verse;

namespace Logistics_Grid.Components
{
    internal sealed class MapComponent_LogisticsGrid : MapComponent
    {
        private const int ProofLogIntervalTicks = 300;

        private sealed class DomainRuntimeState
        {
            public IUtilityDomainProvider Provider;
            public IUtilityDomainCache Cache;
            public int LastRebuildTick = -1;
            public float LastRebuildMilliseconds;
        }

        private readonly Dictionary<string, DomainRuntimeState> domainStatesById =
            new Dictionary<string, DomainRuntimeState>(System.StringComparer.OrdinalIgnoreCase);

        private readonly List<DomainRuntimeState> domainStates = new List<DomainRuntimeState>();

        private bool initialized;
        private int lastProofLogTick = -1;

        public MapComponent_LogisticsGrid(Map map) : base(map)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            EnsureInitialized();
            MarkAllDirty();
        }

        public override void MapComponentTick()
        {
            EnsureInitialized();

            int ticksGame = Find.TickManager.TicksGame;
            for (int i = 0; i < domainStates.Count; i++)
            {
                DomainRuntimeState state = domainStates[i];
                bool shouldRebuild = state.Cache.Dirty
                    || state.LastRebuildTick < 0
                    || ticksGame - state.LastRebuildTick >= state.Provider.RebuildIntervalTicks;
                if (!shouldRebuild)
                {
                    continue;
                }

                Stopwatch stopwatch = Stopwatch.StartNew();
                state.Provider.Rebuild(map, state.Cache);
                stopwatch.Stop();

                state.LastRebuildMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds;
                state.LastRebuildTick = ticksGame;
                state.Cache.Dirty = false;
            }

            if (Prefs.DevMode && ticksGame - lastProofLogTick >= ProofLogIntervalTicks)
            {
                LogProfileSnapshot(ticksGame);
                lastProofLogTick = ticksGame;
            }
        }

        public void MarkDirtyForThing(Thing thing)
        {
            EnsureInitialized();
            if (thing == null)
            {
                return;
            }

            for (int i = 0; i < domainStates.Count; i++)
            {
                DomainRuntimeState state = domainStates[i];
                if (state.Provider.IsThingRelevantForInvalidation(thing))
                {
                    state.Cache.Dirty = true;
                }
            }
        }

        public void MarkDomainDirty(string domainId)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(domainId))
            {
                return;
            }

            DomainRuntimeState state;
            if (domainStatesById.TryGetValue(domainId, out state))
            {
                state.Cache.Dirty = true;
            }
        }

        public bool HasDomainCache(string domainId)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(domainId))
            {
                return false;
            }

            return domainStatesById.ContainsKey(domainId);
        }

        public TDomainCache GetDomainCache<TDomainCache>(string domainId)
            where TDomainCache : class, IUtilityDomainCache
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(domainId))
            {
                return null;
            }

            DomainRuntimeState state;
            if (!domainStatesById.TryGetValue(domainId, out state))
            {
                return null;
            }

            return state.Cache as TDomainCache;
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            UtilityOverlayRegistry.Initialize();
            foreach (IUtilityDomainProvider provider in UtilityOverlayRegistry.GetDomainProviders())
            {
                if (provider == null || string.IsNullOrEmpty(provider.DomainId))
                {
                    continue;
                }

                DomainRuntimeState state = new DomainRuntimeState
                {
                    Provider = provider,
                    Cache = provider.CreateCache(map)
                };

                domainStates.Add(state);
                domainStatesById[provider.DomainId] = state;
            }

            initialized = true;
        }

        private void MarkAllDirty()
        {
            for (int i = 0; i < domainStates.Count; i++)
            {
                domainStates[i].Cache.Dirty = true;
            }
        }

        private void LogProfileSnapshot(int ticksGame)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[Logistics Grid] Overlay profile: ");
            builder.Append("map=");
            builder.Append(map.Index);
            builder.Append(" ticks=");
            builder.Append(ticksGame);
            builder.Append(" drawSubmissions=");
            builder.Append(UtilityOverlayProfiler.GetCurrentFrameDrawSubmissions(map));
            builder.Append(" drawMeshes=");
            builder.Append(UtilityOverlayProfiler.GetCurrentFrameDrawMeshes(map));

            for (int i = 0; i < domainStates.Count; i++)
            {
                DomainRuntimeState state = domainStates[i];
                builder.Append(" | ");
                builder.Append(state.Provider.DomainId);
                builder.Append(":");
                builder.Append(" dirty=");
                builder.Append(state.Cache.Dirty ? "1" : "0");
                builder.Append(" rebuildMs=");
                builder.Append(state.LastRebuildMilliseconds.ToString("0.00"));
                builder.Append(" ");
                builder.Append(state.Cache.PrimaryLabel);
                builder.Append("=");
                builder.Append(state.Cache.PrimaryCount);
                builder.Append(" ");
                builder.Append(state.Cache.SecondaryLabel);
                builder.Append("=");
                builder.Append(state.Cache.SecondaryCount);
            }

            Log.Message(builder.ToString());
        }
    }
}
