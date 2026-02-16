using System.Collections.Generic;
using Logistics_Grid.Framework;
using RimWorld;
using Verse;

namespace Logistics_Grid.Domains.Power
{
    internal sealed class PowerDomainProvider : IUtilityDomainProvider
    {
        private struct NetStateAccumulator
        {
            public bool HasUnpoweredConsumer;
            public bool HasFlickedOffConsumer;
            public bool HasConsumer;
            public bool HasProducer;
            public bool HasEnergyGainRate;
            public float MinEnergyGainRate;
        }

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
            RebuildNetStates(powerCache, allThings);
            powerCache.FinalizeRebuild();
        }

        private static void RebuildNetStates(PowerDomainCache powerCache, List<Thing> allThings)
        {
            int netCount = powerCache.NetGroupCount;
            if (netCount <= 0)
            {
                return;
            }

            Dictionary<PowerNet, int> netIdByPowerNet = BuildNetIdLookupFromConduits(powerCache, allThings, netCount);
            NetStateAccumulator[] accumulators = new NetStateAccumulator[netCount];
            for (int i = 0; i < allThings.Count; i++)
            {
                ThingWithComps thingWithComps = allThings[i] as ThingWithComps;
                if (thingWithComps == null)
                {
                    continue;
                }

                CompPowerTrader powerTrader = thingWithComps.GetComp<CompPowerTrader>();
                if (powerTrader == null)
                {
                    continue;
                }

                int netId = ResolveNetIdForThing(powerCache, thingWithComps, powerTrader, netIdByPowerNet, netCount);
                if (netId < 0 || netId >= netCount)
                {
                    continue;
                }

                NetStateAccumulator accumulator = accumulators[netId];
                bool isPlantProducer = thingWithComps.GetComp<CompPowerPlant>() != null;
                bool hasConsumerLoad = powerTrader.PowerOutput < 0f
                    || (powerTrader.Props != null && (powerTrader.Props.PowerConsumption > 0f || powerTrader.Props.idlePowerDraw > 0f));
                bool shouldTreatAsConsumer = hasConsumerLoad && !isPlantProducer;
                if (shouldTreatAsConsumer)
                {
                    accumulator.HasConsumer = true;

                    CompFlickable flickable = thingWithComps.GetComp<CompFlickable>();
                    bool flickedOff = flickable != null && !flickable.SwitchIsOn;
                    if (flickedOff)
                    {
                        accumulator.HasFlickedOffConsumer = true;
                    }
                    else if (!powerTrader.PowerOn)
                    {
                        accumulator.HasUnpoweredConsumer = true;
                    }
                }

                if (isPlantProducer || powerTrader.PowerOutput > 0f)
                {
                    accumulator.HasProducer = true;
                }

                PowerNet powerNet = powerTrader.PowerNet;
                if (powerNet != null)
                {
                    if (powerNet.HasActivePowerSource)
                    {
                        accumulator.HasProducer = true;
                    }

                    float gainRate = powerNet.CurrentEnergyGainRate();
                    if (!accumulator.HasEnergyGainRate)
                    {
                        accumulator.HasEnergyGainRate = true;
                        accumulator.MinEnergyGainRate = gainRate;
                    }
                    else if (gainRate < accumulator.MinEnergyGainRate)
                    {
                        accumulator.MinEnergyGainRate = gainRate;
                    }
                }

                accumulators[netId] = accumulator;
            }

            for (int netId = 0; netId < netCount; netId++)
            {
                NetStateAccumulator accumulator = accumulators[netId];
                PowerNetOverlayState state = ResolveState(accumulator);
                powerCache.SetNetState(netId, state);
            }
        }

        private static Dictionary<PowerNet, int> BuildNetIdLookupFromConduits(PowerDomainCache powerCache, List<Thing> allThings, int netCount)
        {
            Dictionary<PowerNet, int> netIdByPowerNet = new Dictionary<PowerNet, int>();
            for (int i = 0; i < allThings.Count; i++)
            {
                Building building = allThings[i] as Building;
                if (building == null)
                {
                    continue;
                }

                PowerThingClassification classification = UtilityThingClassifier.ClassifyPowerThing(building);
                if (!classification.IsPowerConduit)
                {
                    continue;
                }

                int netId = powerCache.GetNetIdAt(building.Position);
                if (netId < 0 || netId >= netCount)
                {
                    continue;
                }

                CompPower compPower = building.GetComp<CompPower>();
                PowerNet powerNet = compPower != null ? compPower.PowerNet : null;
                if (powerNet == null || netIdByPowerNet.ContainsKey(powerNet))
                {
                    continue;
                }

                netIdByPowerNet.Add(powerNet, netId);
            }

            return netIdByPowerNet;
        }

        private static int ResolveNetIdForThing(
            PowerDomainCache powerCache,
            ThingWithComps thingWithComps,
            CompPowerTrader powerTrader,
            Dictionary<PowerNet, int> netIdByPowerNet,
            int netCount)
        {
            PowerNet powerNet = powerTrader.PowerNet;
            int netId;
            if (powerNet != null && netIdByPowerNet.TryGetValue(powerNet, out netId) && netId >= 0 && netId < netCount)
            {
                return netId;
            }

            Building building = thingWithComps as Building;
            if (building != null)
            {
                foreach (IntVec3 cell in building.OccupiedRect().Cells)
                {
                    netId = powerCache.GetNetIdAt(cell);
                    if (netId >= 0 && netId < netCount)
                    {
                        return netId;
                    }

                    netId = powerCache.GetNetIdAt(cell + IntVec3.North);
                    if (netId >= 0 && netId < netCount)
                    {
                        return netId;
                    }

                    netId = powerCache.GetNetIdAt(cell + IntVec3.South);
                    if (netId >= 0 && netId < netCount)
                    {
                        return netId;
                    }

                    netId = powerCache.GetNetIdAt(cell + IntVec3.East);
                    if (netId >= 0 && netId < netCount)
                    {
                        return netId;
                    }

                    netId = powerCache.GetNetIdAt(cell + IntVec3.West);
                    if (netId >= 0 && netId < netCount)
                    {
                        return netId;
                    }
                }
            }

            netId = powerCache.GetNetIdAt(thingWithComps.Position);
            if (netId >= 0 && netId < netCount)
            {
                return netId;
            }

            return -1;
        }

        private static PowerNetOverlayState ResolveState(NetStateAccumulator accumulator)
        {
            if (!accumulator.HasConsumer && !accumulator.HasProducer)
            {
                return PowerNetOverlayState.Unlinked;
            }

            if (accumulator.HasUnpoweredConsumer)
            {
                return PowerNetOverlayState.Unpowered;
            }

            if (accumulator.HasFlickedOffConsumer)
            {
                return PowerNetOverlayState.FlickedOff;
            }

            if (accumulator.HasProducer
                && accumulator.HasConsumer
                && accumulator.HasEnergyGainRate
                && accumulator.MinEnergyGainRate <= 0f)
            {
                return PowerNetOverlayState.Transient;
            }

            return PowerNetOverlayState.Powered;
        }
    }
}
