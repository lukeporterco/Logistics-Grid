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

        public static Color TintColor => tintColor;
        public static float TintStrength => tintStrength;
        public static bool FadeWorldAndUi => fadeWorldAndUi;

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
            }
            else
            {
                settings.ClampValues();
                tintStrength = settings.tintStrength;
                useFogTint = settings.useFogTint;
                fadeWorldAndUi = settings.fadeWorldAndUi;
            }

            Color baseColor = useFogTint ? FogBaseColor : DimBaseColor;
            tintColor = new Color(baseColor.r, baseColor.g, baseColor.b, tintStrength);
        }
    }
}
