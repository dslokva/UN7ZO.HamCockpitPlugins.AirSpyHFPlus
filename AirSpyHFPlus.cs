using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Forms;
using VE3NEA.HamCockpit.DspFun;
using VE3NEA.HamCockpit.PluginAPI;

namespace UN7ZO.HamCockpitPlugins.AirSpyHFPlusSource {
    [Export(typeof(IPlugin))]
    [Export(typeof(ISignalSource))]
    class AirSpyHFPlus : IPlugin, ISignalSource {
        private Settings settings = new Settings();
        private AirspyHFDevice device = null;

        public int SAMPLING_RATE;
        private readonly RingBuffer buffer;
        //private OmniRigClient omnirig;
        private int lastRXfreq;
        private bool allowedToChangeFreq = true;
     
        public AirSpyHFPlus() {
            Debug.WriteLine("AirSpyHFPlus - constructor");
            SAMPLING_RATE = 192000;
            
            buffer = new RingBuffer(SAMPLING_RATE);
            Format = new SignalFormat(SAMPLING_RATE, true, false, 1, -48000, 48000, 0);
            buffer.SamplesAvailable += (o, e) => SamplesAvailable?.Invoke(this, e);
            //this.Tuned += internallyTuned;

            device = AirspyHFDevice.GetInstance();
            device.SamplesAvailable += newSamples;           
        }

        private void RefreshDeviceSN() {
            int err = device.Initialize();
            if (err == 0) {
                settings.DeviceSN = Convert.ToString(device.SerialNumber);
                settings.DeviceFW = device.FirmwareRevision;
                device.Dispose();
            } else {
                settings.DeviceSN = "";
                settings.DeviceFW = "Error opening device";
            }
        }

/*        private void omniRigTuned(object sender, EventArgs e) {
            if (omnirig != null && lastRXfreq / 10 != omnirig.RxFrequency/10 && allowedToChangeFreq) {
                SetDialFrequency(omnirig.RxFrequency, 0);
                lastRXfreq = omnirig.RxFrequency;
                settings.Frequencies[0] = omnirig.RxFrequency;
                Debug.WriteLine("omniRigTuned called, freq: " + omnirig.RxFrequency);
            }
            allowedToChangeFreq = true;
        }
*/
/*        private void internallyTuned(object sender, EventArgs e) {
            if (omnirig != null && omnirig.RxFrequency != (int)GetDialFrequency(0)) {
                allowedToChangeFreq = false; 
                omnirig.RxFrequency = (int)GetDialFrequency(0);                
                Debug.WriteLine("internallyTuned called, freq: " + (int)GetDialFrequency(0));
            }
        }*/

        #region IPlugin
        public string Name => "AirSpyHF+ SDR";
        public string Author => "UN7ZO";
        public bool Enabled { get; set; }
        public object Settings { get => getSettings(); set => setSettings(value as Settings); }

        public ToolStrip ToolStrip => null;
        public ToolStripItem StatusItem => null;
        #endregion

        #region ISignalSource 
        public void Initialize() {
            Debug.WriteLine("ISignalSource - Initialize");
            SAMPLING_RATE = Convert.ToInt32(settings.SamplingRate);
            //Samplerate for SDR receiver and audio sample rate should be the same
            if (SAMPLING_RATE == 0)
                throw(new ApplicationException("Please select Samplerate parameter in AirSpyHF+ plugin options."));           

            buffer.Resize(SAMPLING_RATE);
            Format = new SignalFormat(SAMPLING_RATE, true, false, 1, -48000, 48000, 0);            

/*            if (settings.OmniRigEnabled) {
                omnirig = new OmniRigClient();
                if (omnirig != null) {
                    omnirig.RigNo = (int)(settings.RigNumber + 1);
                    omnirig.Tuned += omniRigTuned;
                    Debug.WriteLine("OmniRig initialized succesfully.");
                }
            } */ 
        }

        public bool Active {
            get => GetActive();
            set => SetActive(value);
        }

        private bool GetActive() {
            bool result = false;

            if (device != null)
                result = device.IsStreaming;

            return result;
        }

        private void SetActive(bool value) {
            if (value == Active) return;

            if (value) {
                Debug.WriteLine("SetActive called - Start section now!");
                int err = device.Initialize();
                if (err != 0) {                                      
                    throw new ApplicationException("Cannot open AIRSPY HF+ Dual / Discovery device");                    
                }

/*                if (omnirig != null)
                    omnirig.Active = true;*/

                RefreshDeviceSettings();
                device.Start(SAMPLING_RATE);
                SetDialFrequency(GetDialFrequency(0), 0);

            } else {
                Debug.WriteLine("SetActive called - Stop section now!");
/*                if (omnirig != null)
                    omnirig.Active = false;*/

                device.Dispose();
                var exception = new Exception("Gracefully stopped SDR receiver.");
                Stopped?.Invoke(this, new StoppedEventArgs(exception));
            }
        }
        private void RefreshDeviceSettings() {
            device.PreampEnabled = settings.PreampEnabled;

            AttenuationLevelEnum attLevel = settings.Attenuation;

            if (attLevel == AttenuationLevelEnum.DB_AGC) {
                device.Attenuation = 0;
                device.AgcEnabled = true;
                device.AgcThreshold = settings.AGCTreshold;
            } else {
                device.AgcEnabled = false;
                device.AgcThreshold = false;
                device.Attenuation = (int)attLevel;
            }
        }

        public event EventHandler<StoppedEventArgs> Stopped;
        #endregion

        #region ITuner
        public long GetDialFrequency(int channel = 0) {
            //Debug.WriteLine("GetDialFrequency called, channel: "+channel + ", freq: "+ settings.Frequencies[channel]);
            return settings.Frequencies[channel];
        }

        public void SetDialFrequency(long frequency, int channel) {
            settings.Frequencies[channel] = frequency;
            if (Active)
                try {
                    //send to radio
                    device.SetFrequency(frequency);
                    //notify host application                   
                    Tuned?.Invoke(this, new EventArgs());
                }
                catch (Exception e) {
                    device.Dispose();
                    var exception = new Exception($"Command SetDialFrequency failed:\n\n{e.Message}");
                    Stopped?.Invoke(this, new StoppedEventArgs(exception));
                }
        }
        public event EventHandler Tuned;
        #endregion

        #region ISampleStream
        public SignalFormat Format { get; private set; }
        public int Read(float[] buffer, int offset, int count) { return this.buffer.Read(buffer, offset, count); }

        public event EventHandler<SamplesAvailableEventArgs> SamplesAvailable;
        #endregion

        private object getSettings() {
            return settings;
        }

        private void setSettings(Settings newSettings) {
            settings = newSettings;
            Debug.WriteLine("setSettings called");

            RefreshDeviceSN();
        }

        private unsafe void newSamples(object sender, ComplexSamplesEventArgs e) {
            // Here we don't need additional processing. 
            // HamCockpit RingBuffer accept 16 bit complex floats dirrectly

            var receivedBytes = new float[e.Length * 2];
            for (int i = 0; i < e.Length; i++) { 
                // fixed 2048 I/Q samples
                receivedBytes[2 * i] = e.Buffer[i].Real;
                receivedBytes[2 * i + 1] = e.Buffer[i].Imag;
            }

            buffer.Write(receivedBytes, 0, receivedBytes.Length);
        }

    }
}
