using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Web.WebView2.Core;

namespace EyeGuard.UI.Bridge;

/// <summary>
/// JS → C# 消息处理器
/// 接收前端发送的命令并分发给相应处理器
/// </summary>
public class MessageHandler
{
    private readonly CoreWebView2 _webView;
    private readonly Dictionary<string, Action<JsonElement>> _actionHandlers = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="webView">WebView2 核心对象</param>
    public MessageHandler(CoreWebView2 webView)
    {
        _webView = webView;
        _webView.WebMessageReceived += OnWebMessageReceived;
    }

    /// <summary>
    /// 注册命令处理器
    /// </summary>
    /// <param name="action">命令名称</param>
    /// <param name="handler">处理函数</param>
    public void RegisterHandler(string action, Action<JsonElement> handler)
    {
        _actionHandlers[action] = handler;
    }

    /// <summary>
    /// 发送消息到 JS 前端
    /// </summary>
    /// <param name="type">消息类型</param>
    /// <param name="payload">消息载荷</param>
    public void SendToJS(string type, object payload)
    {
        var message = new
        {
            type,
            payload
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);
        _webView.PostWebMessageAsJson(json);
    }

    /// <summary>
    /// 发送疲劳数据更新
    /// </summary>
    public void SendFatigueUpdate(double value, string status, string color, double breathRate, bool isCareMode)
    {
        SendToJS("FATIGUE_UPDATE", new
        {
            fatigueValue = value,  // 前端期望 fatigueValue
            state = status,        // 前端期望 state
            color,
            breathRate,
            isCareMode
        });
    }

    /// <summary>
    /// 发送上下文数据更新
    /// </summary>
    public void SendContextUpdate(string appName, string displayName, string cluster, int sessionDuration, bool isFocusing)
    {
        SendToJS("CONTEXT_UPDATE", new
        {
            appName,
            displayName,
            cluster,
            sessionDuration,
            isFocusing
        });
    }

    /// <summary>
    /// 发送消耗排行更新
    /// </summary>
    public void SendDrainersUpdate(object[] items, int totalDuration, int fragmentationCount)
    {
        SendToJS("DRAINERS_UPDATE", new
        {
            items,
            totalDuration,
            fragmentationCount
        });
    }

    /// <summary>
    /// 处理从 JS 接收的消息
    /// </summary>
    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("action", out var actionElement))
            {
                var action = actionElement.GetString();
                if (action != null && _actionHandlers.TryGetValue(action, out var handler))
                {
                    var data = root.TryGetProperty("data", out var dataElement)
                        ? dataElement
                        : default;

                    handler(data);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Bridge] 消息解析错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 发送分析数据更新
    /// </summary>
    public void SendAnalyticsUpdate(object data)
    {
        SendToJS("ANALYTICS_DATA_UPDATE", data);
    }

    /// <summary>
    /// 发送调试状态更新
    /// </summary>
    public void SendDebugStatusUpdate(object data)
    {
        SendToJS("DEBUG_STATUS_UPDATE", data);
    }

    /// <summary>
    /// 发送设置数据
    /// </summary>
    public void SendSettingsLoaded(object data)
    {
        SendToJS("SETTINGS_LOADED", data);
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        _webView.WebMessageReceived -= OnWebMessageReceived;
    }

    /// <summary>
    /// JSON 序列化选项
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
