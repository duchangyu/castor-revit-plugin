# Castor Plugin

Castor 是一个 Revit 插件，为 Revit 用户提供现代化的 WPF 界面体验。通过 WebView2 技术集成网页功能，支持族构件的提取、上传和"挖宝"功能，帮助用户将 Revit 族构件发布到 [BIMonChain](https://nft.bimonchain.com) 平台。

## 支持版本

- Revit 2020
- Revit 2021
- Revit 2022
- Revit 2023
- Revit 2024

## 功能特性

- **挖宝 (Dig)**: 自动提取 Revit 族构件信息，包括族名称、类型、参数、尺寸等，生成唯一指纹并上传到 BIMonChain 平台
- **仪表盘 (Dashboard)**: 统一管理已上传的构件，查看发布状态和登记信息
- **用户认证**: 支持登录 BIMonChain 账号，管理个人构件库
- **设置中心**: 配置插件偏好，包括主题、语言等选项
- **WebView2 集成**: 基于微软 WebView2 技术，提供流畅的网页交互体验

## 技术栈

| 组件 | 技术 |
|------|------|
| 目标框架 | .NET Framework 4.8 |
| UI 框架 | WPF + WPF-UI |
| 架构模式 | MVVM + 依赖注入 |
| Revit API | Nice3point.Revit.Toolkit |
| Web 集成 | Microsoft.Web.WebView2 |
| MVVM 框架 | CommunityToolkit.Mvvm |
| 异步 Revit | Revit.Async |
| 日志 | Serilog |

## 构建

### 环境要求

- Visual Studio 2019 或更高版本
- .NET Framework 4.8 Developer Pack
- Revit 2020-2024 (至少一个版本)
- WebView2 Runtime

### 使用 NUKE 构建系统

```bash
# 构建所有版本
./build.cmd Compile

# 构建特定版本
./build.cmd Compile --configuration "Debug R24"

# 清理
./build.cmd Clean

# 创建安装包
./build.cmd Bundle
```

### 使用 dotnet CLI

```bash
dotnet build --configuration "Debug R24"
dotnet build --configuration "Release R23"
```

### 使用 Visual Studio

1. 打开 `Castor.sln`
2. 选择目标版本配置（如 "Debug R24"）
3. 生成解决方案

## 安装

1. 构建项目，输出文件会自动复制到 Revit 插件目录：
   ```
   %APPDATA%\Autodesk\Revit\Addins\[Version]\CastorPlugin\
   ```
2. 启动 Revit，在"附加模块"选项卡中找到 Castor
3. 首次使用需要登录 BIMonChain 账号

## 项目结构

```
CastorPlugin/
├── Application.cs              # Revit 外部应用入口
├── Host.cs                     # 依赖注入配置
├── RibbonController.cs         # Revit 功能区创建
├── CastorPlugin.addin         # Revit 插件清单
├── Core/                       # 核心业务逻辑和 Revit API 封装
│   ├── RevitApi.cs            # Revit API 静态门面
│   └── ApiService.cs          # 族提取业务逻辑
├── Services/                   # 业务服务接口和实现
├── ViewModels/                 # MVVM ViewModels
├── Views/                      # WPF Views 和对话框
├── UserControls/               # 可复用 WPF 控件
│   └── WebView2Control.xaml   # WebView2 封装
├── Commands/                   # Revit 外部命令
├── Config/                     # 配置文件
└── Utils/                      # 工具类
```

## 开发说明

### 添加新页面

1. 在 `ViewModels/Pages/` 创建 ViewModel
2. 在 `Views/Pages/` 创建对应的 View
3. 在 `Host.cs` 中注册服务
4. 在相关 ViewModel 中添加导航命令

### WebView2 消息通信

```csharp
// 发送数据到网页
await webView.SendMessageToWebViewAsync(new { type = "update", data = "value" });

// 接收来自网页的消息
webView.WebMessageReceived += (s, e) => {
    var message = JsonSerializer.Deserialize<JsonElement>(e.Message);
};
```

网页通过 `window.postMessage()` 进行通信。

### 调试

- 日志文件: `CastorPlugin.log` 在输出目录
- 调试输出: Visual Studio 输出窗口
- Revit 附加模块管理器用于热重载调试

## 依赖

- [Nice3point.Revit.Toolkit](https://github.com/Nice3point/RevitToolkit) - MIT License
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MIT License
- [Microsoft.Web.WebView2](https://github.com/MicrosoftEdge/WebView2) - MIT License
- [Revit.Async](https://github.com/Nice3point/RevitAsync) - MIT License
- [Serilog](https://serilog.net/) - Apache-2.0 License

## 许可证

本项目采用 [MIT License](LICENSE)。

## 关于 BIMonChain

[BIMonChain](https://nft.bimonchain.com) 是一个基于区块链的 BIM 构件管理平台，帮助用户登记、追溯和共享 Revit 族构件。