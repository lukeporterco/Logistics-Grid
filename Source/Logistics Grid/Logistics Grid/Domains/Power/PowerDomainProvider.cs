using System.Collections.Generic;
using Logistics_Grid.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Domains.Power
{
    internal sealed class PowerDomainProvider : IUtilityDomainProvider
    {
        private struct NetStateAccumulator
        {
            public bool IsConnected;
            public bool HasActivePowerSource;
            public bool HasEnabledConsumerDemand;
            public bool HasMetConsumerDemand;
            public bool HasUnmetConsumerDemand;
            public bool HasEnergyGainRate;
            public float MinEnergyGainRate;
            public int EnabledConsumerCount;
            public int UnmetConsumerCount;
        }

        public const string DomainIdValue = "power";
        private const int RebuildIntervalTicksValue = 250;
        private const float DistressedUnmetDemandThreshold = 0.75f;

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
                    if (BuildNodeMarker(building, out PowerNodeMarker nodeMarker))
                    {
                        powerCache.AddPowerUser(building, nodeMarker);
                    }
                }
            }

            powerCache.RebuildNeighborMasks();
            powerCache.RebuildNetGroups();
            RebuildNetStates(powerCache, allThings);
            powerCache.FinalizeRebuild();
        }

        private static bool BuildNodeMarker(Building building, out PowerNodeMarker nodeMarker)
        {
            nodeMarker = default(PowerNodeMarker);

            if (building == null)
            {
                return false;
            }

            CellRect occupiedRect = building.OccupiedRect();
            if (occupiedRect.IsEmpty)
            {
                occupiedRect = CellRect.SingleCell(building.Position);
            }

            CompPowerBattery battery = building.GetComp<CompPowerBattery>();
            if (battery != null)
            {
                float maxStored = battery.Props != null ? battery.Props.storedEnergyMax : 0f;
                float ratio = maxStored > 0f ? Mathf.Clamp01(battery.StoredEnergy / maxStored) : 0f;
                float quantized = QuantizeFiveStep(ratio);
                nodeMarker = new PowerNodeMarker(
                    building,
                    occupiedRect,
                    PowerNodeIdentity.Storage,
                    PowerNodeCoreState.StorageCharge,
                    quantized);
                return true;
            }

            CompPowerTrader powerTrader = building.GetComp<CompPowerTrader>();
            if (powerTrader == null)
            {
                nodeMarker = new PowerNodeMarker(
                    building,
                    occupiedRect,
                    PowerNodeIdentity.Consumer,
                    PowerNodeCoreState.Neutral,
                    0f);
                return true;
            }

            bool isProducerCapable = building.GetComp<CompPowerPlant>() != null || powerTrader.PowerOutput > 0f;
            PowerNodeIdentity identity = isProducerCapable ? PowerNodeIdentity.ProducerCapable : PowerNodeIdentity.Consumer;
            PowerNodeCoreState coreState = PowerNodeCoreState.Neutral;
            float coreValue01 = 0f;

            CompFlickable flickable = building.GetComp<CompFlickable>();
            if (flickable != null && !flickable.SwitchIsOn)
            {
                coreState = PowerNodeCoreState.ToggledOff;
                nodeMarker = new PowerNodeMarker(building, occupiedRect, identity, coreState, coreValue01);
                return true;
            }

            CompProperties_Power props = powerTrader.Props;
            bool hasConsumerLoad = props != null && (props.PowerConsumption > 0f || props.idlePowerDraw > 0f || powerTrader.PowerOutput < 0f);
            if (hasConsumerLoad && !powerTrader.PowerOn)
            {
                coreState = PowerNodeCoreState.Fault;
                nodeMarker = new PowerNodeMarker(building, occupiedRect, identity, coreState, coreValue01);
                return true;
            }

            bool flowDirectionRelevant = hasConsumerLoad && isProducerCapable;
            if (flowDirectionRelevant)
            {
                const float NearZeroFlowEpsilon = 1f;
                float flow = powerTrader.PowerOutput;
                if (flow > NearZeroFlowEpsilon)
                {
                    coreState = PowerNodeCoreState.FlowExport;
                    coreValue01 = 1f;
                }
                else if (flow < -NearZeroFlowEpsilon)
                {
                    coreState = PowerNodeCoreState.FlowImport;
                    coreValue01 = 1f;
                }
            }

            nodeMarker = new PowerNodeMarker(building, occupiedRect, identity, coreState, coreValue01);
            return true;
        }

        private static float QuantizeFiveStep(float value01)
        {
            float clamped = Mathf.Clamp01(value01);
            return Mathf.Round(clamped * 4f) / 4f;
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
            SeedAccumulatorsFromConduitNets(netIdByPowerNet, accumulators, netCount);
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
                CompFlickable flickable = thingWithComps.GetComp<CompFlickable>();
                bool isFlickedOff = flickable != null && !flickable.SwitchIsOn;
                bool hasConsumerLoad = powerTrader.PowerOutput < 0f
                    || (powerTrader.Props != null && (powerTrader.Props.PowerConsumption > 0f || powerTrader.Props.idlePowerDraw > 0f));
                if (hasConsumerLoad && !isFlickedOff)
                {
                    accumulator.HasEnabledConsumerDemand = true;
                    accumulator.EnabledConsumerCount++;
                    if (powerTrader.PowerOn)
                    {
                        accumulator.HasMetConsumerDemand = true;
                    }
                    else
                    {
                        accumulator.HasUnmetConsumerDemand = true;
                        accumulator.UnmetConsumerCount++;
                    }
                }

                PowerNet powerNet = powerTrader.PowerNet;
                if (powerNet != null)
                {
                    accumulator.IsConnected = true;
                    if (powerNet.HasActivePowerSource)
                    {
                        accumulator.HasActivePowerSource = true;
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

        private static void SeedAccumulatorsFromConduitNets(
            Dictionary<PowerNet, int> netIdByPowerNet,
            NetStateAccumulator[] accumulators,
            int netCount)
        {
            foreach (KeyValuePair<PowerNet, int> pair in netIdByPowerNet)
            {
                PowerNet powerNet = pair.Key;
                int netId = pair.Value;
                if (powerNet == null || netId < 0 || netId >= netCount)
                {
                    continue;
                }

                NetStateAccumulator accumulator = accumulators[netId];
                accumulator.IsConnected = true;
                if (powerNet.HasActivePowerSource)
                {
                    accumulator.HasActivePowerSource = true;
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

                accumulators[netId] = accumulator;
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
            if (!accumulator.IsConnected)
            {
                return PowerNetOverlayState.Unlinked;
            }

            if (accumulator.EnabledConsumerCount > 0)
            {
                float unmetRatio = accumulator.UnmetConsumerCount / (float)accumulator.EnabledConsumerCount;
                if (unmetRatio >= 1f)
                {
                    return PowerNetOverlayState.Unpowered;
                }

                if (unmetRatio >= DistressedUnmetDemandThreshold)
                {
                    return PowerNetOverlayState.Distressed;
                }
            }

            bool hasNegativeBalance = accumulator.HasEnergyGainRate && accumulator.MinEnergyGainRate < 0f;
            bool sustainedByStorage = accumulator.HasMetConsumerDemand && !accumulator.HasActivePowerSource;
            if ((accumulator.HasMetConsumerDemand && hasNegativeBalance) || sustainedByStorage)
            {
                // Net is currently covering demand but not sustainable long-term (negative balance or no active supply).
                return PowerNetOverlayState.Transient;
            }

            if (accumulator.HasMetConsumerDemand || accumulator.HasActivePowerSource)
            {
                return PowerNetOverlayState.Powered;
            }

            if (accumulator.HasEnabledConsumerDemand || accumulator.HasUnmetConsumerDemand)
            {
                return PowerNetOverlayState.Unpowered;
            }

            if (accumulator.HasActivePowerSource
                && accumulator.HasEnergyGainRate
                && accumulator.MinEnergyGainRate <= 0f)
            {
                return PowerNetOverlayState.Transient;
            }

            // Connected nets with no active source/demand telemetry are treated as healthy idle, not unlinked.
            return PowerNetOverlayState.Powered;
        }
    }
}
