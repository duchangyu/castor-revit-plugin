# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**CastorPlugin** is a Revit add-in built on .NET Framework 4.8 that provides a modern WPF UI experience for Revit users. It integrates with Revit's API and provides web-based functionality through WebView2, following MVVM architecture patterns.

## Architecture

### Core Components
- **Revit Add-in Entry**: `Application.cs` - Main Revit ExternalApplication implementation
- **Dependency Injection**: `Host.cs` - Microsoft.Extensions.Hosting based DI container setup
- **MVVM Structure**: Clean separation with Views, ViewModels, and Services
- **Web Integration**: WebView2-based web content integration with .NET-JavaScript interop

### Key Technologies
- **Target Framework**: .NET Framework 4.8
- **UI Framework**: WPF with WPF-UI (Modern WPF controls)
- **Revit API**: Nice3point.Revit.Toolkit for Revit 2020-2024 support
- **Web Integration**: Microsoft.Web.WebView2 for hybrid desktop-web functionality
- **MVVM**: CommunityToolkit.Mvvm for ViewModels
- **Async Revit**: Revit.Async for async/await Revit API operations
- **Logging**: Serilog with file and debug output
- **Build System**: NUKE build automation

### RevitApi Static Facade
All Revit API access goes through `Core/RevitApi.cs`:
```csharp
RevitApi.UiApplication   // UIApplication instance
RevitApi.UiDocument       // Active UIDocument
RevitApi.Document         // Active Document
RevitApi.ActiveView       // Current View
```
**Always check for null before use:**
```csharp
if (RevitApi.UiApplication is null) return;
if (RevitApi.UiDocument is null) return;
```

### Version-Specific Conditional Compilation
The project uses MSBuild conditionals for multi-version support:
- `R20`, `R21`, `R22`, `R23`, `R24` - version-specific symbols
- `R20_OR_GREATER`, `R22_OR_GREATER`, etc. - cumulative version ranges
```csharp
#if R22_OR_GREATER
    // Revit 2022+ only code
#endif
```

## Development Environment Setup

### Prerequisites
- Visual Studio 2019 or later
- .NET Framework 4.8 Developer Pack
- Revit 2020-2024 (multiple versions supported)
- WebView2 Runtime

### Build Commands

#### Using NUKE Build System
```bash
./build.cmd Compile                          # Build all Revit versions
./build.cmd Compile --configuration "Debug R24"  # Specific version
./build.cmd Clean
./build.cmd Bundle                           # Create installer
```

#### Using dotnet CLI
```bash
dotnet build --configuration "Debug R24"
dotnet build --configuration "Release R23"
```

#### Visual Studio
Open `Castor.sln` and select configuration (e.g., "Debug R24", "Release R23")

### Configuration Matrix
- Debug R20, R21, R22, R23, R24
- Release R20, R21, R22, R23, R24

## Project Structure

```
CastorPlugin/
├── Application.cs              # Revit ExternalApplication entry point
├── Host.cs                     # Dependency injection setup
├── RibbonController.cs         # Revit ribbon UI creation
├── CastorPlugin.addin          # Revit add-in manifest
├── Core/                       # Core business logic and Revit API wrappers
│   ├── RevitApi.cs             # Static facade for Revit API
│   └── ApiService.cs           # Family extraction business logic
├── Services/                   # Business services and contracts
├── ViewModels/                 # MVVM ViewModels
├── Views/                      # WPF Views and dialogs
├── UserControls/               # Reusable WPF controls
│   └── WebView2Control.xaml   # WebView2 wrapper with message handling
├── Commands/                   # Revit external commands
├── Config/                     # Configuration files
└── Utils/                      # Utility classes
```

### Key Services
- **CastorService**: Main service orchestrator for UI interactions
- **SettingsService**: Manages user settings and configuration
- **DigService**: Core business logic for document processing
- **SoftwareUpdateService**: Handles application updates
- **NotificationService**: User notifications and messages
- **WebServiceBroker**: HTTP communication with remote API

## Development Workflow

### Adding New Features
1. Create ViewModel in `ViewModels/Pages/`
2. Create corresponding View in `Views/Pages/`
3. Register services in `Host.cs`
4. Add navigation commands in relevant ViewModels
5. Update ribbon controller if needed

### Testing in Revit
1. Build project (select appropriate Revit version configuration)
2. Files are automatically copied to Revit add-ins folder via build targets
3. Launch Revit and find CastorPlugin in Add-ins tab
4. Use Add-in Manager for debugging if needed

### Configuration Files
- **Settings.cfg**: JSON configuration for API URLs and settings
- **CastorPlugin.addin**: Revit add-in registration manifest
- Build targets automatically handle deployment to Revit add-ins folder

### Adding a New Page
```csharp
// 1. Create ViewModel
public sealed partial class NewPageViewModel : ObservableObject
{
    // Implementation
}

// 2. Create View (WPF UserControl)
// 3. Register in Host.cs
builder.Services.AddScoped<NewPageView>();
builder.Services.AddScoped<NewPageViewModel>();
```

### WebView2 Integration
The `WebView2Control` (`UserControls/WebView2Control.xaml.cs`) provides:
- **Message passing**: `SendMessageToWebViewAsync(object)` sends JSON to web page
- **Message receiving**: `WebMessageReceived` event receives messages from web page
- **Script execution**: `ExecuteScriptAsync(string)` runs arbitrary JS

```csharp
// Send data to web
await webView.SendMessageToWebViewAsync(new { type = "update", data = "value" });

// Receive data from web
webView.WebMessageReceived += (s, e) => {
    var message = JsonSerializer.Deserialize<JsonElement>(e.Message);
};
```

Web pages communicate via `window.postMessage()` - no nativehost bridging needed.

### Debugging Tips
- Log files: `CastorPlugin.log` in output directory
- Debug output: Visual Studio Output window via Serilog.Debug sink
- Revit Add-in Manager for hot-reloading during development
- Revit journal files for additional debugging information

### Build Output
Binaries are output to:
- `bin/Debug R24/` or `bin/Release R24/` (per configuration)
- Automatically copied to: `%APPDATA%\Autodesk\Revit\Addins\[Version]\CastorPlugin\`
