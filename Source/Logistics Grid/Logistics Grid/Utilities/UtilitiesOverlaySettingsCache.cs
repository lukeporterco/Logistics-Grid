using System.Collections.Generic;
using Logistics_Grid.Framework;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal static class UtilitiesOverlaySettingsCache
    {
        private static LogisticsGridSettings settings;
        private static float worldDimAlpha = LogisticsGridSettings.DefaultWorldDimAlpha;
        private static readonly Dictionary<string, bool> channelEnabledByDefName =
            new Dictionary<string, bool>(System.StringComparer.Ordinal);

        internal static bool DebugDisableWorldDim = false;

        public static float WorldDimAlpha => worldDimAlpha;

        public static void Initialize(LogisticsGridSettings sourceSettings)
        {
            settings = sourceSettings;
            Refresh();
        }

        public static void Refresh()
        {
            UtilityOverlayRegistry.Initialize();
            channelEnabledByDefName.Clear();

            if (settings == null)
            {
                worldDimAlpha = LogisticsGridSettings.DefaultWorldDimAlpha;
            }
            else
            {
                settings.ClampValues();
                settings.EnsureChannelDefaults();
                worldDimAlpha = settings.worldDimAlpha;
            }

            IReadOnlyList<UtilityOverlayChannelDef> channels = UtilityOverlayRegistry.GetChannelsInDrawOrder();
            for (int i = 0; i < channels.Count; i++)
            {
                UtilityOverlayChannelDef channelDef = channels[i];
                if (channelDef == null || string.IsNullOrEmpty(channelDef.defName))
                {
                    continue;
                }

                bool isEnabled = settings == null
                    ? channelDef.defaultEnabled
                    : settings.GetChannelEnabled(channelDef.defName, channelDef.defaultEnabled);

                channelEnabledByDefName[channelDef.defName] = isEnabled;
            }
        }

        public static bool IsChannelEnabled(UtilityOverlayChannelDef channelDef)
        {
            if (channelDef == null)
            {
                return false;
            }

            bool isEnabled;
            if (channelEnabledByDefName.TryGetValue(channelDef.defName, out isEnabled))
            {
                return isEnabled;
            }

            return channelDef.defaultEnabled;
        }
    }
}
