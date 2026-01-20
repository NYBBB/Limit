# EyeGuard 项目进度总览
> 最后更新: 2026-01-19

## ✅ 已完成功能

### 第一阶段：架构与核心服务
- [x] 项目架构搭建 (Clean Architecture)
  - [x] `EyeGuard.Core` - 核心业务层
  - [x] `EyeGuard.Infrastructure` - 基础设施层
  - [x] `EyeGuard.Application` - 应用层
  - [x] `EyeGuard.UI` - WinUI 3 界面层

- [x] **F1.1 输入检测** (`GlobalInputMonitor.cs`)
  - [x] 全局键盘/鼠标钩子
  - [x] 空闲状态检测

- [x] **F1.2 媒体检测** (`AudioDetector.cs`)
  - [x] 音频播放检测
  - [x] 媒体豁免模式

- [x] **F2.1 疲劳算法** (`FatigueEngine.cs`)
  - [x] 非线性疲劳曲线
  - [x] 实时疲劳值计算

- [x] **用户状态机** (`UserActivityManager.cs`)
  - [x] Active/MediaMode/Idle/Away 状态

- [x] **数据持久化**
  - [x] `DatabaseService.cs` - SQLite 集成
  - [x] `UsageCollectorService.cs` - 使用数据收集
  - [x] `SettingsService.cs` - 设置保存/加载 (JSON)

---

### 第二阶段：浏览器网站记录
- [x] `WebsiteRecognizer.cs` - 网站识别引擎
  - [x] 内置 100+ 网站规则库
  - [x] 标题关键词匹配逻辑
  - [x] 支持国际+国产浏览器

---

### 第三阶段：UI 层实现
- [x] **主窗口** (`MainWindow.xaml`)
  - [x] NavigationView 导航框架
  - [x] Mica 背景材质

- [x] **仪表盘** (`DashboardPage.xaml`)
  - [x] 疲劳值进度条
  - [x] 今日工作时长/最长连续时间
  - [x] 疲劳趋势图表 (LiveCharts2)
  - [x] 应用使用列表 + 图标显示
  - [x] 展开/收起功能 (Show More/Less)
  - [x] 二三级折叠 (浏览器 → 网站 → 页面标题)
  - [x] "今日使用"和"疲劳趋势"分离面板
  - [x] 开发者调试面板 (DEBUG模式)

- [x] **规则设置** (`RulesPage.xaml`)
  - [x] 简单/智能模式切换
  - [x] 疲劳度阈值设置
  - [x] 空闲判定时间设置
  - [x] 休息规则配置

- [x] **分析页面** (`AnalyticsPage.xaml`) - 骨架
- [x] **通用设置** (`SettingsPage.xaml`) - 骨架

- [x] **休息遮罩** (`BreakOverlayWindow.xaml`)
  - [x] 全屏遮罩 + 模糊效果
  - [x] 倒计时功能
  - [x] Skip/Snooze 按钮
  - [x] 自动触发

- [x] **系统托盘** (`TrayIconService.cs`)
  - [x] 托盘图标
  - [x] 基本菜单

- [x] **UI 辅助**
  - [x] `IconMapper.cs` - 40+ 应用图标 + 60+ 网站图标映射
  - [x] `BoolConverters.cs` - 布尔值转换器

---

## 🔜 未完成功能

### 近期任务：疲劳趋势与分析系统
- [x] **P0 疲劳快照持久化**
  - [x] 创建 `FatigueSnapshot` 实体
  - [x] 每隔 N 秒保存疲劳值
  - [x] 智能归零逻辑（跨天重置）
  - [x] 设置界面配置项
  
- [x] **P0 Dashboard 疲劳趋势加载**
  - [x] 启动时加载今日快照
  - [x] 填充折线图历史数据点
  
- [x] **P1 每小时使用记录**
  - [x] 创建 `HourlyUsageRecord` 实体
  - [x] 按小时聚合应用使用时长

- [x] **P1 Analytics 历史查看**
  - [x] 日期选择器（快捷按钮+自定义日期）
  - [x] 加载历史疲劳趋势
  - [x] 历史应用使用柱状图切换
  
- [x] **P2 Analytics 柱状图**
  - [x] 24小时 × Top N 应用分布图
  - [x] 堆叠柱状图（Top 4 + 其他）

### 中期任务
- [ ] 分析页面完整实现 (F3)
  - [ ] 每周/每月使用时长趋势图
  - [ ] 应用使用时间分布饼图
  - [ ] 休息完成率统计
  - [ ] 最常用应用 Top 10

- [ ] 设置页面增强
  - [ ] 网站记录开关
  - [ ] 自定义规则编辑器

### 远期任务
- [ ] 完善系统托盘
  - [ ] 托盘悬停显示今日概览
  - [ ] 完整托盘菜单 (开始/暂停/设置/退出)

- [ ] 窗口追踪增强 (F1.3)
  - [ ] 应用图标提取 (非手动映射)
  - [ ] 网站 Favicon 获取

- [ ] 多语言支持
- [ ] 端到端功能测试
- [ ] 性能优化与打包发布

---

## 📋 设计想法 (待评估)

### 休息任务清单模式 (Break Tasks)
- **核心概念**: 任务化休息。当疲劳度高时生成"休息任务"（如：休息5分钟）。
- **交互流程**: 执行休息 -> 点击完成 -> 算法结算。
- **动态结算**: 结合任务类型和当前疲劳值，计算疲劳减少量，赋予休息"成就感"。

### 核心算法进化 (Algorithm Evolution)
- [ ] **加权疲劳贡献 (Weighted Fatigue Contribution)**
  - 区分"耗能应用"(代码/设计, 系数1.0)与"低耗能应用"(视频/阅读, 系数0.3)。
  - 报表逻辑修正：不仅仅记录时长，更要记录"产生的疲劳值"。
- [ ] **枯竭预测 (Burnout Forecast)**
  - Dashboard 动态倒计时："按当前强度，42分钟后精力耗尽"。
  - 建议提示："切换到媒体模式可延长至 1小时15分"。

### 分析页面深度设计 (Analytics PRO)
#### 1. 全景仪表盘 (Overview) - 精力的流向
- **精力消耗环形图 (Energy Pie)**: 按"产生的疲劳值"而非时间分布（如：工作占80%疲劳，但时间仅占40%）。
- **"垃圾时间"统计 (The Grind)**: 统计疲劳值 > 80% 时的强行工作时长。

#### 2. 趋势深度分析 (Trends) - 寻找规律
- **疲劳速率热力图**: 类似 GitHub Contribution，颜色深浅代表"疲劳增长速度"（寻找压力巅峰时段）。
- **恢复质量分析/有效休息**: 散点图对比"休息时长"vs"疲劳下降幅度"，统计将疲劳真正降至20%以下的"有效休息"。

#### 3. 应用画像 (App Profiling) - 谁是罪魁祸首
- **ROI 矩阵 (性价比)**: X轴投入时间 vs Y轴产生疲劳。区分"杀时间不累人"与"高耗能"应用。
- **专注碎片化指数**: 统计窗口切换频率（如 Chrome 45秒/次 vs VSCode 15分钟/次）。

#### 4. 浏览器内网站使用分析 (Browser Insights)
- **功能构想**: 在柱状图中展示浏览器内前几个最常用网站的使用时长。
- **实现思路**: 利用已有的 `HourlyUsageRecord` 和网站识别数据，聚合浏览器内各网站使用时长。
- **展示形式**: 点击浏览器柱子时，弹出或展开显示该小时内访问的 Top 3 网站。

---

## 📁 关键文件索引

| 类别 | 文件 | 说明 |
|------|------|------|
| 需求文档 | `SRS.md` | 软件需求规格 |
| 核心服务 | `src/EyeGuard.Infrastructure/Services/` | 8个核心服务 |
| UI 页面 | `src/EyeGuard.UI/Views/` | 5个页面+1个窗口 |
| 图标映射 | `src/EyeGuard.UI/Services/IconMapper.cs` | 应用和网站图标 |
| 网站识别 | `src/EyeGuard.Infrastructure/Services/WebsiteRecognizer.cs` | 100+ 规则 |
