using System;
using System.Collections.Generic;
using Logistics_Grid.Domains.Power;
using Logistics_Grid.Utilities;
using Verse;

namespace Logistics_Grid.Framework
{
    internal static class UtilityOverlayRegistry
    {
        private static readonly Dictionary<string, IUtilityDomainProvider> DomainProvidersById =
            new Dictionary<string, IUtilityDomainProvider>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, IUtilitiesOverlayLayer> LayersById =
            new Dictionary<string, IUtilitiesOverlayLayer>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Func<IUtilitiesOverlayLayer>> LayerFactoriesById =
            new Dictionary<string, Func<IUtilitiesOverlayLayer>>(StringComparer.Ordinal);

        private static readonly HashSet<string> MissingLayerWarnings = new HashSet<string>(StringComparer.Ordinal);

        private static List<UtilityOverlayChannelDef> channelsInDrawOrder = new List<UtilityOverlayChannelDef>();
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            RegisterDomainProvider(new PowerDomainProvider());
            RegisterLayerFactory("LogisticsGrid.Layer.WorldDim", () => new UtilitiesWorldDimLayer());
            RegisterLayerFactory("LogisticsGrid.Layer.PowerConduits", () => new UtilitiesPowerConduitsLayer());
            RegisterLayerFactory("LogisticsGrid.Layer.PowerNets", () => new UtilitiesPowerNetsLayer());
            RegisterLayerFactory("LogisticsGrid.Layer.PowerUsers", () => new UtilitiesPowerUsersLayer());

            RefreshChannelCache();
            initialized = true;
        }

        public static IEnumerable<IUtilityDomainProvider> GetDomainProviders()
        {
            Initialize();
            return DomainProvidersById.Values;
        }

        public static IReadOnlyList<UtilityOverlayChannelDef> GetChannelsInDrawOrder()
        {
            Initialize();
            if (channelsInDrawOrder == null || channelsInDrawOrder.Count == 0)
            {
                RefreshChannelCache();
            }

            return channelsInDrawOrder;
        }

        public static bool TryGetLayerForChannel(UtilityOverlayChannelDef channelDef, out IUtilitiesOverlayLayer layer)
        {
            layer = null;
            if (channelDef == null || string.IsNullOrEmpty(channelDef.layerId))
            {
                return false;
            }

            Initialize();
            if (LayersById.TryGetValue(channelDef.layerId, out layer))
            {
                return true;
            }

            Func<IUtilitiesOverlayLayer> layerFactory;
            if (LayerFactoriesById.TryGetValue(channelDef.layerId, out layerFactory))
            {
                layer = layerFactory();
                if (layer != null)
                {
                    LayersById[channelDef.layerId] = layer;
                    return true;
                }
            }

            if (MissingLayerWarnings.Add(channelDef.layerId))
            {
                Log.Warning($"[Logistics Grid] Overlay channel '{channelDef.defName}' references missing layer '{channelDef.layerId}'.");
            }

            return false;
        }

        public static void RegisterDomainProvider(IUtilityDomainProvider provider)
        {
            if (provider == null || string.IsNullOrEmpty(provider.DomainId))
            {
                return;
            }

            DomainProvidersById[provider.DomainId] = provider;
        }

        public static void RegisterLayer(IUtilitiesOverlayLayer layer)
        {
            if (layer == null || string.IsNullOrEmpty(layer.LayerId))
            {
                return;
            }

            LayersById[layer.LayerId] = layer;
        }

        public static void RegisterLayerFactory(string layerId, Func<IUtilitiesOverlayLayer> layerFactory)
        {
            if (string.IsNullOrEmpty(layerId) || layerFactory == null)
            {
                return;
            }

            LayerFactoriesById[layerId] = layerFactory;
        }

        private static void RefreshChannelCache()
        {
            channelsInDrawOrder = new List<UtilityOverlayChannelDef>(DefDatabase<UtilityOverlayChannelDef>.AllDefsListForReading);
            channelsInDrawOrder.Sort(CompareChannels);
        }

        private static int CompareChannels(UtilityOverlayChannelDef left, UtilityOverlayChannelDef right)
        {
            if (left == right)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            int drawOrderCompare = left.drawOrder.CompareTo(right.drawOrder);
            if (drawOrderCompare != 0)
            {
                return drawOrderCompare;
            }

            return string.CompareOrdinal(left.defName, right.defName);
        }
    }
}
