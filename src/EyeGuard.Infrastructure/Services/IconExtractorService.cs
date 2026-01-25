using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EyeGuard.Infrastructure.Services;

/// <summary>
/// 图标提取服务
/// 负责从可执行文件提取图标并缓存（按需缓存策略）
/// 使用安全的 Win32 API 获取进程路径，避免 Win32Exception
/// </summary>
public class IconExtractorService : IDisposable
{
    // 用于缓存提取过的图标 Base64 字符串 (Key: ExePath)
    private readonly ConcurrentDictionary<string, string> _iconCache = new();

    // 正在提取中的路径（避免重复提取）
    private readonly ConcurrentDictionary<string, bool> _extractingPaths = new();

    public IconExtractorService()
    {
    }

    /// <summary>
    /// 安全地获取进程的可执行文件路径
    /// 使用 QueryFullProcessImageName 替代 Process.MainModule.FileName
    /// </summary>
    public static string? GetProcessPath(int processId)
    {
        IntPtr hProcess = IntPtr.Zero;
        try
        {
            hProcess = Win32.OpenProcess(Win32.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero)
                return null;

            var buffer = new StringBuilder(1024);
            int size = buffer.Capacity;

            if (Win32.QueryFullProcessImageName(hProcess, 0, buffer, ref size))
            {
                return buffer.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (hProcess != IntPtr.Zero)
                Win32.CloseHandle(hProcess);
        }
    }

    /// <summary>
    /// 获取应用图标 Base64 字符串（同步方法，从缓存读取）
    /// 如果未缓存，返回 null 并触发异步提取
    /// </summary>
    public string? GetIconBase64(string exePath)
    {
        if (string.IsNullOrEmpty(exePath))
            return null;

        // 从缓存读取
        if (_iconCache.TryGetValue(exePath, out var cachedIcon))
        {
            return cachedIcon;
        }

        // 如果未缓存且未在提取中，触发异步提取
        if (_extractingPaths.TryAdd(exePath, true))
        {
            _ = Task.Run(() => ExtractAndCacheIcon(exePath));
        }

        return null; // 本次返回 null，下次更新时缓存应该已就绪
    }

    /// <summary>
    /// 提取图标并写入缓存（后台执行）
    /// </summary>
    private void ExtractAndCacheIcon(string exePath)
    {
        try
        {
            if (!File.Exists(exePath))
            {
                _extractingPaths.TryRemove(exePath, out _);
                return;
            }

            // 使用 Shell32 提取大图标
            var shinfo = new SHFILEINFO();
            IntPtr hIcon = Win32.SHGetFileInfo(
                exePath,
                0,
                ref shinfo,
                (uint)Marshal.SizeOf(shinfo),
                Win32.SHGFI_ICON | Win32.SHGFI_LARGEICON
            );

            if (hIcon == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
            {
                _extractingPaths.TryRemove(exePath, out _);
                return;
            }

            try
            {
                // 即用即销毁：Icon -> Bitmap -> MemoryStream -> Base64 String
                using var icon = Icon.FromHandle(shinfo.hIcon);
                using var bitmap = icon.ToBitmap();
                using var ms = new MemoryStream();

                bitmap.Save(ms, ImageFormat.Png);
                var base64 = Convert.ToBase64String(ms.ToArray());
                var result = $"data:image/png;base64,{base64}";

                // 写入缓存
                _iconCache.TryAdd(exePath, result);
            }
            finally
            {
                // 销毁非托管图标句柄
                Win32.DestroyIcon(shinfo.hIcon);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IconExtractor] Failed to extract icon for {exePath}: {ex.Message}");
        }
        finally
        {
            _extractingPaths.TryRemove(exePath, out _);
        }
    }

    /// <summary>
    /// 获取应用图标 Base64 字符串（异步版本）
    /// </summary>
    public async Task<string?> GetIconBase64Async(string exePath)
    {
        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            return null;

        // 查缓存
        if (_iconCache.TryGetValue(exePath, out var cachedIcon))
        {
            return cachedIcon;
        }

        // 异步提取
        await Task.Run(() => ExtractAndCacheIcon(exePath));

        // 再次查缓存
        if (_iconCache.TryGetValue(exePath, out cachedIcon))
        {
            return cachedIcon;
        }

        return null;
    }

    // ===== P/Invoke Definitions =====
    private static class Win32
    {
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref int lpdwSize);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    public void Dispose()
    {
        _iconCache.Clear();
        _extractingPaths.Clear();
    }
}
