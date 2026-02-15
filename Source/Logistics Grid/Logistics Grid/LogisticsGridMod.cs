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

            float visiblePercent = (1f - settings.tintStrength) * 100f;
            listing.Label($"Tint strength ({visiblePercent:F0}% world visibility)");
            float newTintStrength = listing.Slider(settings.tintStrength, LogisticsGridSettings.MinTintStrength, LogisticsGridSettings.MaxTintStrength);
            if (!Mathf.Approximately(newTintStrength, settings.tintStrength))
            {
                settings.tintStrength = newTintStrength;
                UtilitiesOverlaySettingsCache.Refresh();
            }

            listing.GapLine();

            bool useFogTint = settings.useFogTint;
            listing.CheckboxLabeled("Fog tint (off = dim tint)", ref useFogTint, "Switch between neutral dimming and a fog-like tint.");
            if (useFogTint != settings.useFogTint)
            {
                settings.useFogTint = useFogTint;
                UtilitiesOverlaySettingsCache.Refresh();
            }

            bool fadeWorldAndUi = settings.fadeWorldAndUi;
            listing.CheckboxLabeled("Fade world + UI (off = world only)", ref fadeWorldAndUi, "When enabled, overlay tint is drawn after main tabs to include UI.");
            if (fadeWorldAndUi != settings.fadeWorldAndUi)
            {
                settings.fadeWorldAndUi = fadeWorldAndUi;
                UtilitiesOverlaySettingsCache.Refresh();
            }

            bool showPowerConduitsOverlay = settings.showPowerConduitsOverlay;
            listing.CheckboxLabeled("Show power conduits overlay", ref showPowerConduitsOverlay, "Draws a minimal conduit-cell highlight from cached map data.");
            if (showPowerConduitsOverlay != settings.showPowerConduitsOverlay)
            {
                settings.showPowerConduitsOverlay = showPowerConduitsOverlay;
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
