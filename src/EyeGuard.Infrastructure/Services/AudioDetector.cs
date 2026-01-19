namespace EyeGuard.Infrastructure.Services;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EyeGuard.Infrastructure.Native;

/// <summary>
/// 音频检测服务。
/// 检测系统是否正在播放音频（看视频/听音乐）。
/// </summary>
public class AudioDetector : IDisposable
{
    private AudioNativeMethods.IMMDeviceEnumerator? _deviceEnumerator;
    private AudioNativeMethods.IAudioMeterInformation? _audioMeter;
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// 音频检测阈值。峰值高于此值认为有音频播放。
    /// </summary>
    public float AudioThreshold { get; set; } = 0.01f;

    /// <summary>
    /// 是否正在播放音频。
    /// </summary>
    public bool IsAudioPlaying
    {
        get
        {
            try
            {
                if (!_initialized) Initialize();
                return GetCurrentPeakValue() > AudioThreshold;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 获取当前音频峰值 (0.0 - 1.0)。
    /// </summary>
    public float CurrentPeakValue
    {
        get
        {
            try
            {
                if (!_initialized) Initialize();
                return GetCurrentPeakValue();
            }
            catch
            {
                return 0f;
            }
        }
    }

    private void Initialize()
    {
        if (_initialized) return;

        try
        {
            var clsid = AudioNativeMethods.CLSID_MMDeviceEnumerator;
            var iid = AudioNativeMethods.IID_IMMDeviceEnumerator;
            
            int hr = AudioNativeMethods.CoCreateInstance(
                ref clsid,
                IntPtr.Zero,
                AudioNativeMethods.CLSCTX_ALL,
                ref iid,
                out _deviceEnumerator);

            if (hr != 0 || _deviceEnumerator == null)
            {
                Debug.WriteLine($"[AudioDetector] Failed to create device enumerator: {hr}");
                return;
            }

            hr = _deviceEnumerator.GetDefaultAudioEndpoint(
                AudioNativeMethods.EDataFlow.eRender,
                AudioNativeMethods.ERole.eMultimedia,
                out var device);

            if (hr != 0 || device == null)
            {
                Debug.WriteLine($"[AudioDetector] Failed to get default audio endpoint: {hr}");
                return;
            }

            var meterIid = AudioNativeMethods.IID_IAudioMeterInformation;
            hr = device.Activate(ref meterIid, AudioNativeMethods.CLSCTX_ALL, IntPtr.Zero, out var meterPtr);

            if (hr != 0 || meterPtr == IntPtr.Zero)
            {
                Debug.WriteLine($"[AudioDetector] Failed to activate audio meter: {hr}");
                return;
            }

            _audioMeter = (AudioNativeMethods.IAudioMeterInformation)Marshal.GetObjectForIUnknown(meterPtr);
            _initialized = true;
            
            Debug.WriteLine("[AudioDetector] Initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioDetector] Initialize failed: {ex.Message}");
        }
    }

    private float GetCurrentPeakValue()
    {
        if (_audioMeter == null) return 0f;

        try
        {
            _audioMeter.GetPeakValue(out float peak);
            return peak;
        }
        catch
        {
            return 0f;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_audioMeter != null)
        {
            Marshal.ReleaseComObject(_audioMeter);
            _audioMeter = null;
        }

        if (_deviceEnumerator != null)
        {
            Marshal.ReleaseComObject(_deviceEnumerator);
            _deviceEnumerator = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~AudioDetector()
    {
        Dispose();
    }
}
