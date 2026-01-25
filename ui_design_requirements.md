# EyeGuard 前端 UI 设计需求文档

> **目的**：本文档作为前端页面设计的完整参考，定义了所有页面的布局、数据、交互和视觉规范。
> 
> **使用流程**：设计师根据本文档创建 UI 设计 → 开发者根据设计实现 Vue 组件 → 通过 Bridge 对接后端数据

---

## 📐 技术要求

### 技术栈
- **框架**: Vue 3（Composition API）
- **样式**: TailwindCSS（已配置设计系统颜色）
- **图表**: ECharts
- **动画**: GSAP
- **字体**: 
  - 标题: Outfit（Sans-serif，现代几何感）
  - 正文: Manrope（高可读性）
  - 代码/数据: JetBrains Mono

### 设计约束
- **窗口尺寸**: 1920x1080（可缩放，但布局以此为基准）
- **最小宽度**: 1280px
- **配色**: 深色模式（Dark Mode）为主
- **响应式**: 暂不需要移动端适配

---

## 🎨 设计系统

### 颜色规范（已定义在 TailwindCSS）

#### 背景色
```
bg-deep: #050507       // 主背景（深空黑）
bg-surface: #0a0a0f    // 卡片表面
bg-glass: rgba(20, 20, 30, 0.4)  // 玻璃面板
```

#### 强调色（根据疲劳状态动态变化）
```
accent-cyan: #00f0ff    // 青色 - 精力充沛 (0-40%)
accent-amber: #ffaa00   // 琥珀色 - 疲劳警告 (40-70%)
accent-red: #ff2a2a     // 红色 - 严重疲劳 (70%+)
accent-purple: #bc13fe  // 紫色 - 创意/洞察
care: #FF8C00           // 橙色 - 关怀模式
```

#### 功能色
```
energy: #00B294    // 高精力
flow: #FFB900      // 流动/紧张
drain: #E81123     // 枯竭
```

### 组件样式

#### 玻璃卡片 (`.glass-card`)
```css
background: rgba(20, 20, 30, 0.4);
backdrop-filter: blur(20px) saturate(180%);
border: 1px solid rgba(255, 255, 255, 0.08);
border-radius: 24px;
box-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.3);
```

#### 主按钮 (`.btn-primary`)
- 背景：`accent-cyan` 半透明 (20%)
- 边框：`accent-cyan` (30%)
- 悬停：背景 30%，边框 50%
- 点击：`scale(0.95)`

#### 次要按钮 (`.btn-secondary`)
- 背景：白色 5%
- 文字：白色 70%
- 悬停：背景 10%，文字 100%

---

## 📱 页面设计需求

### 1. Dashboard（仪表盘 - 今日）

**目标**：一目了然查看当前疲劳状态、活动时长和消耗排行。

#### 布局结构（Bento Grid）

```
┌─────────────────────────────────────────┐
│  [顶部导航栏 - 固定]                   │
├────────────┬────────────────────────────┤
│            │  Zone B: 上下文感知        │
│  Zone A:   ├────────────────────────────┤
│  精力      │  Zone C: 消耗排行          │
│  反应堆    │                            │
│            │                            │
└────────────┴────────────────────────────┘
```

**CSS Grid 配置**：
```css
grid-template-columns: 320px 1fr;
grid-template-rows: auto 1fr;
gap: 24px;
padding: 32px;
```

---

#### Zone A: 精力反应堆（FatigueRing）

**组件名**: `FatigueRing.vue`

**功能**：显示当前疲劳值，呼吸动画，状态文字。

**数据需求**（从 C# Bridge 接收）：
```typescript
interface FatigueData {
  value: number           // 疲劳值 0-100
  state: string          // 状态: FRESH/FOCUSED/FLOW/STRAIN/DRAIN/CARE
  color: string          // 当前状态颜色 (Hex)
  breathRate: number     // 呼吸速率 1.5-4 秒/周期
  isCareMode: boolean    // 是否关怀模式
}
```

**视觉设计**：
- **圆环**：
  - 直径：240px
  - 线宽：12px
  - 描边：`color`（动态）
  - 进度：`value / 100`（顺时针填充）
  - 端点：圆角 (`stroke-linecap="round"`)
  
- **呼吸动画**：
  - 使用 GSAP 实现 `scale` 0.98 → 1.02 无限循环
  - 周期：`breathRate` 秒
  - 缓动：`ease-in-out`

- **中心内容**：
  - 行1：疲劳值 `42.5%`（粗体，32px）
  - 行2：状态文字 `专注中`（16px，半透明）
  - 颜色：跟随 `color`

- **Care 指示器**：
  - 当 `isCareMode = true` 时，右上角显示 ❤️ 图标
  - 带光晕效果 (`drop-shadow`)

**交互**：
- 点击圆环中心 → 弹出菜单：
  - "我觉得很累" → Bridge 发送 `CALIBRATE_FATIGUE {mode: 'tired'}`
  - "我刚休息过" → Bridge 发送 `CALIBRATE_FATIGUE {mode: 'rested'}`

---

#### Zone B: 上下文感知（ContextCard）

**组件名**: `ContextCard.vue`

**功能**：显示当前正在使用的应用、所属工作流簇、连续使用时长。

**数据需求**：
```typescript
interface ContextData {
  appName: string          // 进程名 "msedge.exe"
  displayName: string      // 显示名 "Edge 浏览器"
  cluster: string          // 簇类型 "coding"/"writing"/"media" 等
  clusterName: string      // 簇显示名 "编程开发"
  sessionDuration: number  // 当前应用使用时长（秒）
  isFocusing: boolean      // 用户是否手动标记为专注
}
```

**视觉设计**：
- **顶部**：小标题 "当前上下文" (12px，半透明)
- **应用图标**：48px × 48px（如果有）
- **应用名称**：`displayName`（20px，粗体）
- **簇标签**：
  - 文字：`clusterName`（如 "编程开发"）
  - 背景：根据 `cluster` 类型显示不同颜色：
    - coding → 蓝色
    - writing → 绿色
    - media → 紫色
    - social → 橙色

- **时长显示**：`sessionDuration` 格式化为 "连续使用 25分钟"

- **Focusing 开关**（未来功能，暂时隐藏）：
  - ToggleSwitch："专注中" / "休闲中"
  - 状态：`isFocusing`

**交互**：暂无

---

#### Zone C: 消耗排行（DrainersCard）

**组件名**: `DrainersCard.vue`

**功能**：显示今日消耗疲劳值最多的前 5 个应用。

**数据需求**：
```typescript
interface DrainerItem {
  id: string
  appName: string
  displayName: string     // "Visual Studio Code"
  duration: number        // 使用时长（秒）
  percentage: number      // 占比 0-100
  cluster: string         // 所属簇
  isFragmented?: boolean  // 是否为碎片时间
}

interface DrainersData {
  items: DrainerItem[]           // Top 5
  totalDuration: number          // 今日总时长
  fragmentedDuration: number     // 碎片时间总计
}
```

**视觉设计**：
- **标题**："能量消耗排行" (16px，粗体)
- **列表**：显示前 5 项
  - 每项包含：
    - 应用名：`displayName`
    - 时长：`duration` 格式化（"2小时 15分钟"）
    - 进度条：
      - 宽度：`percentage`%
      - 颜色：根据 `cluster` 类型
      - 特殊：如果 `isFragmented=true`，使用虚线样式 + 灰色

**交互**：
- 点击整个卡片 → 导航到 Analytics 页面

---

### 2. Analytics（分析页面）

**目标**：深度分析疲劳趋势、应用使用模式、时间分布。

#### 布局结构（垂直滚动）

```
┌────────────────────────────────────┐
│  Insight Banner（洞察横幅）       │
├────────────────────────────────────┤
│  疲劳趋势图（折线图）             │
├────────────────────────────────────┤
│  24小时应用使用热力图             │
├────────────────────────────────────┤
│  应用使用时长统计（柱状图）       │
└────────────────────────────────────┘
```

---

#### Insight Banner（洞察横幅）

**组件名**: `InsightBanner.vue`

**功能**：显示 AI 生成的洞察文本（如"今日下午3点疲劳峰值，建议提前处理难点"）。

**数据需求**：
```typescript
interface InsightData {
  text: string       // 洞察文本
  type: string       // "warning" | "info" | "success"
  icon?: string      // 可选图标
}
```

**视觉设计**：
- 浮动药丸形状，居中
- 背景：半透明，根据 `type` 显示不同颜色边框
- 图标 + 文字
- 入场动画：从上往下滑入

---

#### 疲劳趋势图

**组件名**: `FatigueTrendChart.vue`

**功能**：显示今日疲劳值随时间变化的曲线。

**数据需求**：
```typescript
interface TrendPoint {
  timestamp: number   // Unix 时间戳
  value: number       // 疲劳值 0-100
}

interface TrendData {
  points: TrendPoint[]
}
```

**图表配置（ECharts）**：
- X轴：时间（0:00 - 24:00）
- Y轴：疲劳值（0 - 100%）
- 曲线：
  - 平滑曲线 (`smooth: true`)
  - 渐变填充（底部透明 → 顶部半透明）
  - 颜色：根据疲劳值区间动态变化（绿 → 黄 → 红）
- Tooltip：显示时间点和疲劳值

---

#### 24小时应用使用热力图

**组件名**: `HourlyUsageChart.vue`

**功能**：显示每小时使用的 Top 4 应用及"其他"分类。

**数据需求**：
```typescript
interface HourlyApp {
  appName: string
  displayName: string
  duration: number   // 秒
  color: string      // 柱状颜色
}

interface HourlyData {
  hour: number       // 0-23
  apps: HourlyApp[]  // Top 4 + "其他"
}

interface UsageChartData {
  hours: HourlyData[]  // 24 小时
}
```

**图表配置（ECharts）**：
- 类型：堆叠柱状图
- X轴：0-23 时
- Y轴：使用时长（分钟）
- 每个柱子：Top 4 应用 + "其他"堆叠显示
- Tooltip：显示应用名和时长

---

#### 应用使用时长统计

**组件名**: `AppUsageChart.vue`

**功能**：显示今日所有应用的使用时长排行（可展开/折叠详细）。

**数据需求**：
```typescript
interface AppUsage {
  appName: string
  displayName: string
  totalDuration: number  // 秒
  sessions: number       // 会话次数
  avgDuration: number    // 平均会话时长
}

interface AppUsageData {
  apps: AppUsage[]
}
```

**视觉设计**：
- 横向条形图（ECharts）
- 显示 Top 10
- "显示更多" 按钮展开完整列表

---

### 3. Rules（规则配置页面）

**目标**：管理应用分类（Cluster）和疲劳计算规则。

#### 功能模块

##### 3.1 Cluster 编辑器（拖拽分类）

**组件名**: `ClusterEditor.vue`

**功能**：将应用拖拽到不同的工作流簇（如"编程"、"写作"、"娱乐"）。

**数据需求**：
```typescript
interface Cluster {
  id: number
  name: string           // "编程开发"
  color: string          // 簇颜色
  apps: string[]         // 应用列表 ["vscode.exe", "unity.exe"]
  isSystemPreset: boolean
}

interface UnassignedApp {
  processName: string
  displayName: string
}

interface ClusterData {
  clusters: Cluster[]
  unassignedApps: UnassignedApp[]
}
```

**视觉设计**：
- **左侧**：未分类应用池
  - 应用以标签形式显示
  - 可拖拽

- **右侧**：簇容器（3-5个）
  - 每个簇是一个"桶"
  - 显示簇名称、颜色标记、包含的应用列表
  - 可接收拖拽

**交互**：
- 拖拽应用到簇 → Bridge 发送 `UPDATE_CLUSTER`
- 右键应用 → "移到编程" / "移除" 菜单

---

##### 3.2 疲劳规则设置（简化版）

**组件名**: `FatigueRulesPanel.vue`

**功能**：调整疲劳计算的敏感度。

**数据需求**：
```typescript
interface FatigueRules {
  sensitivityLevel: "low" | "medium" | "high"  // 敏感度级别
  idleThreshold: number      // 空闲判定时间（秒）
  awayThreshold: number      // 离开判定时间（秒）
}
```

**视觉设计**：
- 滑块：低 ←→ 中 ←→ 高
- 数值输入框：空闲时间、离开时间
- 实时预览：调整后显示预估疲劳增长速率

---

### 4. Settings（设置页面）

**目标**：应用的通用设置（通知、开机启动、数据管理）。

#### 功能模块

##### 4.1 通知设置

**数据需求**：
```typescript
interface NotificationSettings {
  enableNotifications: boolean    // 启用通知
  enableSoundAlerts: boolean      // 声音提示
  interventionMode: number        // 0=礼貌, 1=平衡, 2=强制
}
```

**视觉设计**：
- 开关：启用通知、声音提示
- 单选按钮组：干预模式（礼貌/平衡/强制）

---

##### 4.2 系统设置

**数据需求**：
```typescript
interface SystemSettings {
  autoStartOnBoot: boolean        // 开机自启
  ecoModeEnabled: boolean         // 省电模式
  showTrayIcon: boolean           // 显示托盘图标
}
```

---

##### 4.3 数据管理

**功能**：
- 清除历史数据（按钮）
- 导出数据（按钮）
- 重置所有设置（危险按钮）

**交互**：
- 点击"清除数据" → 确认对话框 → Bridge 发送 `CLEAR_DATA`

---

## 🔌 Bridge 数据接口总览

### C# → JS（后端推送数据）

| 消息类型 | 更新频率 | 用途 |
|---------|---------|------|
| `FATIGUE_UPDATE` | 每秒 | Dashboard 疲劳圆环 |
| `CONTEXT_UPDATE` | 应用切换时 | Dashboard 上下文卡片 |
| `DRAINERS_UPDATE` | 每分钟 | Dashboard 消耗排行 |
| `TREND_UPDATE` | 每分钟 | Analytics 趋势图 |
| `HEATMAP_UPDATE` | 每小时 | Analytics 热力图 |
| `SETTINGS_UPDATE` | 设置更改时 | Settings 页面同步 |

### JS → C#（前端发送命令）

| 命令 | 参数 | 用途 |
|-----|------|------|
| `CALIBRATE_FATIGUE` | `{mode: 'tired'/'fresh'/'rested'}` | 手动校准疲劳值 |
| `UPDATE_CLUSTER` | `{appName, clusterId}` | 更新应用分类 |
| `UPDATE_SETTINGS` | `{...settings}` | 保存设置 |
| `NAVIGATE` | `{view: 'dashboard'/'analytics'/...}` | 页面导航 |
| `REQUEST_REFRESH` | - | 请求全量数据刷新 |

---

## 📝 实现优先级

### P0（核心功能 - 必须实现）
1. Dashboard 页面完整实现（Zone A/B/C）
2. FatigueRing 呼吸动画和数据更新
3. Bridge 数据对接（疲劳值、上下文、消耗排行）

### P1（重要功能）
1. Analytics 疲劳趋势图
2. Settings 页面基础设置
3. 24小时热力图

### P2（增强功能）
1. Cluster 拖拽编辑器
2. Insight Banner
3. 数据导出功能

---

## 🎯 设计交付物

请提供以下设计文件：

1. **高保真原型**（Figma/Sketch）
   - Dashboard 完整页面
   - Analytics 完整页面（含滚动状态）
   - Rules/Settings 页面
   
2. **组件规范**
   - FatigueRing（各状态变体）
   - 玻璃卡片样式细节
   - 按钮 Hover/Active 状态
   
3. **动效说明**
   - 呼吸动画参考（GIF/视频）
   - 页面切换过渡建议
   
4. **响应式断点**（可选）
   - 1920x1080（主要）
   - 1280x720（最小支持）

---

## ❓ 待设计师决策的问题

1. **顶部导航栏**：是否需要重新设计？目前是临时占位
2. **图标风格**：Line/Duotone/Filled？推荐使用 Phosphor Icons
3. **数据空状态**：当没有数据时如何展示？
4. **加载状态**：Skeleton 还是 Spinner？
5. **错误提示**：Toast 通知还是 Modal 对话框？

请在设计时一并考虑这些细节。设计完成后，将文件和说明发给我，我会落地到项目中！
