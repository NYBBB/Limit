老版策划文档：


暂定核心框架: .NET 8.0 (C# 12)
UI 框架: WinUI 3 (Windows App SDK 1.4+) —— 追求 Win11 原生 Mica/Acrylic 效果。
架构模式: MVVM (Model-View-ViewModel) + Clean Architecture (分层架构)。
MVVM 库: CommunityToolkit.Mvvm (使用 Source Generators 简化代码)。
数据库: SQLite (本地文件存储) + sqlite-net-pcl (ORM)。
图表库: LiveCharts2 (Geared for WinUI) —— 用于绘制高性能圆环图和柱状图。
依赖注入: Microsoft.Extensions.DependencyInjection。
系统交互: P/Invoke (User32.dll, Kernel32.dll) 用于底层钩子。




1.1 核心目标
构建一个运行在 Windows 上的现代化护眼与生产力工具。它必须具备 macOS 应用 "Eye Monitor" 的视觉美感（毛玻璃、极简风格），同时提供精准的屏幕时间统计和非侵入式的强制休息提醒。
1.2 功能模块需求
F1: 状态监测系统 (State Monitoring System)
•	F1.1 输入检测: 监听全局键盘/鼠标事件。若无输入超过 60秒 (可配)，标记为 Idle 状态。
•	F1.2 媒体/全屏豁免 (Media/Game Mode):
o	若检测到音频输出（正在看视频），即使无输入也不进入 Idle，不打断。
o	若检测到全屏独占应用（如游戏、PPT放映），自动推迟强提醒（降级为 Toast 通知）。
•	F1.3 窗口追踪: 记录当前激活窗口的 ProcessName (e.g., chrome.exe) 和 WindowTitle (e.g., YouTube - Chrome)。需对 Title 进行敏感词脱敏（如 "Password", "Bank"）。
F2: 休息策略引擎 (Break Strategy Engine)
•	F2.1 疲劳值算法 (Fatigue Algorithm):
o	维护变量 Fatigue (0-100%)。
o	规则: 工作 1 分钟 +1%；休息 1 分钟 -20%。
o	UI 需实时展示此数值的变化。
•	F2.2 提醒类型:
o	Micro-break: 每 15 分钟提示 20 秒（缓解视疲劳）。
o	Long-break: 每 45 分钟提示 5 分钟（站立/走动）。
•	F2.3 强制遮罩 (The Overlay):
o	触发时显示全屏、置顶窗口。
o	视觉: 背景为高斯模糊（Acrylic/Blur），而非纯黑。
o	操作: 提供 "Skip"（跳过）和 "Snooze"（推迟5分钟）按钮。允许 Alt+Tab 切换以处理紧急情况，不锁死键盘。
F3: 数据与报表 (Analytics dashboard)
•	F3.1 仪表盘: 显示“今日总时长”、“疲劳值实时读数”、“今日最常用 App (Top 5)”。
•	F3.2 历史趋势: 柱状图显示过去 7天/30天 的使用时长。
•	F3.3 分类统计: 基于规则将 App 归类（Work, Entertainment, Social, Other）并显示饼图。
F4: 现代化 UI/UX
•	F4.1 材质: 必须启用 Windows 11 Mica 背景材质。
•	F4.2 布局: 左侧导航栏 (NavigationView)，右侧内容区。
•	F4.3 托盘: 最小化到系统托盘，悬停显示今日概览。


临时 UI 设计规范 (UI Design Spec)
“index.html”里是示意demo，并不是最终版，只提供参考
交给 AI 写 XAML 时，强调以下设计语言：

1.1 样式指南 (Style Guide)
Window Backdrop: MicaBackdrop (Kind=Base)。
Color Palette:
Primary: #8A2BE2 (BlueViolet - 类似 Eye Monitor 的紫色)。
Background: Transparent (由 Mica 处理)。
Cards: SolidColorBrush Color="#FFFFFF" Opacity="0.05" (深色模式下) + CornerRadius="8".
Font: Segoe UI Variable Display (标题), Segoe UI Variable Text (正文)。

1.2 关键界面布局
Dashboard (主页):
顶部: 4个卡片并排 —— 疲劳值(带进度环)、今日总时长、下次休息倒计时、最长连续工作时间。
中部: LiveCharts2 柱状图，X轴为时间(0-24h)，Y轴为活跃度。无网格线，极简风格。
底部: “最近使用的应用”列表，包含图标、名称、时长条。

Overlay Window (遮罩):
WindowStyle="None", WindowState="Maximized", Topmost="True".
背景使用 AcrylicBrush (Tint="#000000", TintOpacity="0.6")。
中央显示巨大的倒计时文本，下方两个幽灵按钮 (Ghost Button): "Skip", "Snooze"。