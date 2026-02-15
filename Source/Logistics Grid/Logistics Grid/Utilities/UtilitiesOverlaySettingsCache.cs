using UnityEngine;

namespace Logistics_Grid.Utilities
{
    internal static class UtilitiesOverlaySettingsCache
    {
        private static readonly Color DimBaseColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color FogBaseColor = new Color(0.12f, 0.14f, 0.16f, 1f);

        private static LogisticsGridSettings settings;
        private static Color tintColor = new Color(0f, 0f, 0f, LogisticsGridSettings.DefaultTintStrength);
        private static float tintStrength = LogisticsGridSettings.DefaultTintStrength;
        private static bool useFogTint;
        private static bool fadeWorldAndUi;
        private static bool showPowerConduitsOverlay;
        private static bool showPowerUsersOverlay;

        public static Color TintColor => tintColor;
        public static float TintStrength => tintStrength;
        public static bool FadeWorldAndUi => fadeWorldAndUi;
        public static bool ShowPowerConduitsOverlay => showPowerConduitsOverlay;
        public static bool ShowPowerUsersOverlay => showPowerUsersOverlay;

        public static void Initialize(LogisticsGridSettings sourceSettings)
        {
            settings = sourceSettings;
            Refresh();
        }

        public static void Refresh()
        {
            if (settings == null)
            {
                tintStrength = LogisticsGridSettings.DefaultTintStrength;
                useFogTint = LogisticsGridSettings.DefaultFogTint;
                fadeWorldAndUi = LogisticsGridSettings.DefaultFadeWorldAndUi;
                showPowerConduitsOverlay = LogisticsGridSettings.DefaultShowPowerConduitsOverlay;
                showPowerUsersOverlay = LogisticsGridSettings.DefaultShowPowerUsersOverlay;
            }
            else
            {
                settings.ClampValues();
                tintStrength = settings.tintStrength;
                useFogTint = settings.useFogTint;
                fadeWorldAndUi = settings.fadeWorldAndUi;
                showPowerConduitsOverlay = settings.showPowerConduitsOverlay;
                showPowerUsersOverlay = settings.showPowerUsersOverlay;
            }

            Color baseColor = useFogTint ? FogBaseColor : DimBaseColor;
            tintColor = new Color(baseColor.r, baseColor.g, baseColor.b, tintStrength);
        }
    }
}
