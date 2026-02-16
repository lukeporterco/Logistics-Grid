using System;
using System.Collections.Generic;
using Logistics_Grid.Domains.Power;
using RimWorld;
using Verse;

namespace Logistics_Grid.Framework
{
    internal struct PowerThingClassification
    {
        public bool IsOverlayRelevant;
        public bool IsPowerConduit;
        public bool IsPowerUser;
        public PowerConduitType ConduitType;
    }

    internal static class UtilityThingClassifier
    {
        private const string PowerDomainId = "power";

        private static bool descriptorIndexBuilt;
        private static readonly Dictionary<string, UtilityThingDescriptorDef> DescriptorByTargetDefName =
            new Dictionary<string, UtilityThingDescriptorDef>(StringComparer.OrdinalIgnoreCase);

        public static PowerThingClassification ClassifyPowerThing(Thing thing)
        {
            PowerThingClassification result = default(PowerThingClassification);

            ThingDef def = thing != null ? thing.def : null;
            if (def == null)
            {
                return result;
            }

            ApplyExtension(def.GetModExtension<DefModExtension_UtilityOverlay>(), ref result);

            UtilityThingDescriptorDef descriptor;
            if (TryGetDescriptorFor(def.defName, out descriptor))
            {
                ApplyDescriptor(descriptor, ref result);
            }

            ApplyVanillaFallback(thing, def, ref result);

            if (result.IsPowerConduit)
            {
                result.IsOverlayRelevant = true;
                if (result.ConduitType == PowerConduitType.None)
                {
                    result.ConduitType = PowerConduitType.Standard;
                }
            }

            return result;
        }

        private static void ApplyExtension(DefModExtension_UtilityOverlay extension, ref PowerThingClassification result)
        {
            if (extension == null || !IsPowerDomain(extension.domainId))
            {
                return;
            }

            if (extension.powerOverlayRelevant)
            {
                result.IsOverlayRelevant = true;
            }

            if (extension.powerConduit)
            {
                result.IsPowerConduit = true;
                result.IsOverlayRelevant = true;
            }

            if (extension.powerUser)
            {
                result.IsPowerUser = true;
                result.IsOverlayRelevant = true;
            }

            PowerConduitType conduitType;
            if (TryParseConduitType(extension.powerConduitType, out conduitType))
            {
                result.IsPowerConduit = true;
                result.ConduitType = conduitType;
                result.IsOverlayRelevant = true;
            }
        }

        private static void ApplyDescriptor(UtilityThingDescriptorDef descriptor, ref PowerThingClassification result)
        {
            if (descriptor == null || !IsPowerDomain(descriptor.domainId))
            {
                return;
            }

            if (descriptor.powerOverlayRelevant)
            {
                result.IsOverlayRelevant = true;
            }

            if (descriptor.powerConduit)
            {
                result.IsPowerConduit = true;
                result.IsOverlayRelevant = true;
            }

            if (descriptor.powerUser)
            {
                result.IsPowerUser = true;
                result.IsOverlayRelevant = true;
            }

            PowerConduitType conduitType;
            if (TryParseConduitType(descriptor.powerConduitType, out conduitType))
            {
                result.IsPowerConduit = true;
                result.ConduitType = conduitType;
                result.IsOverlayRelevant = true;
            }
        }

        private static void ApplyVanillaFallback(Thing thing, ThingDef def, ref PowerThingClassification result)
        {
            BuildingProperties buildingProperties = def.building;
            if (buildingProperties != null && buildingProperties.isPowerConduit)
            {
                result.IsPowerConduit = true;
                result.IsOverlayRelevant = true;
                if (result.ConduitType == PowerConduitType.None)
                {
                    result.ConduitType = PowerConduitType.Standard;
                }
            }

            if (ThingDefOf.HiddenConduit != null && def == ThingDefOf.HiddenConduit)
            {
                result.IsPowerConduit = true;
                result.IsOverlayRelevant = true;
                result.ConduitType = PowerConduitType.Hidden;
            }
            else if (ThingDefOf.PowerConduit != null && def == ThingDefOf.PowerConduit)
            {
                result.IsPowerConduit = true;
                result.IsOverlayRelevant = true;
                if (result.ConduitType == PowerConduitType.None)
                {
                    result.ConduitType = PowerConduitType.Standard;
                }
            }

            ThingWithComps thingWithComps = thing as ThingWithComps;
            if (thingWithComps == null)
            {
                return;
            }

            if (thingWithComps.GetComp<CompPowerTrader>() != null)
            {
                result.IsPowerUser = true;
                result.IsOverlayRelevant = true;
            }

            if (thingWithComps.GetComp<CompPowerBattery>() != null)
            {
                result.IsPowerUser = true;
                result.IsOverlayRelevant = true;
            }

            if (thingWithComps.GetComp<CompPower>() != null)
            {
                result.IsOverlayRelevant = true;
            }
        }

        private static bool TryParseConduitType(string conduitType, out PowerConduitType parsedConduitType)
        {
            parsedConduitType = PowerConduitType.None;
            if (string.IsNullOrEmpty(conduitType))
            {
                return false;
            }

            if (conduitType.Equals("Standard", StringComparison.OrdinalIgnoreCase))
            {
                parsedConduitType = PowerConduitType.Standard;
                return true;
            }

            if (conduitType.Equals("Hidden", StringComparison.OrdinalIgnoreCase))
            {
                parsedConduitType = PowerConduitType.Hidden;
                return true;
            }

            if (conduitType.Equals("Waterproof", StringComparison.OrdinalIgnoreCase))
            {
                parsedConduitType = PowerConduitType.Waterproof;
                return true;
            }

            return false;
        }

        private static bool IsPowerDomain(string domainId)
        {
            return string.IsNullOrEmpty(domainId)
                || domainId.Equals(PowerDomainId, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetDescriptorFor(string targetDefName, out UtilityThingDescriptorDef descriptor)
        {
            descriptor = null;
            if (string.IsNullOrEmpty(targetDefName))
            {
                return false;
            }

            EnsureDescriptorIndex();
            return DescriptorByTargetDefName.TryGetValue(targetDefName, out descriptor);
        }

        private static void EnsureDescriptorIndex()
        {
            if (descriptorIndexBuilt)
            {
                return;
            }

            DescriptorByTargetDefName.Clear();
            List<UtilityThingDescriptorDef> descriptors = DefDatabase<UtilityThingDescriptorDef>.AllDefsListForReading;
            for (int i = 0; i < descriptors.Count; i++)
            {
                UtilityThingDescriptorDef descriptor = descriptors[i];
                if (descriptor == null || string.IsNullOrEmpty(descriptor.targetDefName))
                {
                    continue;
                }

                DescriptorByTargetDefName[descriptor.targetDefName] = descriptor;
            }

            descriptorIndexBuilt = true;
        }
    }
}
