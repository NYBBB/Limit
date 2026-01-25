
# Limit 2.0 策划文档

## 0. 版本定位

**Limit 1.0（旧方向）**：屏幕时间统计 + 强制休息提醒，复刻 macOS Eye Monitor 的体验（Mica/极简/Overlay）。
**Limit 2.0（新方向）**：实时“认知疲劳代理（Cognitive Fatigue Proxy）”监控 + 枯竭预测（Burnout Forecast）+ 贴心建议（Coaching）+ 任务化休息（Break Tasks），并保留“久坐站立提醒”作为任务类型融合。

> 核心差异：从“计时器驱动”转为“数据与模型驱动（signals → inference → interventions）”。

---

## 1. 核心目标与用户价值

### 1.1 核心目标（按优先级）

P0：**枯竭预测** —— 告诉用户“还能高效率工作多久”，并给出可操作的延长方案（切换低负荷模式/做一个任务化休息）。
P0：**贴心建议体系** —— 分级干预（nudge → micro break → long break → overlay），每条建议可解释、可追踪、可评估。
P1：**上下文语义分类** —— 解决 “浏览器万能容器（YouTube 悖论）”，让统计和疲劳系数可信。
P1：**行为生物特征疲劳估算（Behavioral Biometrics）** —— 用输入模式异常检测识别“垃圾时间（The Grind）”，构建护城河。
P1：**Analytics 升级为能量流（Energy Flow）** —— 按“疲劳贡献”而非“时长”解释用户问题（Energy Pie、疲劳速率热力图、恢复质量、ROI 矩阵等）。

### 1.2 价值主张（Value Proposition）

* “我现在累不累”不靠玄学：用可解释的代理指标衡量。
* “我还能坚持多久”：给出时间到阈值的预测和可选方案。
* “我该怎么做”：用任务化建议引导用户恢复，而不是粗暴打断。

---

## 2. 现有实现盘点（用于重构对齐）

你们已完成/具备的能力（Limit 2.0 的底座）：

* 全局输入钩子、idle 判定（GlobalInputMonitor）。
* 音频检测与媒体豁免（AudioDetector）。
* 实时疲劳引擎（FatigueEngine：非线性曲线）。
* 用户状态机（UserActivityManager：Active/MediaMode/Idle/Away）。
* SQLite 持久化（DatabaseService、UsageCollectorService、SettingsService）。
* 网站识别引擎（WebsiteRecognizer：100+ 规则）。
* WinUI3 UI：Dashboard、Analytics 骨架、RulesPage、Overlay、Tray、图标映射等。

另外已有数据能力：

* FatigueSnapshot 持久化 + 历史趋势加载。
* HourlyUsageRecord 按小时聚合 + 24h 堆叠柱图。

---

## 3. Limit 2.0 总体架构（设计原则）

### 3.1 设计原则

1. **可解释性（Explainable by default）**：任何建议/预测都要能显示 “why”。
2. **可退化（Graceful degradation）**：AI 不可用时自动退回规则/统计模型，不影响主功能。
3. **隐私优先（On-device first）**：文本分类与特征提取本地执行；Title 可脱敏（SRS 已明确）。
4. **低资源（Low overhead）**：后台采样、限频、缓存；默认不保存原始按键内容。

### 3.2 模块分层（与现有 Clean Architecture 对齐）

维持现有分层（Core / Application / Infrastructure / UI）。
新增/重构的关键模块（逻辑名，供 Codex 建目录）：

**Infrastructure**

* InputSignalCollector（已有 GlobalInputMonitor + 扩展聚合）
* WindowContextCollector（process/title/domain）
* AudioFullscreenDetector（已有 AudioDetector + 可加全屏）
* LocalNlpClassifier（ONNX/ML.NET 推断）
* BehaviorFeatureExtractor（输入行为特征聚合）

**Core**

* StateModel（四层正交状态：见第 4 节）
* FatigueModel（dF/dt 合成模型）
* ForecastModel（time-to-threshold + what-if）
* InterventionPolicy（建议/提醒策略引擎）
* BreakTaskSystem（任务生成、结算、合规率）

**Application**

* Orchestrator：把 signals → state → fatigue → forecast → interventions 串起来
* Persistence：写入 snapshots、usage、tasks、features、forecasts

**UI**

* Dashboard 2.0（Burnout countdown + next best action）
* Analytics PRO（Energy Flow）
* Coaching Center（建议历史/合规率/任务完成）
* Privacy & AI Settings（模型开关、脱敏策略）

---

## 4. 状态机重构：四层正交状态（必须做）

你现有的 Active/MediaMode/Idle/Away 要保留作为 ActivityState 基层。
但 Limit 2.0 增加三层状态用于“推断与决策”：

### 4.1 状态定义

**A. ActivityState（行为态）**

* Active / Idle / Away / MediaMode（沿用）

**B. ContextState（语义上下文态）**

* Work / Learning / Entertainment / Communication / Neutral
  来源：窗口标题、网站 domain、规则与 NLP 推断。

**C. LoadState（认知负荷态）**

* Low / Medium / High
  由 ContextState + AppType + 行为特征派生（规则/模型）。

**D. FatigueState（疲劳风险态）**

* Fresh / Strained / Overloaded / Grind
  用于决定干预强度、预测阈值、报表统计（例如 “The Grind”）。

### 4.2 状态更新频率

* ActivityState：事件驱动（输入钩子）+ 1s tick
* ContextState：窗口切换事件驱动 + 限频（例如 2–5s/次）
* LoadState：每 10s 计算一次（或窗口切换时）
* FatigueState：每 10s 计算一次（基于 Fatigue 值 + slope）

---

## 5. 数据与信号（Signals）规范

### 5.1 输入信号（已存在 + 扩展）

* Keyboard/mouse activity、idle duration（已有）
* Active window：processName + windowTitle（已有并要求 title 脱敏）
* Audio playing（已有）
* Website recognition：domain / site label（已有 100+ 规则）

### 5.2 行为生物特征（阶段二，MVP 先聚合不建模）

“只存统计，不存内容”的特征集（按 60s window 聚合）：

* KeystrokesPerMin
* KeyIntervalMean / KeyIntervalStd（敲击间隔均值/方差）
* BackspaceRate
* MouseDistance / MouseSpeedMean / MouseJitter（轨迹抖动可用曲率/方向变化近似）
* WindowSwitchCountPerMin（碎片化）

---

## 6. 疲劳模型（FatigueModel）规格：可解释、可插拔

你们已实现非线性疲劳曲线与实时疲劳值计算。
2.0 需要把 “时间×系数”升级为“多因子合成”，并支持 AI/biometrics 插拔。

### 6.1 基本输出

* `Fatigue ∈ [0,100]`（连续值）
* `FatigueSlope`（单位：%/min，平滑后）
* `FatigueState`（离散态）

### 6.2 计算框架（建议实现为接口 + 参数表）

定义每次 tick（例如 10s）更新：

**dF/dt = BaseCurve(F) × LoadWeight(Context) × BehaviorPenalty − RecoveryCredit**

* BaseCurve(F)：你们现有非线性曲线（保留）
* LoadWeight(Context)：由 ContextState 决定（阶段一 AI 会影响）
* BehaviorPenalty：阶段二 biometrics 输出，默认 1.0（无惩罚）
* RecoveryCredit：来自 Idle/Away/BreakTask 完成结算

> 重点：把每一项的数值记录在 “FatigueExplainSnapshot”，用于 UI 的 “why” 和 Debug。

### 6.3 默认权重（供 MVP）

* Work/Learning：1.0
* Communication：0.7
* Neutral：0.5
* Entertainment：0.3
  与“耗能应用 1.0 vs 低耗能 0.3”的既有想法一致。

---

## 7. 枯竭预测（Burnout Forecast）规格：P0 必做

你们 task 已把这块定义为核心：Dashboard 倒计时 + “切换模式延长”提示。

### 7.1 定义

* `ThresholdHighEfficiency`：例如 85（可配置）
* `TimeToThreshold (TTE)`：按当前增长率估算达到阈值的剩余时间

### 7.2 即时预测（v1）

* `slope = EMA(FatigueSlopeRaw, window=5–10min)`
* `TTE = (Threshold - FatigueNow) / slope`
* 若 slope 很小或负（在恢复）：显示 “恢复中” 或 “> 2h”。

### 7.3 What-if 预测（v1.1）

计算多个场景：

* Scenario A：保持当前上下文
* Scenario B：切到 MediaMode / Entertainment（LoadWeight=0.3）
* Scenario C：立刻执行一个 BreakTask（先减 RecoveryCredit，再算 TTE）

UI 输出示例：

* “按当前强度：42 分钟后进入低效区”
* “若切换到低负荷：可延长至 1小时15分”

---

## 8. 贴心建议系统（InterventionPolicy）：保留并升级为“规则表 + 可解释记录”

### 8.1 干预分级

* L0：Nudge（轻提示，不打断）
* L1：Micro break（20–30s）
* L2：Long break（2–5min / 站立走动）
* L3：Overlay（全屏遮罩，已有）

Overlay 交互延续 Skip/Snooze。

### 8.2 建议触发条件（MVP 规则表示例）

每条建议都记录 `reason`：

* Rule: `Fatigue > 60 && slope high` → L1 micro break
  reason：“疲劳增长速度高于今日均值”
* Rule: `Fatigue > 80 for > 10min` → L2 long break
  reason：“进入 The Grind 区间（>80%）”
* Rule: `ActiveContinuous > 30min` → Mobility task（久坐站立）
  reason：“连续工作超过 30 分钟（久坐保护）”
* Rule: `WindowSwitchRate high` → L0 nudge
  reason：“专注碎片化上升（切换频率高）”

---

## 9. 任务化休息（Break Tasks）：把“提醒”变成“闭环”

你们已有完整的交互设想：生成任务 → 执行 → 点击完成 → 算法结算，并按任务类型与当前疲劳动态结算，提供成就感。

### 9.1 Task 类型（MVP）

* EyeTask（20-20-20 / 20s）
* BreathTask（呼吸放空 30s）
* MobilityTask（站立/走动 60–120s）— 用于“久坐提醒”
* StretchTask（肩颈 30s）

### 9.2 结算（Settlement）

完成任务产生：

* `RecoveryCredit`（直接影响 FatigueModel）
* `Compliance`（完成率、推迟率、跳过率）

### 9.3 与 Overlay 的关系

* L1/L2 触发时可直接生成 Task，并在 Overlay 中展示倒计时与“完成”按钮（当前 Overlay 已有倒计时与按钮体系，可复用）。

---

## 10. AI 引入路线：两阶段、强隐私、可退化

### 10.1 阶段一：本地语义上下文分类（解决浏览器容器问题）

输入：Window Title（脱敏后）+ domain + processName。
输出：ContextState 标签（Work/Learning/Entertainment/Communication/Neutral）。

实现建议（落地要点）：

* Rule-first：WebsiteRecognizer/规则先打标签（你们已有 100+ 规则库）
* Model-fallback：规则不确定或冲突时再跑本地模型
* 缓存：同一 title/domain 的分类结果缓存 10–30 分钟，降低开销

模型形态：

* ML.NET + ONNX runtime（BERT-tiny 或轻量文本分类）
* 若要 zero-shot，建议先用“少标签监督模型 + 规则增强”，zero-shot 在本地资源与准确率上风险更高（可作为后续迭代）。

隐私卖点：

* 全离线推断
* 输入 title 先脱敏（SRS 已要求）
* 可提供 “禁用 AI / 仅规则” 开关

### 10.2 阶段二：行为生物特征异常检测（护城河）

目标：识别“垃圾时间”与注意力下降，并将其作为 `BehaviorPenalty` 影响疲劳增长，而不是直接宣称“你累了”。

流程：

1. Calibration Week（前 7 天基线采集）
2. 实时特征聚合（按分钟）
3. Anomaly score → BehaviorPenalty（如 1.0–1.8）
4. 用于：

   * 提前拉高 Grind 风险
   * 提前触发建议/任务
   * 提供 “你已经进入低 ROI 时段” 的解释性提示

---

## 11. Analytics PRO：从“时间”升级为“能量流”

你们 task 里已经定义了 Analytics PRO 的结构与图表目标，建议直接按该设计落地：Overview / Trends / App Profiling / Browser Insights。

### 11.1 Overview（精力流向）

* Energy Pie：按“疲劳贡献”而非时长分布
* The Grind：疲劳>80% 强行工作时长

### 11.2 Trends（规律）

* 疲劳速率热力图（GitHub contribution 风格）
* 恢复质量散点（休息时长 vs 疲劳下降幅度；有效休息）

### 11.3 App Profiling（谁在耗你）

* ROI 矩阵：时间投入 vs 疲劳产出
* 专注碎片化指数：窗口切换频率

### 11.4 Browser Insights（浏览器内网站分析）

* 利用 HourlyUsageRecord + 网站识别聚合“浏览器内 Top 网站”
* UI：点击浏览器柱子展开 Top3 网站

> 你们已有 FatigueSnapshot、HourlyUsageRecord、堆叠柱图基础，属于“数据结构已就位，换统计口径与图表逻辑”。

---

## 12. 数据库与实体（SQLite）扩展建议

现有：Usage、Settings、FatigueSnapshot、HourlyUsageRecord（已做）
新增建议实体（为 2.0 功能服务）：

1. `ContextClassificationRecord`

* timestamp, process, domain, title_hash, context_label, confidence, source(rule/model)

2. `BehaviorFeatureMinute`

* timestamp_minute, key_count, interval_mean, interval_std, backspace_rate, mouse_jitter, switch_count, anomaly_score

3. `BreakTaskRecord`

* id, created_at, task_type, duration, trigger_reason, completed_at, result(completed/snoozed/skipped), recovery_credit

4. `ForecastSnapshot`

* timestamp, fatigue, slope, threshold, tte_current, tte_lowload, tte_after_task

5. `FatigueExplainSnapshot`（可选但强烈建议）

* timestamp, base_curve, load_weight, behavior_penalty, recovery_credit, fatigue_delta, reason_codes

---

## 13. UI/UX 2.0 信息架构（在现有 WinUI3 基础上改）

你们 SRS 的 UI 风格规范可沿用：Mica、极简、托盘、Overlay。
在 Dashboard 的四卡片里，新增/替换为：

* Fatigue（实时）
* Burnout Countdown（TTE）
* Next Best Action（建议/任务入口）
* Longest Focus Streak / Grind Time Today

Overlay（已有）继续作为 L3 承接，保持 Skip/Snooze。

Settings 增加：

* AI 开关（规则-only / hybrid / full）
* 隐私（title 脱敏级别、是否保存 title）
* Calibration（基线采集状态）
* Intervention 强度（coaching-first / enforce）

---

## 14. 里程碑（给 Codex 的实现顺序）

按“先 P0 可交付，再逐步 AI 化”的顺序，避免返工：

### Milestone 1（P0）：Burnout Forecast v1 + Explainable Fatigue

* 把 FatigueModel 改为可解释框架（记录各项因子）
* 计算 slope（EMA）
* Dashboard 显示 TTE（当前强度）
* 写入 ForecastSnapshot
  **验收**：能稳定显示倒计时；疲劳恢复时显示合理状态；CPU/内存无明显上升。

### Milestone 2（P0）：Break Tasks（任务化休息）+ 久坐站立任务

* Task 生成与完成结算
* MobilityTask：连续 Active > 30min 触发（久坐保护）
* 在 Overlay/Toast 承接任务
  **验收**：完成任务会降低 Fatigue；任务完成率可统计。

### Milestone 3（P0→P1）：What-if Forecast + “延长方案”提示

* 计算低负荷场景 TTE（LoadWeight=0.3）
* 计算 “执行任务后” TTE
  **验收**：能展示至少 2 个场景对比，并与用户体验一致（切换娱乐/媒体确实延长）。

### Milestone 4（P1）：NLP Context Classification（Hybrid）

* 规则先行（WebsiteRecognizer）
* 模型兜底（本地 ONNX）
* 缓存 + 限频 + Debug 展示
  **验收**：同为 Chrome，不同标题能区分标签；开关 AI 不影响主流程。

### Milestone 5（P1）：Behavior Biometrics v1（特征 + 异常分）

* 先落地 FeatureMinute 表
* 异常检测先用统计阈值/简单模型（后续再升级）
* 输出 BehaviorPenalty 影响疲劳增长
  **验收**：在明显疲劳/混乱操作时，Grind 风险提前上升；不保存敏感内容。

### Milestone 6（P1）：Analytics PRO（能量流）

* Energy Pie、The Grind、Heatmap、Recovery scatter、ROI、碎片化、Browser Insights 
  **验收**：图表与数据口径一致；用户能看懂“为什么累”。

---

## 15. 风险与对策（必须写进实现注意事项）

1. **误判导致“烦人”**：默认 coaching-first；强制 overlay 作为可选。
2. **隐私担忧**：title 脱敏 + hash；AI 离线；提供“仅规则模式”。
3. **资源开销**：分类限频缓存；特征按分钟聚合；不存原始输入事件。
4. **缺少真实疲劳标签**：阶段二先异常检测；同时加入可选“每日 5 秒自评”作为弱标签（后续可做个性化）。
