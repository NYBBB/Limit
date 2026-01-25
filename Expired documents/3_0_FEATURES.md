# Limit 3.0 功能特性与架构全解

> 文档生成时间: 2026-01-21
> 版本: 3.0 (Alpha)

本文档详细记录了 Limit 3.0 版本的所有开发内容、软件架构设计、算法核心逻辑、数据存储结构以及各页面功能细节。

---

## 1. 软件架构设计

Limit 3.0 采用清晰的分层架构（Clean Architecture 变体），确保逻辑与 UI 分离，便于维护和扩展。

- **Layer 1: EyeGuard.Core (核心层)**
    - **职责**: 定义系统核心实体 (Entities)、接口 (Interfaces) 和枚举 (Enums)。不依赖任何外部库。
    - **内容**: 
        - `FatigueSnapshot` (疲劳快照实体)
        - `UsageRecord` (使用记录实体)
        - `Cluster` (工作流簇实体)
        - `IFatigueEngine`, `IDatabaseService` 等接口定义。

- **Layer 2: EyeGuard.Infrastructure (基础设施层)**
    - **职责**: 实现核心接口，处理具体业务逻辑、系统交互和数据持久化。
    - **核心服务**:
        - `FatigueEngine` (精力算法引擎)
        - `UserActivityManager` (活动监控与状态机)
        - `DatabaseService` (SQLite 数据存储)
        - `PowerAwarenessService` (电源感知)
        - `ClusterService` (应用聚类)
        - `ContextClassifier` (上下文分类)
        - `SettingsService` (配置管理)

- **Layer 3: EyeGuard.UI (表现层)**
    - **职责**: 使用 WinUI 3 构建用户界面，通过 MVVM 模式与基础设施层交互。
    - **技术**: WinUI 3 (Windows App SDK), CommunityToolkit.Mvvm, LiveChartsCore, SkiaSharp。

---

## 2. Empathy Engine (共情引擎) - 核心算法

Limit 3.0 的核心不再是简单的倒计时，而是模拟人类生物节律的非线性数学模型。

### 核心公式
- **疲劳增长 (工作时)**:
    ```
    EffectiveRate = BaseRate * (1 + Fatigue/100) * LoadWeight * (1 + SensitivityBias)
    ```
    - **非线性特性**: 疲劳值越高，增长越快（模拟疲劳累积效应）。
    - **LoadWeight**: 上下文权重 (工作=1.0, 视频=0.3)。
    - **SensitivityBias**: 主观偏差 (-0.5 ~ +0.5)，由用户反馈调节。

- **精力恢复 (休息时)**:
    ```
    RecoveryRate = BaseRecovery * Max(0.2, Fatigue/50)
    ```
    - **弹性恢复**: 疲劳值越高，初期恢复越快；接近 0 时恢复减慢。

### 辅助机制
- **Slope (变化率)**: 计算最近 60 秒的 EMA (指数移动平均) 斜率，用于预测趋势。
- **FatigueState (离散态)**:
    - **Fresh (0-30)**: 精力充沛 (绿色)
    - **Strained (30-60)**: 略感疲劳 (黄色)
    - **Overloaded (60-85)**: 比较疲劳 (橙色)
    - **Grind (85-100)**: 严重透支 (红色)
- **主观校准**:
    - "我感觉很累": 触发 Care Mode，敏感度 +20% (衰减: 5%/小时)。
    - "我还好": 敏感度 -15%。
    - "刚休息过": 直接扣减 15 点疲劳值。

---

## 3. 数据记录与存储

所有数据存储在本地 SQLite 数据库 (`AppData/EyeGuard/data.db`)，保障隐私。

### 核心数据表
1.  **UsageRecord (原始使用记录)**
    -   `AppName`: 应用名称 (如 "Visual Studio")
    -   `WebsiteName`: 网站域名 (如 "github.com")
    -   `DurationSeconds`: 持续秒数
    -   `Date`: 记录日期
    -   *用途: 基础分析数据源*

2.  **HourlyUsageRecord (每小时聚合)**
    -   `AppName`, `Date`, `Hour`, `DurationSeconds`
    -   *用途: 提升图表加载速度，支持 Analytics 页面快速查询*

3.  **FatigueSnapshot (疲劳快照)**
    -   `FatigueValue`: 0-100 的浮点数
    -   `RecordedAt`: 记录时间戳 (每分钟一次)
    -   *用途: 绘制疲劳趋势图，分析过载时间*

4.  **DailyAggregateRecord (每日聚合 - Phase 6)**
    -   `TotalActiveMinutes`: 总活跃时间
    -   `OverloadMinutes`: 过载时间
    -   `PeakFatigue`: 峰值疲劳
    -   *用途: 长期趋势分析*

5.  **Cluster (工作流簇)**
    -   `Name`, `Color`, `AppNames` (逗号分隔)
    -   *用途: Context Monitor 上下文识别*

---

## 4. 页面功能详解

### 4.1 Dashboard 3.0 (仪表盘)
采用 Bento Grid 布局，分为三个核心区域。

-   **Zone A: The Core (精力反应堆)**
    -   **FatigueRing**: 自定义绘制的圆环控件。
        -   **内环**: 动态 Arc 显示疲劳值，支持呼吸动画 (Eco 模式下暂停)。
        -   **外环**: 专注模式下显示倒计时 (双环模式)。
        -   **数字**: 实时疲劳百分比。
    -   **State Indicator**: 左上角显示当前状态 (如 "精力充沛") 和颜色点。
    -   **Eco Icon**: 检测到电池供电时显示绿叶图标。

-   **Zone B: Context Monitor (上下文感知)**
    -   **ContextCard**: 显示当前前台应用及所属 Cluster。
    -   **Focus Control**: 快速开启/停止 "专注模式" (Focus Commitment)。
        -   **交互**: 点击 "专注" -> 弹出 `FocusCommitmentDialog` -> 设定时间/任务。

-   **Zone C: The Timeline (今日概览)**
    -   **Mini Charts**: 包含三个迷你图表。
        1.  **Work Load**: 过去 1 小时的负载曲线 (Slope)。
        2.  **Recovery**: 今日累积恢复效率。
        3.  **Focus**: 今日总专注时长。

### 4.2 Analytics 3.0 (分析中心)
全方位的数据可视化页面。

-   **Insight Banner (顶部)**
    -   **功能**: 每日智能洞察，基于规则引擎分析当日数据，提供一句式建议。
    -   **动效**: 打字机效果显示文字。

-   **Main Charts (核心图表)**
    -   **Hourly Usage (堆叠柱状图)**:
        -   X轴: 0-24 小时
        -   Y轴: 使用分钟数
        -   数据: Top 8 应用独立配色，其他合并为灰色。
    -   **Fatigue Trend (折线图)**:
        -   X轴: 时间
        -   Y轴: 疲劳值
        -   数据: 基于 `FatigueSnapshot`，显示全天精力波动。

-   **The Grind Statistics (透支统计)** (Phase 6)
    -   **Longest Session**: 最长连续工作时间 (未休息)。
    -   **Overload Time**: 处于红色/橙色状态的总分钟数。
    -   **Overload %**: 过载时间占比。

-   **Energy Pie (精力分布)** (Phase 6)
    -   **类型**: 环形图
    -   **分类**: 工作/学习, 娱乐, 沟通, 其他 (基于 `ContextClassifier`)。

-   **Weekly Trends (周趋势)** (Phase 6)
    -   **类型**: 分组柱状图
    -   **数据**: 过去 7 天的 **峰值疲劳** (橙色) vs **平均疲劳** (紫色)。

### 4.3 Rules & Settings (规则与设置)
-   **Intervention Ladder (干预阶梯)**: 设置不同疲劳等级下的干预方式 (通知/全屏/强制休息)。
-   **Eco Mode**: 手动开启/关闭电源感知功能。
-   **Productivity**: 设定聚类与上下文规则。

---

## 5. 特色功能系统

### Focus Commitment (专注承诺) - Phase 9
-   **目的**: 解决 "不知不觉就开始摸鱼" 的问题。
-   **交互**:
    -   用户主动承诺 "通过 30 分钟完成代码编写"。
    -   **视觉反馈**: FatigueRing 变为双环模式，外环显示倒计时。
    -   **逻辑**: 计时器运行，结束后提醒。

### Power Awareness (电源感知) - Phase 10
-   **目的**: 笔记本电池供电时降低功耗。
-   **实现**:
    -   使用 Win32 `GetSystemPowerStatus` 轮询 (3秒间隔)。
    -   **降级策略**:
        -   暂停 `FatigueRing` 呼吸动画。
        -   显示绿色省电图标。
        -   (未来) 降低图表刷新率。

### Empathy Calibration (共情校准)
-   **交互**: 通过 "校准" 菜单，用户告诉系统 "我很累" 或 "我不累"。
-   **逻辑**: 系统根据反馈调整内部参数 `SensitivityBias`，使算法更贴合用户实际体感。

---

## 6. 开发总结

Limit 3.0 从简单的定时提醒工具进化为**上下文感知的精力管理系统**。它不仅记录时间，更试图理解用户的"累"，并通过视觉、数据和干预手段，帮助用户建立可持续的数字生活习惯。
