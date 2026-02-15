using System.Collections.Generic;
using Logistics_Grid.Framework;
using UnityEngine;
using Verse;

namespace Logistics_Grid
{
    internal sealed class LogisticsGridSettings : ModSettings
    {
        private const string PowerConduitsChannelDefName = "LogisticsGrid_Channel_PowerConduits";
        private const string PowerUsersChannelDefName = "LogisticsGrid_Channel_PowerUsers";

        public const float MinWorldDimAlpha = 0.20f;
        public const float MaxWorldDimAlpha = 0.80f;
        public const float DefaultWorldDimAlpha = 0.45f;
        public const bool DefaultShowPowerConduitsOverlay = false; // legacy migration value
        public const bool DefaultShowPowerUsersOverlay = false; // legacy migration value

        public float worldDimAlpha = DefaultWorldDimAlpha;
        public Dictionary<string, bool> channelEnabledByDefName =
            new Dictionary<string, bool>(System.StringComparer.Ordinal);

        private bool legacyShowPowerConduitsOverlay = DefaultShowPowerConduitsOverlay;
        private bool legacyShowPowerUsersOverlay = DefaultShowPowerUsersOverlay;
        private List<string> channelKeysWorkingList;
        private List<bool> channelValuesWorkingList;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref worldDimAlpha, "worldDimAlpha", DefaultWorldDimAlpha);
            Scribe_Collections.Look(
                ref channelEnabledByDefName,
                "channelEnabledByDefName",
                LookMode.Value,
                LookMode.Value,
                ref channelKeysWorkingList,
                ref channelValuesWorkingList);

            // Legacy fields are still loaded so existing settings migrate forward.
            Scribe_Values.Look(ref legacyShowPowerConduitsOverlay, "showPowerConduitsOverlay", DefaultShowPowerConduitsOverlay);
            Scribe_Values.Look(ref legacyShowPowerUsersOverlay, "showPowerUsersOverlay", DefaultShowPowerUsersOverlay);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (channelEnabledByDefName == null)
                {
                    channelEnabledByDefName = new Dictionary<string, bool>(System.StringComparer.Ordinal);
                }

                MigrateLegacyChannelValues();
                EnsureChannelDefaults();
            }

            ClampValues();
        }

        public void ClampValues()
        {
            worldDimAlpha = Mathf.Clamp(worldDimAlpha, MinWorldDimAlpha, MaxWorldDimAlpha);
        }

        public void EnsureChannelDefaults()
        {
            UtilityOverlayRegistry.Initialize();
            IReadOnlyList<UtilityOverlayChannelDef> channels = UtilityOverlayRegistry.GetChannelsInDrawOrder();
            for (int i = 0; i < channels.Count; i++)
            {
                UtilityOverlayChannelDef channelDef = channels[i];
                if (channelDef == null || string.IsNullOrEmpty(channelDef.defName))
                {
                    continue;
                }

                if (!channelEnabledByDefName.ContainsKey(channelDef.defName))
                {
                    channelEnabledByDefName[channelDef.defName] = channelDef.defaultEnabled;
                }
            }
        }

        public bool GetChannelEnabled(string channelDefName, bool defaultEnabled)
        {
            if (string.IsNullOrEmpty(channelDefName))
            {
                return defaultEnabled;
            }

            bool enabled;
            if (channelEnabledByDefName.TryGetValue(channelDefName, out enabled))
            {
                return enabled;
            }

            return defaultEnabled;
        }

        public void SetChannelEnabled(string channelDefName, bool enabled)
        {
            if (string.IsNullOrEmpty(channelDefName))
            {
                return;
            }

            channelEnabledByDefName[channelDefName] = enabled;
        }

        private void MigrateLegacyChannelValues()
        {
            if (!channelEnabledByDefName.ContainsKey(PowerConduitsChannelDefName))
            {
                channelEnabledByDefName[PowerConduitsChannelDefName] = legacyShowPowerConduitsOverlay;
            }

            if (!channelEnabledByDefName.ContainsKey(PowerUsersChannelDefName))
            {
                channelEnabledByDefName[PowerUsersChannelDefName] = legacyShowPowerUsersOverlay;
            }
        }
    }
}
