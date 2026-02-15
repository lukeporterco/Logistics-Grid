using Logistics_Grid.Components;
using Logistics_Grid.Framework;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal static class UtilitiesOverlayManager
    {
        private static int lastDrawFrame = -1;
        private static int lastDrawMapIndex = -1;

        public static void DrawWorld(Map map, MapComponent_LogisticsGrid component)
        {
            if (map == null || component == null || !UtilitiesViewController.ShouldDrawForMap(map) || map != Find.CurrentMap)
            {
                return;
            }

            int frame = Time.frameCount;
            if (frame == lastDrawFrame && map.Index == lastDrawMapIndex)
            {
                return;
            }

            lastDrawFrame = frame;
            lastDrawMapIndex = map.Index;

            // Keep overlays responsive while paused by rebuilding dirty domain caches on draw.
            component.EnsureCachesCurrentForDraw();

            UtilityOverlayProfiler.BeginFrame(map);
            UtilityOverlayContext context = new UtilityOverlayContext(map, component);
            System.Collections.Generic.IReadOnlyList<UtilityOverlayChannelDef> channels = UtilityOverlayRegistry.GetChannelsInDrawOrder();
            for (int i = 0; i < channels.Count; i++)
            {
                UtilityOverlayChannelDef channelDef = channels[i];
                if (!UtilitiesOverlaySettingsCache.IsChannelEnabled(channelDef))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(channelDef.domainId) && !component.HasDomainCache(channelDef.domainId))
                {
                    continue;
                }

                IUtilitiesOverlayLayer layer;
                if (!UtilityOverlayRegistry.TryGetLayerForChannel(channelDef, out layer))
                {
                    continue;
                }

                layer.Draw(context, channelDef);
            }
        }
    }
}
