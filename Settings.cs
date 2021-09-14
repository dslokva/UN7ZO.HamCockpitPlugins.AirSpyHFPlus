using System;
using System.ComponentModel;
using VE3NEA.HamCockpit.PluginHelpers;

namespace UN7ZO.HamCockpitPlugins.AirSpyHFPlusSource {
/*    enum RigEnum : int {
        [Description("Rig #1")]
        RIG1 = 0,

        [Description("Rig #2")]
        RIG2 = 1
    }*/

    enum AttenuationLevelEnum : int {       
        [Description("0 dB (HF AGC mode)")]
        DB_AGC = -1,

        [Description("0 dB")]
        DB_0 = 0,

        [Description("6 dB")]
        DB_6 = 1,

        [Description("12 dB")]
        DB_12 = 2,

        [Description("18 dB")]
        DB_18 = 3,

        [Description("24 dB")]
        DB_24 = 4,

        [Description("30 dB")]
        DB_30 = 5,

        [Description("36 dB")]
        DB_36 = 6,

        [Description("42 dB")]
        DB_42 = 7,

        [Description("48 dB")]
        DB_48 = 8
    }

    class Settings {    
        private bool aGCThreshold;
        private AttenuationLevelEnum attenuation;
/*
        [DisplayName("OmniRig enabled")]
        [Description("Enables support for OmniRig v 1.xx")]
        [DefaultValue(false)]
        public bool OmniRigEnabled { get; set; }

        [DisplayName("OmniRig Rig Number")]
        [Description("Please select the Rig number")]
        [TypeConverter(typeof(EnumDescriptionConverter))]
        [DefaultValue(RigEnum.RIG1)]
        public RigEnum RigNumber { get; set; }
*/
        [DisplayName("Sampling Rate")]
        [Description("Receiver's output sampling rate")]
        [DefaultValue(192000)]
        [TypeConverter(typeof(SamplerateValueConverter))]

        public int SamplingRate { get; set; }

        [DisplayName("Device serial number")]
        [Description("S/N from attached device")]
        [DefaultValue("")]
        [ReadOnly(true)]
        public string DeviceSN { get; set; }

        [DisplayName("Device FW rev")]
        [Description("Firmware revision version from attached device")]
        [DefaultValue("")]
        [ReadOnly(true)]
        public string DeviceFW { get; set; }

        [DisplayName("Preamplifier enabled")]
        [Description("Enables device HF LNA preamplifier")]
        [DefaultValue(false)]
        public bool PreampEnabled { get; set; }

        [DisplayName("Device ATT")]
        [Description("Select HF attenuation value")]
        [TypeConverter(typeof(EnumDescriptionConverter))]
        [DefaultValue(AttenuationLevelEnum.DB_0)]
        public AttenuationLevelEnum Attenuation { get => attenuation; set => SetAttenuation(value); }

        private void SetAttenuation(AttenuationLevelEnum value) {
            attenuation = value;
            if (value == AttenuationLevelEnum.DB_AGC) {
                aGCEnabled = true;
            } else {
                aGCEnabled = false;
                AGCTreshold = false;
            }
        }

        [DisplayName("HF AGC threshold")]
        [Description("Set AGC high threshold level (HF AGC mode should be ON)")]
        [DefaultValue(false)]
        public bool AGCTreshold { get => aGCThreshold; set => SetAGCHighThreshold(value); }

        private void SetAGCHighThreshold(bool value) {
            if (Attenuation == AttenuationLevelEnum.DB_AGC)
                aGCThreshold = value;
            else
                aGCThreshold = false;
        }

        [Browsable(false)]
        [DefaultValue(false)]
        public bool aGCEnabled { get; set; }

        [Browsable(false)]
        public long[] Frequencies { get; set; } = new long[] { 14021000, 105000000 };

    }
}