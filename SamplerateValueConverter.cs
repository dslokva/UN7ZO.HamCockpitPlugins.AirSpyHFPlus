using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UN7ZO.HamCockpitPlugins.AirSpyHFPlusSource {
    public struct SamplerateValueEntry {
        public int Id;
        public string Name;
        public SamplerateValueEntry(int samplerate, string name) { Id = samplerate; Name = name; }
    }

    /// <exclude />
    public class SamplerateValueConverter : StringConverter {
        internal SamplerateValueEntry[] valuesTable;
        private AirspyHFDevice device = AirspyHFDevice.GetInstance();

        /// <exclude />
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true;
        }

        /// <exclude />
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            if (valuesTable == null)
                ListSampleRates();
            return new StandardValuesCollection(valuesTable.Select(s => s.Id).ToArray());
        }

        /// <exclude />
        protected void ListSampleRates() {

            if (device != null) {
                //We want to re-read samplerates only if receiver not running!
                if (!device.IsStreaming)
                    device.Initialize();

                uint[] nativeSampleRates = device.NativeSampleRates;

                valuesTable = new SamplerateValueEntry[nativeSampleRates.Length];

                for (uint i = 0; i < nativeSampleRates.Length; i++) {
                    valuesTable[i] = new SamplerateValueEntry((int)nativeSampleRates[i], Convert.ToString(nativeSampleRates[i] / 1000) + " KSps");
                }
                //and close it if it not running
                if (!device.IsStreaming)
                    device.Dispose();               
            }
        }

        /// <exclude />
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (valuesTable == null) ListSampleRates();
            return valuesTable.Where(s => s.Name == value as string)?.Select(s => s.Id)?.First();
        }

        /// <exclude />
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (valuesTable == null) ListSampleRates();
       
            try {
                return valuesTable.Where(s => s.Id == (int)value).Select(s => s.Name).First();
            }
            catch {
                return "<please select>";
            }
        }
    }


}
