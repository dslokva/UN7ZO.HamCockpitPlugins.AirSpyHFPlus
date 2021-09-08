using System;
using System.Runtime.InteropServices;

namespace UN7ZO.HamCockpitPlugins.AirSpyHFPlusSource
{
    #region AirspyHF Structures

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct airspyhf_transfer
    {
        public IntPtr device;
        public IntPtr ctx;
        public Complex* samples;
        public int sample_count;
        public ulong dropped_samples;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct airspyhf_read_partid_serialno
    {
        public uint part_id;
        public fixed uint serial_no[4];
    }

    #endregion

    #region AirspyHF Callback Delegate

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int airspyhf_sample_cb(airspyhf_transfer* ptr);

    #endregion

    #region Enumerations

    public enum airspyhf_error
    {
        SUCCESS = 0,
        ERROR = -1
    }

    public enum airspyhf_user_output_state : byte
    {
        AIRSPYHF_USER_OUTPUT_LOW = 0,
        AIRSPYHF_USER_OUTPUT_HIGH = 1
    }

    public enum airspyhf_user_output : byte
    {
        AIRSPYHF_USER_OUTPUT_0 = 0,
        AIRSPYHF_USER_OUTPUT_1 = 1,
        AIRSPYHF_USER_OUTPUT_2 = 2,
        AIRSPYHF_USER_OUTPUT_3 = 3
    }

    #endregion

    public unsafe static class NativeMethods
    {
        private const string LibAirspyHF = "airspyhf";

        private const int MAX_VERSION_STRING_SIZE = 64;

        #region Native Methods

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_list_devices", CallingConvention = CallingConvention.Cdecl)]
        public static extern int airspyhf_list_devices(UInt64* serials, int count);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_open(out IntPtr dev);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_open_sn", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_open_sn(out IntPtr dev, UInt64 serial);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_close(IntPtr dev);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_start", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_start(IntPtr dev, airspyhf_sample_cb cb, IntPtr ctx);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_stop", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_stop(IntPtr dev);
        
        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_is_streaming", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool airspyhf_is_streaming(IntPtr dev);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_set_freq", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_set_freq(IntPtr dev, uint freq_hz);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_get_samplerates", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_get_samplerates(IntPtr device, uint* buffer, uint len);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_set_samplerate", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_set_samplerate(IntPtr device, uint samplerate);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_is_low_if", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool airspyhf_is_low_if(IntPtr dev);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_get_overall_gain", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_get_overall_gain(IntPtr device, out byte gain);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_get_calibration", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_get_calibration(IntPtr device, out int ppb);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_set_calibration", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_set_calibration(IntPtr device, int ppb);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_set_optimal_iq_correction_point", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_set_optimal_iq_correction_point(IntPtr device, float w);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_iq_balancer_configure", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_iq_balancer_configure(IntPtr device, int buffers_to_skip, int fft_integration, int fft_overlap, int correlation_integration);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_flash_calibration", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_flash_calibration(IntPtr dev);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_board_partid_serialno_read", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_board_partid_serialno_read(IntPtr dev, out airspyhf_read_partid_serialno read_partid_serialno);

        public static ulong airspyhf_get_serialno(IntPtr dev)
        {
            airspyhf_read_partid_serialno sn;
            if (dev != IntPtr.Zero) {
                var rc = airspyhf_board_partid_serialno_read(dev, out sn);
                if (rc == airspyhf_error.SUCCESS) {
                    return ((ulong)sn.serial_no[0] << 32) | sn.serial_no[1];
                }
            }
            return 0;
        }

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_version_string_read", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_version_string_read(IntPtr dev, sbyte* version, byte length);

        public static string airspyhf_version_string_read(IntPtr dev)
        {
            var buffer = new sbyte[MAX_VERSION_STRING_SIZE];
            fixed (sbyte* ptr = buffer)
            {
                var rc = airspyhf_version_string_read(dev, ptr, MAX_VERSION_STRING_SIZE);
                if (rc == airspyhf_error.SUCCESS)
                {
                    buffer[MAX_VERSION_STRING_SIZE - 1] = 0;
                    return new string(ptr);
                }
            }

            return string.Empty;
        }


        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_set_user_output", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_set_user_output(IntPtr dev, airspyhf_user_output pin, airspyhf_user_output_state value);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_set_hf_agc", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_set_hf_agc(IntPtr dev, byte flag);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_set_hf_agc_threshold", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_set_hf_agc_threshold(IntPtr dev, byte flag);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_set_hf_att", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_set_hf_att(IntPtr dev, byte value);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_set_hf_lna", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_set_hf_lna(IntPtr dev, byte flag);

        [DllImport(LibAirspyHF, EntryPoint = "airspyhf_set_lib_dsp", CallingConvention = CallingConvention.Cdecl)]
        public static extern airspyhf_error airspyhf_set_lib_dsp(IntPtr dev, byte flag);

        #endregion
    }
}
