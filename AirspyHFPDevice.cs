using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UN7ZO.HamCockpitPlugins.AirSpyHFPlusSource
{
    public delegate void SamplesAvailableDelegate<ArgsType>(object sender, ArgsType e);

    public unsafe sealed class ComplexSamplesEventArgs : EventArgs
    {
        public int Length { get; set; }
        public ulong DroppedSamples { get; set; }
        public Complex* Buffer { get; set; }
    }

    public struct Complex
    {
        public float Real;
        public float Imag;
    }

    //Singletone class because have to read available sample rates before start
    public unsafe class AirspyHFDevice : IDisposable {
        public const uint DefaultFrequency = 14074000;
        public const uint DefaultSampleRate = 192000;

        private IntPtr _dev;
        private uint[] _nativeSampleRates;
        private uint _nativeSampleRate = DefaultSampleRate;
        private uint _centerFrequency;
        private bool _agcEnabled = true;
        private bool _preampEnabled = false;
        private bool _threshold = true;
        private int _attenuation = 0;
        private int _gpioA = 0;
        private int _gpioB = 1;
        private int _gpioC = 2;
        private int _gpioD = 3;

        private uint _currentSampleRate; 
        private bool _isStreaming;
        private GCHandle _gcHandle;
        
        private static readonly airspyhf_sample_cb _airspyhfCallback = AirSpyHFSamplesAvailable;

        public event SamplesAvailableDelegate<ComplexSamplesEventArgs> SamplesAvailable;

        private static readonly AirspyHFDevice instance = new AirspyHFDevice();

        public static AirspyHFDevice GetInstance() {
            return instance;
        }

        private AirspyHFDevice() {
            
        }


        public int Initialize() {
            UInt64 serial = 0;
            _nativeSampleRates = new uint[] { DefaultSampleRate };

            airspyhf_error r = NativeMethods.airspyhf_open_sn(out _dev, serial);
            if (r != airspyhf_error.SUCCESS) {                
                return -1;
                // throw new ApplicationException("Cannot open AIRSPY HF+ Dual / Discovery device");
            }

            uint count;
            r = NativeMethods.airspyhf_get_samplerates(_dev, &count, 0);
            if (r == airspyhf_error.SUCCESS && count > 0) {
                _nativeSampleRates = new uint[count];
                fixed (uint* ptr = _nativeSampleRates) {
                    NativeMethods.airspyhf_get_samplerates(_dev, ptr, count);
                }
            }

            NativeMethods.airspyhf_set_user_output(_dev, (airspyhf_user_output)0, 0);
            NativeMethods.airspyhf_set_user_output(_dev, (airspyhf_user_output)1, 0);
            NativeMethods.airspyhf_set_user_output(_dev, (airspyhf_user_output)2, 0);
            NativeMethods.airspyhf_set_user_output(_dev, (airspyhf_user_output)3, 0);

            NativeMethods.airspyhf_set_lib_dsp(_dev, 1);
            NativeMethods.airspyhf_set_calibration(_dev, 0);

            _gcHandle = GCHandle.Alloc(this);
            return 0;
        }

        internal void SetFrequency(long frequency) {
            _centerFrequency = (uint)frequency;
            var errCode = NativeMethods.airspyhf_set_freq(_dev, _centerFrequency);
            if (errCode != airspyhf_error.SUCCESS) {
                throw new ApplicationException("Cannot change LO freq to:" + frequency);
            } else
            Debug.WriteLine("device LO freq changed: "+ frequency);

        }

        ~AirspyHFDevice() {
            Dispose();
        }

        public static UInt64[] ListAvailableDevices() {
            var count = NativeMethods.airspyhf_list_devices(null, 0);
            if (count <= 0) {
                return null;
            }
            var serials = new UInt64[count];

            fixed (UInt64* ptr = serials) {
                if (NativeMethods.airspyhf_list_devices(ptr, count) <= 0) {
                    return null;
                }
            }

            return serials;
        }

        public uint[] NativeSampleRates {
            get { 
                return _nativeSampleRates; 
            }
        }

        public uint NativeSampleRate {
            get {
                return _nativeSampleRate;
            }
            set {
                if (Array.IndexOf(_nativeSampleRates, value) >= 0) {
                    _nativeSampleRate = value;
                    NativeMethods.airspyhf_set_samplerate(_dev, value);
                }
            }
        }

        public void Dispose() {
            if (_dev != IntPtr.Zero) {
                Stop();
                NativeMethods.airspyhf_close(_dev);
                if (_gcHandle.IsAllocated) {
                    _gcHandle.Free();
                }
                _dev = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }
        }

        public void Start(int samplingRate)
        {            
            if (_isStreaming)
            {
                return;
            }

            /*
             * Native samplerates:
             *   0 - 912000
             *   1 - 768000
             *   2 - 456000
             *   3 - 384000
             *   4 - 256000
             *   5 - 192000
             */

            airspyhf_error r;
            if (_currentSampleRate != (uint)samplingRate) //maybe not needed?
            {
                r = NativeMethods.airspyhf_set_samplerate(_dev, (uint)samplingRate);
                if (r != airspyhf_error.SUCCESS)
                {
                    throw new ApplicationException("airspyhf_set_samplerate() error");
                }
                else
                {
                    _currentSampleRate = (uint)samplingRate;
                }
            }

            r = NativeMethods.airspyhf_start(_dev, _airspyhfCallback, (IntPtr) _gcHandle);
            if (r != airspyhf_error.SUCCESS)
            {
                throw new ApplicationException("airspyhf_start_rx() error");
            }

            _isStreaming = true;
        }

        public void Stop()
        {
            if (!_isStreaming)
            {
                return;
            }

            NativeMethods.airspyhf_stop(_dev);
            _isStreaming = false;
        }

        public uint Frequency
        {
            get { return _centerFrequency; }
            set
            {
                _centerFrequency = value;
                NativeMethods.airspyhf_set_freq(_dev, _centerFrequency);
            }
        }

        public ulong SerialNumber
        {
            get
            {
                return NativeMethods.airspyhf_get_serialno(_dev);
            }
        }

        public string FirmwareRevision
        {
            get
            {
                return NativeMethods.airspyhf_version_string_read(_dev);
            }
        }

        public bool AgcEnabled
        {
            get
            {
                return _agcEnabled;
            }
            set
            {
                if (value != _agcEnabled)
                {
                    _agcEnabled = value;
                    NativeMethods.airspyhf_set_hf_agc(_dev, (byte)(_agcEnabled ? 1 : 0));
                }
            }
        }

        public bool AgcThreshold
        {
            get
            {
                return _threshold;
            }
            set
            {
                if (value != _threshold)
                {
                    _threshold = value;
                    NativeMethods.airspyhf_set_hf_agc_threshold(_dev, (byte)(_threshold ? 1 : 0));
                }
            }
        }

        public bool PreampEnabled
        {
            get
            {
                return _preampEnabled;
            }
            set
            {
                if (value != _preampEnabled)
                {
                    _preampEnabled = value;
                    NativeMethods.airspyhf_set_hf_lna(_dev, (byte)(_preampEnabled ? 1 : 0));
                }
            }
        }

        public int Attenuation
        {
            get
            {
                return _attenuation;
            }
            set
            {
                if (value != _attenuation)
                {
                    _attenuation = value;
                    NativeMethods.airspyhf_set_hf_att(_dev, (byte)_attenuation);
                }
            }
        }

        public bool IsStreaming
        {
            get { return _isStreaming; }
        }

        #region Streaming methods

        protected virtual void OnComplexSamplesAvailable(Complex* buffer, int length, ulong droppedSamples)
        {
            var handler = SamplesAvailable;
            if (handler != null)
            {
                var e = new ComplexSamplesEventArgs();
                e.Buffer = buffer;
                e.Length = length;
                e.DroppedSamples = droppedSamples;
                handler(this, e);
            }
        }

        private static int AirSpyHFSamplesAvailable(airspyhf_transfer* data)
        {
            int len = data->sample_count;
            var buf = data->samples;
            ulong dropped_samples = data->dropped_samples;
            IntPtr ctx = data->ctx;

            var gcHandle = GCHandle.FromIntPtr(ctx);
            if (!gcHandle.IsAllocated)
            {
                return -1;
            }

            var instance = (AirspyHFDevice) gcHandle.Target;

            instance.OnComplexSamplesAvailable(buf, len, dropped_samples);
            
            return 0;
        }

        #endregion
    }
}
