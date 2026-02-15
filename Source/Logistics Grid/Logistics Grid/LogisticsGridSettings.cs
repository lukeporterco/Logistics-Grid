using UnityEngine;
using Verse;

namespace Logistics_Grid
{
    internal sealed class LogisticsGridSettings : ModSettings
    {
        public const float MinWorldDimAlpha = 0.20f;
        public const float MaxWorldDimAlpha = 0.80f;
        public const float DefaultWorldDimAlpha = 0.45f;
        public const bool DefaultShowPowerConduitsOverlay = false;
        public const bool DefaultShowPowerUsersOverlay = false;

        public float worldDimAlpha = DefaultWorldDimAlpha;
        public bool showPowerConduitsOverlay = DefaultShowPowerConduitsOverlay;
        public bool showPowerUsersOverlay = DefaultShowPowerUsersOverlay;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref worldDimAlpha, "worldDimAlpha", DefaultWorldDimAlpha);
            Scribe_Values.Look(ref showPowerConduitsOverlay, "showPowerConduitsOverlay", DefaultShowPowerConduitsOverlay);
            Scribe_Values.Look(ref showPowerUsersOverlay, "showPowerUsersOverlay", DefaultShowPowerUsersOverlay);
            ClampValues();
        }

        public void ClampValues()
        {
            worldDimAlpha = Mathf.Clamp(worldDimAlpha, MinWorldDimAlpha, MaxWorldDimAlpha);
        }
    }
}
