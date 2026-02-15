# EyeGuard Development Guide

This repository contains the source code for **EyeGuard**, a WinUI 3 desktop application built with .NET 8, designed to help users manage screen time and protect their eyes.

## 1. Build, Test & Run

### Prerequisites
- **.NET 8 SDK**
- **Visual Studio 2022** (17.8+) with standard workloads:
  - .NET Desktop Development
  - Windows App SDK / WinUI 3

### CLI Commands
Run these commands from the repository root:

- **Restore Dependencies:**
  ```bash
  dotnet restore src/EyeGuard.sln
  ```

- **Build Solution:**
  ```bash
  dotnet build src/EyeGuard.sln --configuration Debug
  ```

- **Run Application:**
  *Note: WinUI 3 apps often require packaging. Running via VS `F5` is recommended. For CLI:*
  ```bash
  dotnet run --project src/EyeGuard.UI/EyeGuard.UI.csproj
  ```

- **Run Tests:**
  *Currently, no unit tests exist. When added, use:*
  ```bash
  dotnet test src/EyeGuard.sln
  ```
  *Convention: Create `EyeGuard.Tests` project for unit tests.*

- **Linting & Formatting:**
  Enforce standard .NET coding styles:
  ```bash
  dotnet format src/EyeGuard.sln
  ```

## 2. Architecture & Code Structure

The solution strictly follows **Clean Architecture**:

### `src/EyeGuard.Core` (Domain Layer)
- **Purpose**: Central domain logic and definitions. **Zero external dependencies.**
- **Contents**:
  - `Entities/`: Database models (e.g., `AppUsageLog`).
  - `Interfaces/`: Contracts for services (e.g., `IWindowTracker`).
  - `Enums/`: Shared enumerations.

### `src/EyeGuard.Infrastructure` (Infrastructure Layer)
- **Purpose**: Implementation of Core interfaces. Depends on `Core` and `Application`.
- **Contents**:
  - `Services/`: Concrete implementations (e.g., `DatabaseService`, `UserActivityManager`).
  - `Native/`: P/Invoke calls and Win32 API wrappers.
  - `Monitors/`: Logic for tracking system state.

### `src/EyeGuard.Application` (Application Layer)
- **Purpose**: Orchestration logic (currently minimal/merged with Infra/UI in parts, but intended for use-cases).

### `src/EyeGuard.UI` (Presentation Layer)
- **Purpose**: The WinUI 3 entry point and user interface.
- **Contents**:
  - `ViewModels/`: MVVM logic using CommunityToolkit.
  - `Views/`: XAML pages and windows.
  - `Controls/`: Custom user controls.
  - `Assets/`: Images and resources.

## 3. Code Style & Conventions

### Formatting & Syntax
- **Braces**: Use **Allman Style** (opening brace on a new line).
- **Namespaces**: Use **File-scoped namespaces** (`namespace EyeGuard.UI.ViewModels;`).
- **Language**: C# 12 features are encouraged (e.g., primary constructors where clear).

### Naming Conventions
- **Classes/Public Members**: `PascalCase` (e.g., `UpdateStatisticsAsync`).
- **Private Fields**: `_camelCase` (e.g., `_timer`, `_windowTracker`).
- **Interfaces**: `I` prefix (e.g., `IDatabaseService`).
- **Async Methods**: Suffix with `Async` (except event handlers).

### Comments & Documentation
- **Language**: Code comments should primarily be in **Chinese** (Simplified).
- **XML Docs**: Required for public interfaces and complex logic.
  ```csharp
  /// <summary>
  /// 获取当前活动窗口的进程信息
  /// </summary>
  ```
- **Focus**: Explain *why* a complex block exists, not just *what* it does.

### MVVM Patterns (CommunityToolkit.Mvvm)
- Inherit ViewModels from `ObservableObject`.
- **Properties**: Use `[ObservableProperty]` to auto-generate `INotifyPropertyChanged` code.
  ```csharp
  [ObservableProperty]
  private string _statusMessage; // Generates StatusMessage property
  ```
- **Commands**: Use `[RelayCommand]` for actions.
  ```csharp
  [RelayCommand]
  private void SaveSettings() { ... }
  ```

### Dependency Injection
- Register services in `App.xaml.cs` (`ConfigureServices`).
- **Usage**:
  - **Constructor Injection**: Preferred method in ViewModels/Services.
  - **Service Locator**: Use `App.Services.GetRequiredService<T>()` *only* when constructor injection is impossible (e.g., static helpers, specific UI events).

## 4. Error Handling & Threading

### Async/Await
- **UI Updates**: Must happen on the UI thread.
  ```csharp
  App.MainWindow.DispatcherQueue.TryEnqueue(() => { MyCollection.Add(item); });
  ```
- **Background Work**: Use `Task.Run` for heavy computations/IO.
- **Fire-and-Forget**: Avoid `async void` except for event handlers. If `_ = Task.Run(...)` is needed, wrap the delegate in a `try-catch` block to prevent crashing the app.

### Database (SQLite)
- Use `SemaphoreSlim` for thread-safe database access in `DatabaseService`.
- Connection string and setup are handled in `Infrastructure`.

## 5. Development Workflow

1.  **Understand**: specific requirements. Identify affected layers.
2.  **Core First**: Define new Entities or Interfaces in `EyeGuard.Core`.
3.  **Infrastructure**: Implement the logic in `EyeGuard.Infrastructure`.
4.  **UI/ViewModel**:
    - Add ViewModel logic in `EyeGuard.UI/ViewModels`.
    - Create/Update XAML in `EyeGuard.UI/Views`.
    - Register new services in `App.xaml.cs`.
5.  **Verify**: Build the solution and manually verify the feature/fix.

## 6. Git Conventions

- **Commit Messages**: Use conventional commits (e.g., `feat: add dark mode`, `fix: crash on startup`).
- **Branches**: Create feature branches (e.g., `feature/dashboard-redesign`) from `main`.
