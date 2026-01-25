# Limit 3.0 - UI/UX Design Specification

**设计语言**: Fluent Design System (WinUI 3)

**核心隐喻**: **"The Living Reactor" (有生命的反应堆)** **材质**: Mica Alt (云母), Acrylic (亚克力), Smoke (烟熏)

---

## 一、 全局样式与视觉系统 (Global Visual System)

### 1. 窗口架构 (Window Frame)

* **无边框设计**: 移除标准 Windows 标题栏。
* **材质**: 启用 `MicaBackdrop` 或 `DesktopAcrylicBackdrop`。让窗口背景隐约透出桌面壁纸的颜色，增强“融入感”。
* **标题栏**: 自定义 (`ExtendsContentIntoTitleBar = true`)。
* **左侧**: App Icon (24px) + "Limit" (Segoe UI Variable Display Semibold).
* **右侧**: 最小化 / 关闭 (标准 Windows 控件)。
* **高度**: 48px，作为拖拽区域。



### 2. 配色系统 (Color Palette) - 基于状态的动态色

UI 的主色调不再是固定的，而是跟随 **Fatigue Ring (疲劳环)** 的状态在全局发生微妙变化（例如按钮的 Hover 态、进度条颜色）。

| 状态 | 核心色 (Hex) | 语义 | 视觉感受 |
| --- | --- | --- | --- |
| **High Energy (0-40%)** | `#00B294` (Teal) | 专注、冷静 | 清新、锐利 |
| **Flow/Strain (40-70%)** | `#FFB900` (Amber) | 警示、温和 | 温暖、流动 |
| **Drain/Grind (70%+)** | `#E81123` (Coral Red) | 紧急、枯竭 | 强烈、不稳定 |
| **Care Mode (关怀)** | `#FF8C00` (Soft Orange) | 治愈 | 柔和发光 (Glow) |
| **Fragmented (碎片)** | `#888888` (Gray) | 无效消耗 | 沉闷、低对比度 |

### 3. 字体系统 (Typography)

* **主要字体**: `Segoe UI Variable Text` (Win11 标准)。
* **数据大字**: `Segoe UI Variable Display` (开启 `tnum` 等宽特性，防止数字跳动)。
* **代码/Cluster**: `Cascadia Code` (用于显示代码类应用的名称，增加极客感)。

---

## 二、 界面 1: Dashboard (主控制台)

**布局模式**: **Bento Grid (便当盒)**。
**交互原则**: View Only (只看)，Minimal Action (极简操作)。

### 布局网格

整个页面被划分为 **2列 x 2行** 的 Grid。

* **Zone A (Left)**: Row 0-1, Col 0 (占据左半边)。
* **Zone B (Top Right)**: Row 0, Col 1。
* **Zone C (Bottom Right)**: Row 1, Col 1。

---

### Zone A: The Core (精力反应堆)

这是 UI 的灵魂。

* **组件**:
* **ProgressRing (自定义)**: 一个巨大的圆环，线宽 12px，两端圆角 (`StrokeLineCap="Round"`).
* **Center Text**:
* Line 1: 状态词 (e.g., "Deep Focus") - 24pt Display Bold.
* Line 2: 建议词 (e.g., "High Logic Tasks") - 14pt Text Gray.


* **Care Indicator**: 圆环右上角悬浮一个小小的 `HeartIcon` 或 `BandageIcon`，带有 `DropShadow` (发光效果)，仅在 Care Mode 激活时显示。


* **动效 (Motion)**:
* **呼吸**: 圆环的 `Scale` (缩放) 在 0.98 到 1.02 之间做无限循环的正弦波动画 (`SineEaseInOut`)。
* **频率**: 疲劳值越低，呼吸越慢 (4s/cycle)；疲劳值越高，呼吸越急促 (1s/cycle)。


* **交互**:
* **Click**: 点击圆环中心 -> 弹出 `MenuFlyout`。
* 选项 1: "我觉得很累 (Calibrate: Tired)" -> 触发 Care Mode。
* 选项 2: "我刚休息过 (Reset)" -> 扣减疲劳值。





---

### Zone B: Context Monitor (上下文感知)

这是“纠偏”区域。

* **组件**:
* **Header**: 左上角小标题 "CURRENT CONTEXT"。
* **Main Content**:
* 大号图标 (48px) 显示当前 App (如 VS Code)。
* App 名称 + 所属 Cluster (如 "Coding Workflow")。


* **The Switch (纠偏开关)**:
* 一个自定义样式的 `ToggleSwitch`。
* **On (左)**: "Focusing" (绿色指示灯)。
* **Off (右)**: "Chilling" (灰色指示灯)。
* *用途*: 当你在看 Youtube 教程被误判为娱乐时，手动拨到 "Focusing"。




* **视觉细节**:
* 卡片背景使用 `AcrylicBrush`，比背景稍亮。



---

### Zone C: Drainers (消耗排行)

这是“警示”区域。

* **组件**:
* **Header**: "ENERGY DRAINERS"。
* **List**: 仅显示 Top 3 条目。
* **特殊条目 - Fragmented Time**:
* 如果检测到大量碎片时间，它会始终占据第一位。
* **样式**: 进度条颜色为灰色虚线 (`StrokeDashArray`)，看起来像“破碎”的感觉。
* **文案**: "Fragmented Attention (25m)"。




* **交互**:
* 点击整个 Zone C 卡片 -> 导航至 **Analytics** 页面。



---

## 三、 界面 2: Analytics (数据分析)

**布局模式**: 滚动视图 (ScrollView)。
**交互原则**: Exploration (探索)，Insight (洞察)。

### 1. 顶部: The Insight Banner

* **样式**: 纯文字，打字机效果。
* **内容**: "昨日你在下午 3 点遭遇了精力崩溃。建议今日提前处理难点。" (由简单的规则引擎生成)。

### 2. 图表区: The Heatmap (精力热力图)

* **控件**: 基于 Grid 的自定义绘制，或魔改 LiveCharts2。
* **X轴**: 0:00 - 24:00 (Hours)。
* **Y轴**: Date (最近 7 天)。
* **色块**:
* 透明/灰色: 空闲。
* 浅紫: 低疲劳工作。
* 深红: 高疲劳/过载 (Grind)。


* **交互**: Hover 任意色块，显示当时的 Top App 和 Cluster。

### 3. 详情区: Workflow Timeline (工作流时间轴)

* **设计**: 垂直时间轴。
* **特色**: 引入 **"Cluster Line"**。
* 如果 10:00-11:00 都在 Coding Cluster (VS -> Unity -> Chrome)，用一条**实线**把这些点连起来，左侧标注 "Focus Block: 1h"。
* 如果是碎片时间，则是断开的散点。


* **目的**: 鼓励用户把线“连起来”。

### 4. 老版图表: The Summary (总结)

* **设计**: 
* **内容**: 在这里保留2.0中的软件使用时间记录、疲劳度趋势、应用使用时长、等图表。可以先做收起，点击查看详细信息再展开。

---

## 四、 界面 3: Rules & Settings (规则配置)

**核心功能**: Cluster Editor (工作流簇编辑器)。

### The "Bucket" Interaction (桶交互)

不要用枯燥的列表。使用“拖拽分类”隐喻。

* **左侧**: **"Unassigned Apps" (未分类应用池)**。
* 一堆散落的 Tag (最近使用过的 App)。


* **右侧**: **"Clusters" (工作流簇)**。
* 几个框 (Buckets)："Coding", "Writing", "Meeting"。
* **交互**:
* 用户将左侧的 Tag **拖拽**进右侧的框里。
* 或者右键 Tag -> "Move to Coding"。




* **价值**: 让配置过程像整理桌面一样解压。

---

## 五、 关键交互流程 (Interaction Flows)

### 1. The Intervention (干预 - 强打扰)

当疲劳度 > 80% 触发。

* **UI**: **Overlay Window (全屏覆盖层)**。
* **背景**: 屏幕截图 + 高斯模糊 (Blur radius: 40) + 黑色遮罩 (Opacity 0.8)。
* **中心内容**:
* 文案: "Diminishing Returns." (收益递减)
* 副标题: "Your error rate is likely increasing." (你的错误率可能正在上升)


* **按钮**:
* 主按钮 (亮色): "Step Away" (离开) -> 锁屏或仅隐藏 Overlay。
* 副按钮 (幽灵按钮): "I Must Finish" (必须完成)。




* **The Friction (摩擦力设计)**:
* 点击 "I Must Finish" **不会立即生效**。
* 按钮上出现一个环形进度条，用户必须**按住不放 3 秒钟**。
* 这 3 秒钟是用来让用户冷静反思的。



### 2. The Morning Briefing (晨报)

当新的一天首次检测到活跃。

* **Step 1**: 系统托盘弹出 Toast: "Limit 已准备就绪。查看昨日简报？"
* **Step 2** (点击后): 屏幕中央弹出 **Modal Dialog (模态卡片)**。
* **动画**: 卡片从下往上滑入 (`SlideUp`).
* **内容**:
* 左侧: 昨日评分 (S/A/B/C)。
* 右侧: 关键数据 (深度工作时长 vs 碎片时长)。


* **底部**: "Start Today" 按钮。点击后卡片消失，软件静默进入后台。



---

## 六、 开发资源清单 (Assets List)

### 1. 图标 (Segoe Fluent Icons)

* `HeartFill` (Care Mode)
* `LightningBolt` (Energy)
* `Coffee` (Break)
* `Bandage` (Healing)
* `Processing` (Settings/Cluster)

### 2. 推荐 Nuget 包

* **WinUIEx**: 极大简化 WinUI 3 的窗口管理（去边框、托盘图标、始终置顶等）。
* **CommunityToolkit.WinUI.UI.Controls**: 获取 `DropShadowPanel` 等高级控件。
* **LiveChartsCore.SkiaSharpView.WinUI**: 绘制图表。

---

### 总结

**Next Step for Developer:**
可以考虑的方案：直接创建一个新的 WinUI 3 项目，安装 `WinUIEx`，然后按照“二、Dashboard”的布局，用 `Grid` 先把这三个 Zone 画出来。