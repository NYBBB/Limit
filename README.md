# Limit 👁️

**Limit** is a modern Windows desktop application designed to help users manage screen time, monitor digital habits, and reduce eye strain through intelligent usage tracking and break reminders. Built with **.NET 8** and **WinUI 3**, it follows strict Clean Architecture principles to ensure scalability and maintainability.

![Dashboard Preview](assets/dashboard_preview.png)
*(Note: Add a screenshot of your dashboard here)*

## ✨ Key Features

- **Activity Tracking**: Automatically monitors active application usage and logs screen time with precision.
- **Fatigue Monitoring**: Uses intelligent algorithms to estimate user fatigue based on continuous usage patterns.
- **Usage Analytics**: Visualizes daily and hourly app usage trends to help you understand your digital habits.
- **Break Reminders**: Suggests timely breaks to prevent eye strain and improve productivity.
- **Modern UI**: A clean, responsive interface built with WinUI 3 and Fluent Design principles.
- **Privacy First**: All data is stored locally using SQLite. No data leaves your device.

## 🛠️ Technology Stack

- **Framework**: .NET 8
- **UI Framework**: Windows App SDK (WinUI 3)
- **Architecture**: Clean Architecture (Core, Infrastructure, Application, UI)
- **MVVM**: CommunityToolkit.Mvvm
- **Database**: SQLite (sqlite-net-pcl)
- **Charts**: LiveCharts2
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection

## 🚀 Getting Started

### Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/) (v17.8 or later)
- **Workloads**:
  - .NET Desktop Development
  - Windows App SDK / WinUI 3

### Installation

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/yourusername/Limit.git
    cd Limit
    ```

2.  **Restore dependencies**:
    ```bash
    dotnet restore src/EyeGuard.sln
    ```

3.  **Build the solution**:
    ```bash
    dotnet build src/EyeGuard.sln --configuration Debug
    ```

4.  **Run the application**:
    Open `src/EyeGuard.sln` in Visual Studio and press `F5`, or run:
    ```bash
    dotnet run --project src/EyeGuard.UI/EyeGuard.UI.csproj
    ```

## 🗺️ Roadmap & Progress

- [x] **Core Architecture**: Implementation of Clean Architecture layers.
- [x] **Database Integration**: Local SQLite database for usage logs.
- [x] **Usage Tracking Service**: Background monitoring of active windows.
- [x] **Basic Dashboard**: Visualization of daily usage stats.
- [ ] **Advanced Settings**: Customizable break intervals and strict mode.
- [ ] **Dark Mode Support**: Full theme switching capability.
- [ ] **Export Data**: Ability to export usage logs to CSV/JSON.

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1.  Fork the repository.
2.  Create a feature branch (`git checkout -b feature/AmazingFeature`).
3.  Commit your changes (`git commit -m 'Add some AmazingFeature'`).
4.  Push to the branch (`git push origin feature/AmazingFeature`).
5.  Open a Pull Request.

Please ensure your code follows the existing style guidelines (see `AGENTS.md` for details).

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

---

**Note**: This project is currently in active development. Features may change as we iterate towards version 1.0.
