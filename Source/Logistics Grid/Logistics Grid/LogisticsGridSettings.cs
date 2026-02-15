using UnityEngine;
using Verse;

namespace Logistics_Grid
{
    internal sealed class LogisticsGridSettings : ModSettings
    {
        public const float MinTintStrength = 0.50f;
        public const float MaxTintStrength = 1.00f;
        public const float DefaultTintStrength = 0.90f;
        public const bool DefaultFogTint = false;
        public const bool DefaultFadeWorldAndUi = false;
        public const bool DefaultShowPowerConduitsOverlay = false;
        public const bool DefaultShowPowerUsersOverlay = false;

        public float tintStrength = DefaultTintStrength;
        public bool useFogTint = DefaultFogTint;
        public bool fadeWorldAndUi = DefaultFadeWorldAndUi;
        public bool showPowerConduitsOverlay = DefaultShowPowerConduitsOverlay;
        public bool showPowerUsersOverlay = DefaultShowPowerUsersOverlay;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref tintStrength, "tintStrength", DefaultTintStrength);
            Scribe_Values.Look(ref useFogTint, "useFogTint", DefaultFogTint);
            Scribe_Values.Look(ref fadeWorldAndUi, "fadeWorldAndUi", DefaultFadeWorldAndUi);
            Scribe_Values.Look(ref showPowerConduitsOverlay, "showPowerConduitsOverlay", DefaultShowPowerConduitsOverlay);
            Scribe_Values.Look(ref showPowerUsersOverlay, "showPowerUsersOverlay", DefaultShowPowerUsersOverlay);
            ClampValues();
        }

        public void ClampValues()
        {
            tintStrength = Mathf.Clamp(tintStrength, MinTintStrength, MaxTintStrength);
        }
    }
}
