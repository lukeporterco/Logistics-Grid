using Verse;

namespace Logistics_Grid.Utilities
{
    internal static class UtilitiesOverlaySettingsCache
    {
        private static LogisticsGridSettings settings;
        private static float worldDimAlpha = LogisticsGridSettings.DefaultWorldDimAlpha;
        private static bool showPowerConduitsOverlay;
        private static bool showPowerUsersOverlay;
        internal static bool DebugDisableWorldDim = false;

        public static float WorldDimAlpha => worldDimAlpha;
        public static bool ShowPowerConduitsOverlay => showPowerConduitsOverlay;
        public static bool ShowPowerUsersOverlay => showPowerUsersOverlay;
        public static bool ShouldDrawWorldDim => !Prefs.DevMode || !DebugDisableWorldDim;

        public static void Initialize(LogisticsGridSettings sourceSettings)
        {
            settings = sourceSettings;
            Refresh();
        }

        public static void Refresh()
        {
            if (settings == null)
            {
                worldDimAlpha = LogisticsGridSettings.DefaultWorldDimAlpha;
                showPowerConduitsOverlay = LogisticsGridSettings.DefaultShowPowerConduitsOverlay;
                showPowerUsersOverlay = LogisticsGridSettings.DefaultShowPowerUsersOverlay;
            }
            else
            {
                settings.ClampValues();
                worldDimAlpha = settings.worldDimAlpha;
                showPowerConduitsOverlay = settings.showPowerConduitsOverlay;
                showPowerUsersOverlay = settings.showPowerUsersOverlay;
            }
        }
    }
}
