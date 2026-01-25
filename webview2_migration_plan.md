# WebView2 架构迁移实施计划 (Migration Implementation Plan)

> **目标**：将 EyeGuard UI 从纯 WinUI 3 迁移到 **WinUI 3 (Shell) + WebView2 (Content)** 混合架构，以解除 UI 渲染限制，实现极致视觉体验。

---

## 🏗️ 核心架构定义

### 1. 技术栈选择 (Tech Stack)

#### App Shell (Windows)
- **Framework**: WinUI 3 (Windows App SDK)
- **Role**: 窗口管理、系统托盘、进程监控、疲劳算法核心、数据持久化
- **Features**: 无边框窗口 (Custom Window Chrome)、亚克力背景透传 (Mica Alt)

#### Frontend (UI Content)
- **Framework**: **Vue 3** (轻量、高性能、易上手)
- **Build Tool**: **Vite** (极速 HMR，开发体验极佳)
- **Language**: TypeScript (类型安全)
- **Styling**: **TailwindCSS** (快速构建现代 UI，配合 Headless UI)
- **Charts**: **ECharts** 或 **Chart.js** (丰富的数据可视化)
- **Animations**: **GSAP** (专业级动画) 或 **Lottie-web**

### 2. 目录结构 (Directory Structure)

```
src/
├── EyeGuard.Core/           # 核心业务逻辑 (不变)
├── EyeGuard.Infrastructure/ # 基础设施 (不变)
└── EyeGuard.UI/
    ├── ClientApp/           # [NEW] 前端工程 (Vue3 + Vite)
    │   ├── src/
    │   │   ├── components/  # UI 组件 (FatigueRing, ContextCard...)
    │   │   ├── views/       # 页面 (Dashboard, Analytics...)
    │   │   ├── assets/      # 静态资源
    │   │   └── bridge/      # JS 端 Bridge 封装
    │   ├── index.html
    │   └── package.json
    ├── Bridge/              # [NEW] C# 端 Bridge 实现
    │   ├── MessageHandler.cs
    │   └── BridgeEvents.cs
    ├── Assets/
    │   └── WebRoot/         # 编译后的前端资源 (Production)
    ├── MainWindow.xaml      # 承载 WebView2
    └── App.xaml
```

---

## 🌉 JS-C# 通信机制 (The Bridge)

采用 **双向消息总线 (Bi-directional Message Bus)** 模式，而非直接的对象调用，以降低耦合。

### 1. 消息协议 (Message Protocol)

```json
/* C# -> JS (Event/State Update) */
{
  "type": "FATIGUE_UPDATE",
  "payload": {
    "value": 45.5,
    "status": "FOCUSED",
    "color": "#13c8ec"
  }
}

/* JS -> C# (Action/Command) */
{
  "action": "START_FOCUS_SESSION",
  "data": {
    "durationMinutes": 25,
    "taskName": "Deep Work"
  }
}
```

### 2. 实现方式

- **C# 发送**: `webView.CoreWebView2.PostWebMessageAsJson(jsonString)`
- **JS 接收**: `window.chrome.webview.addEventListener('message', handler)`
- **JS 发送**: `window.chrome.webview.postMessage(jsonObject)`
- **C# 接收**: `webView.CoreWebView2.WebMessageReceived += OnMessageReceived`

---

## 📅 迁移路线图 (Roadmap)

### Phase 1: 基础设施搭建 (The Foundation)
1. **初始化前端工程**: 在 `EyeGuard.UI/ClientApp` 创建 Vue3+Vite+Tailwind 项目。
2. **WinUI Shell 改造**: 清空 `DashboardPage3.xaml`，替换为全屏 `WebView2` 控件。
3. **Bridge 通道打通**: 实现基础的 "Ping-Pong" 通信测试。
4. **Dev 环境配置**: 配置 Debug 模式下 WebView2 加载 `http://localhost:5173` (Vite Server)，Release 模式加载本地文件。

### Phase 2: 核心组件迁移 (Core Visuals)
1. **FatigueRing 重制**: 使用 SVG + CSS/GSAP 重写呼吸圆环。实现更细腻的呼吸更随疲劳值变色。
2. **Dashboard 布局**: 使用 CSS Grid 实现 Bento Grid 布局 (Zone A/B/C)。
3. **VM 对接**: 将 `DashboardViewModel3` 的属性更新改为发送 Bridge 消息。

### Phase 3: 数据可视化与交互 (Analytics & Interaction)
1. **TopDrainers**: 使用 HTML/CSS 进度条（带动画）。
2. **Timeline/Charts**: 引入 ECharts 实现 24h 热力图和趋势图。
3. **交互迁移**: 迁移设置页面、托盘菜单交互。

### Phase 4: 视觉打磨 (Polishing)
1. **Glassmorphism**: 实现背景模糊 (Backdrop Filter) 和透视效果。
2. **Micro-interactions**: 鼠标悬停光效、卡片点击反馈。
3. **Release 打包**: 配置 CI/CD 将前端构建产物复制到 WinUI 输出目录。

---

## ⚠️ 关键注意事项

1. **内存管理**: 前端需注意及时销毁 Chart 实例和定时器，避免内存泄漏。
2. **性能优化**: 使用 Virtual List 处理长列表；避免过高频率的 Bridge 通信（如每帧更新），必要时使用 `requestAnimationFrame` 在前端插值。
3. **安全性**: 禁用 WebView2 的通用访问权限，仅允许特定的 Bridge 通信。
