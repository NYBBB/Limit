namespace EyeGuard.Core.Interfaces;

/// <summary>
/// 媒体检测接口 (F1.2)。
/// 负责检测是否有音频播放或全屏应用运行。
/// </summary>
public interface IMediaDetector
{
    /// <summary>
    /// 检查是否有音频正在播放。
    /// 用于判断用户是否在观看视频。
    /// </summary>
    bool IsAudioPlaying { get; }
    
    /// <summary>
    /// 检查是否有全屏独占应用运行。
    /// 用于判断用户是否在游戏或演示模式。
    /// </summary>
    bool IsFullscreenAppActive { get; }
    
    /// <summary>
    /// 刷新检测状态。
    /// </summary>
    void Refresh();
}
