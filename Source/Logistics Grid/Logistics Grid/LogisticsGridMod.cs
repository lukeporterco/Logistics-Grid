using UnityEngine;
using Verse;
using Logistics_Grid.Framework;
using Logistics_Grid.Utilities;

namespace Logistics_Grid
{
    public sealed class LogisticsGridMod : Mod
    {
        private readonly LogisticsGridSettings settings;

        public LogisticsGridMod(ModContentPack content) : base(content)
        {
            UtilityOverlayRegistry.Initialize();
            settings = GetSettings<LogisticsGridSettings>();
            settings.ClampValues();
            settings.EnsureChannelDefaults();
            UtilitiesOverlaySettingsCache.Initialize(settings);
        }

        public override string SettingsCategory()
        {
            return "Logistics Grid";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("Utilities Overlay");
            listing.Gap(4f);

            listing.Label($"World dim strength: {settings.worldDimAlpha:0.00}");
            float newWorldDimAlpha = listing.Slider(settings.worldDimAlpha, LogisticsGridSettings.MinWorldDimAlpha, LogisticsGridSettings.MaxWorldDimAlpha);
            if (!Mathf.Approximately(newWorldDimAlpha, settings.worldDimAlpha))
            {
                settings.worldDimAlpha = newWorldDimAlpha;
                settings.ClampValues();
                UtilitiesOverlaySettingsCache.Refresh();
            }

            listing.GapLine();

            bool settingsChanged = false;
            System.Collections.Generic.IReadOnlyList<UtilityOverlayChannelDef> channels = UtilityOverlayRegistry.GetChannelsInDrawOrder();
            for (int i = 0; i < channels.Count; i++)
            {
                UtilityOverlayChannelDef channelDef = channels[i];
                if (channelDef == null || !channelDef.showInSettings)
                {
                    continue;
                }

                bool isEnabled = settings.GetChannelEnabled(channelDef.defName, channelDef.defaultEnabled);
                bool newIsEnabled = isEnabled;
                string channelLabel = !string.IsNullOrEmpty(channelDef.label)
                    ? channelDef.LabelCap.ToString()
                    : channelDef.defName;
                listing.CheckboxLabeled(channelLabel, ref newIsEnabled, channelDef.description);
                if (newIsEnabled != isEnabled)
                {
                    settings.SetChannelEnabled(channelDef.defName, newIsEnabled);
                    settingsChanged = true;
                }
            }

            if (settingsChanged)
            {
                UtilitiesOverlaySettingsCache.Refresh();
            }

            listing.End();
        }

        public override void WriteSettings()
        {
            settings.ClampValues();
            base.WriteSettings();
            UtilitiesOverlaySettingsCache.Refresh();
        }
    }
}
