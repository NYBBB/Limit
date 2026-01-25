# Limit 3.0 产品策划文档 (Refined)

**项目代号**: Limit (Energy Guardian)

**核心哲学**: **Quiet Technology (静默科技)** —— 不打扰的心流守护者。

**技术栈**: C# / WinUI 3 (Windows App SDK) / .NET 8 / SQLite / Win32 API

---

## 一、 产品愿景 (Vision)

Limit 3.0 不再是一个单纯的“疲劳计算器”或“倒计时闹钟”。
它是用户的**精力守护者**。它理解工作的上下文，体谅用户的状态，只在必要时介入。它旨在消除“假性繁忙”和“碎片化消耗”，帮助用户维持高质量的心流状态。


## 二、 核心算法引擎: The Empathy Engine

抛弃单纯的 `Time * Weight` 线性逻辑，引入动态校准与上下文感知。

### 2.1 基础计算与修正

* **基础逻辑**: `Fatigue = (Time * AppWeight) + ContextSwitchPenalty`。
* **主观校准 (Calibration Loop)**:
* **输入**: 用户可通过托盘/界面反馈“我感到很累”或“我不累”。
* **机制**:
* 用户反馈“累” -> `SensitivityBias` (敏感度偏差) 瞬间 +20%。
* **Care Mode (关怀模式)** 开启：UI 显示柔和光晕，干预阈值降低。
* **情绪衰减**: `SensitivityBias` 每小时自动衰减 5% 直至归零（模拟休息后的恢复）。





### 2.2 工作流簇 (Context Clusters)

解决“误杀工作切换”的问题。

* **定义**: 允许用户定义（或系统预设）一组相关应用为一个 Cluster。
* *例: Game Dev Cluster = Unity + Visual Studio + Photoshop + Chrome (StackOverflow).*


* **判定逻辑**:
* **簇内切换 (Intra-cluster)**: 切换惩罚 = 0。视为连续工作。
* **簇外切换 (Inter-cluster)**: 切换惩罚 > 0。视为注意力分散。


* **智能辅助**: 使用轻量级规则（标题关键词如 `Docs`, `API`）自动将浏览器页面归类到 Coding Cluster。

### 2.3 活跃与空闲判定 (True Active Detection)

修复“看视频被当成空闲”的 Bug。

* **判定公式**: `IsActive = (PhysicalInput || (AudioOutput > Threshold && IsMediaApp) || IsFullscreen)`
* **PhysicalInput**: 鼠标键盘操作（Win32 Hook）。
* **PassiveConsumption**: 无输入但有音频/全屏 -> 判定为“被动消耗”（低负载，不恢复精力）。

---

## 三、 用户界面设计: The Bento Dashboard

主界面采用不可滚动的 **Bento Grid (便当盒布局)**，极简、抗噪。

### 3.1 Zone A: The Core (精力反应堆) - [左侧 50%]

* **视觉**: 带有呼吸动效的亚克力圆环。颜色随疲劳度流转 (青 -> 琥珀 -> 珊瑚红)。
* **状态指示**:
* 显示当前心理状态文案（如 "Deep Flow", "Zoning Out"）。
* **Care Mode 指示**: 若处于高敏感模式，圆环旁显示微弱的“创可贴/爱心”图标。


* **交互**:
* Hover 显示具体百分比。
* 点击圆环中心触发“手动校准”或“快速休息”。
* **即时建议**: 取代“枯竭倒计时”，显示当前适合的行为（"适合高强度Coding" / "仅处理邮件" / "必须停下"）。



### 3.2 Zone B: Context Awareness (上下文感知) - [右上 25%]

* **显示**: 当前应用图标 + 所属 Cluster 名称。
* **纠偏交互**: 显眼的 Toggle 开关 **[I'm Working / Just Chilling]**。
* 用于当算法误判（如看网课被判娱乐）时，用户一键手动覆盖状态。



### 3.3 Zone C: Energy Drainers (精力黑洞) - [右下 25%]

* **显示**: 仅显示 **Top 3** 消耗精力的源头（进度条形式）。
* **碎片化警示**: 如果检测到大量 <10分钟 的无意义切换，显示一个灰色的 **"Fragmented Time" (碎片时间)** 进度条，警示用户注意力已经散了。

---

## 四、 交互系统: The Intervention Ladder

遵循“礼貌原则”，层层递进。

### 4.1 Level 1: Ambient (环境感知) [0-60%]

* **行为**: 仅托盘图标变色。主界面呼吸平缓。
* **打扰**: 0。

### 4.2 Level 2: Soft Nudge (轻推) [60-80%]

* **行为**: 发送 **Toast 通知**。
* **文案**: "效率正在边际递减。切换到 Code Review 或文档整理效果更好。"
* **交互**: 只有在检测到 `ContextSwitch` 频率突然变高时才触发（抓用户走神的瞬间）。

### 4.3 Level 3: Hard Stop (强干预) [80%+]

* **行为**: 全屏覆盖层 (Overlay)。黑色半透明，亚克力模糊。
* **交互**:
* 显示 "Diminishing Returns" (收益递减)。
* **摩擦力设计**: 解锁按钮需要**长按 3 秒**才能关闭。增加拒绝休息的成本。



### 4.4 Lifecycle: The Daily Briefing (日次晨报)

* **场景**: 电脑从睡眠唤醒或开机，Limit 后台静默启动。
* **触发**: 检测到用户活跃 3 分钟后（给用户缓冲期）。
* **形式**: Toast 简报 -> 点击弹出精美卡片。
* **内容**:
* 昨日总结 ("昨天专注度击败了 80% 的日子")。
* 今日状态 ("精力充沛，Care Mode 已关闭")。
* 鼓励 ("去创造点什么吧。")。



---

## 五、 数据架构: Anti-Bloat Strategy

防止 SQLite 膨胀导致性能下降。

### 5.1 数据分层

* **Hot Data (热数据)**: 最近 7 天。保留秒级/分钟级详细记录（用于生成今日/本周图表）。
* **Cold Data (冷数据)**: 7 天前的数据。

### 5.2 聚合策略 (Roll-up Service)

* **执行时机**: 每日闲时或软件启动时。
* **逻辑**:
* 将 7 天前的 `HourlyUsageRecord` 合并。
* **碎片压缩**: 所有 < 5分钟的非工作类记录，合并为一条 `Scattered Usage`。
* 保留关键指标：`TotalActiveTime`, `PeakFatigue`, `ClusterDistribution`。
* 删除原始记录。



### 5.3 分析页 (Analytics)

* **功能**: 承载复杂数据可视化。
* **组件**:
* **Heatmap (热力图)**: Hour x Day 的精力投入分布。
* **Cluster TreeMap**: 按工作流簇展示的时间消耗。
* **Flow Timeline**: 将连续的、同一 Cluster 的操作连成线，直观展示“心流”有多长。



---

## 六、 技术实施关键点 (Technical Notes)

### 6.1 性能优化

* **Win32 Title Hook**: 优先读取窗口标题进行关键词匹配（消耗极低）。
* **UI Automation (FlaUI) 防抖**: 仅当 Window 句柄停留 > 2秒，且标题无法识别时，才异步调用 FlaUI 获取 URL。
* **Audio Detection**: 使用 CSCore 监听系统音频峰值，辅助判断“被动消耗”。

