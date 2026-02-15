using UnityEngine;
using Verse;
using Logistics_Grid.Utilities;

namespace Logistics_Grid
{
    public sealed class LogisticsGridMod : Mod
    {
        private readonly LogisticsGridSettings settings;

        public LogisticsGridMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<LogisticsGridSettings>();
            settings.ClampValues();
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

            bool showPowerConduits = settings.showPowerConduitsOverlay;
            listing.CheckboxLabeled("Show power conduits", ref showPowerConduits, "Draw vanilla conduit visuals and connected path segments above the utilities dim.");
            if (showPowerConduits != settings.showPowerConduitsOverlay)
            {
                settings.showPowerConduitsOverlay = showPowerConduits;
                UtilitiesOverlaySettingsCache.Refresh();
            }

            bool showPowerUsers = settings.showPowerUsersOverlay;
            listing.CheckboxLabeled("Show power users", ref showPowerUsers, "Draw occupied cells for CompPowerTrader buildings above the utilities dim.");
            if (showPowerUsers != settings.showPowerUsersOverlay)
            {
                settings.showPowerUsersOverlay = showPowerUsers;
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
