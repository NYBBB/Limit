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

## ⚠️ 待修复问题

### 高优先级
1. **TestOverlayButton 空实现**
   - 测试模式按钮 (Start/Reset/Test Overlay) 未生效
   - 需要验证 Timer 初始化和事件绑定

2. **状态文本更新问题**
   - "准备就绪" 状态可能不更新

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
  
- [ ] **P1 每小时使用记录**
  - [ ] 创建 `HourlyUsageRecord` 实体
  - [ ] 按小时聚合应用使用时长

- [ ] **P1 Analytics 历史查看**
  - [ ] 日期选择器
  - [ ] 加载历史疲劳趋势
  
- [ ] **P2 Analytics 柱状图**
  - [ ] 24小时 × Top N 应用分布图

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

### 休息任务清单模式
- 将休息建议设计成可交互任务列表
- 用户点击"已完成"立即减少疲劳度
- 记录休息完成情况用于分析

### 特殊模式
- **超级工作模式**: 忽略所有疲劳度提醒
- **娱乐模式**: 强制娱乐，禁止打开工作软件
- **强制工作模式**: 必须完成工作时长，禁止娱乐

### AI 集成 (远期)
- AI 进度督导
- 内置 Todo List 与专注时间绑定

---

## 📁 关键文件索引

| 类别 | 文件 | 说明 |
|------|------|------|
| 需求文档 | `SRS.md` | 软件需求规格 |
| 核心服务 | `src/EyeGuard.Infrastructure/Services/` | 8个核心服务 |
| UI 页面 | `src/EyeGuard.UI/Views/` | 5个页面+1个窗口 |
| 图标映射 | `src/EyeGuard.UI/Services/IconMapper.cs` | 应用和网站图标 |
| 网站识别 | `src/EyeGuard.Infrastructure/Services/WebsiteRecognizer.cs` | 100+ 规则 |
