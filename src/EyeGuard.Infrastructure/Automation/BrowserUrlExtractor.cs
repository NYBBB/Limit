namespace EyeGuard.Infrastructure.Automation;

using System;
using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

/// <summary>
/// 浏览器 URL 提取器 - 通过 UI Automation 获取浏览器地址栏内容
/// </summary>
public class BrowserUrlExtractor
{
    /// <summary>
    /// 从浏览器窗口获取当前 URL
    /// </summary>
    /// <param name="windowHandle">窗口句柄</param>
    /// <returns>URL 字符串，失败返回 null</returns>
    public static string? GetBrowserUrl(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return null;

        try
        {
            using var automation = new UIA3Automation();
            var window = automation.FromHandle(windowHandle);

            if (window == null)
                return null;

            // 地址栏可能的 Name 属性（多语言兼容）
            string[] addressBarNames = 
            {
                "Address and search bar",    // Chrome/Edge English
                "Search or enter address",   // Chrome alternative
                "地址和搜索栏",                // Chrome/Edge 中文
                "地址栏",                      // 通用中文
                "アドレスバー"                 // 日语
            };

            // 遍历所有可能的地址栏名称
            foreach (var addressBarName in addressBarNames)
            {
                var url = TryGetUrlByName(automation, window, addressBarName);
                if (url != null)
                    return url;
            }

            // 如果按名称查找失败，尝试按 AutomationId 查找（某些浏览器）
            var urlById = TryGetUrlByAutomationId(automation, window);
            if (urlById != null)
                return urlById;

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BrowserUrlExtractor] Error: {ex.Message}");
            return null;
        }
    }

    private static string? TryGetUrlByName(UIA3Automation automation, AutomationElement window, string addressBarName)
    {
        try
        {
            // 查找地址栏元素（编辑框）
            var condition = new AndCondition(
                new PropertyCondition(automation.PropertyLibrary.Element.ControlType, FlaUI.Core.Definitions.ControlType.Edit),
                new PropertyCondition(automation.PropertyLibrary.Element.Name, addressBarName)
            );

            var addressBar = window.FindFirstDescendant(condition);

            if (addressBar != null)
            {
                // 获取文本值
                var valuePattern = addressBar.Patterns.Value.Pattern;
                return valuePattern?.Value;
            }
        }
        catch { }

        return null;
    }

    private static string? TryGetUrlByAutomationId(UIA3Automation automation, AutomationElement window)
    {
        try
        {
            // Chromium 浏览器的地址栏可能有 AutomationId
            string[] automationIds = { "OmniboxViewViews", "addressbar" };

            foreach (var id in automationIds)
            {
                var condition = new PropertyCondition(automation.PropertyLibrary.Element.AutomationId, id);
                var addressBar = window.FindFirstDescendant(condition);

                if (addressBar != null)
                {
                    var valuePattern = addressBar.Patterns.Value.Pattern;
                    return valuePattern?.Value;
                }
            }
        }
        catch { }

        return null;
    }
}
