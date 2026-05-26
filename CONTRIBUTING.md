# 贡献指南

感谢您对本项目的兴趣！欢迎提交 Pull Request 或创建 Issue。

## 开发环境

### 前置条件
- Visual Studio 2019 或更高版本
- .NET Framework 4.8 Developer Pack
- Revit 2020-2024 (至少一个版本)
- WebView2 Runtime

### 环境搭建
1. 克隆仓库
2. 使用 `dotnet build --configuration "Debug R24"` 构建项目
3. 在 Visual Studio 中打开 `Castor.sln`
4. 选择 "Debug R24" 配置
5. 按 F5 启动 Revit 进行调试

## 分支管理

- `main` - 稳定版本
- `feature/*` - 新功能开发分支
- `fix/*` - 错误修复分支

## 代码规范

- 使用 C# 最新语法（项目配置为 `LangVersion=preview`）
- 遵循 MVVM 架构模式
- 所有公共 API 应有 XML 文档注释
- 提交前确保代码格式化一致

## Pull Request 流程

1. Fork 本仓库
2. 从 `main` 创建新分支 (`git checkout -b feature/my-feature`)
3. 进行开发并提交
4. 推送分支到您的 Fork
5. 创建 Pull Request
6. 等待代码审查和合并

## 提交信息规范

```
<type>: <subject>

<body>

footer
```

Type 类型:
- `feat`: 新功能
- `fix`: 错误修复
- `docs`: 文档更新
- `style`: 代码格式（不影响功能）
- `refactor`: 重构
- `test`: 测试相关
- `chore`: 构建/工具相关

## 问题反馈

- 请先搜索 [Issues](https://github.com/your-repo/CastorPlugin/issues) 是否已有相同问题
- 创建 Issue 时请提供详细的复现步骤
- 包含 Revit 版本、操作系统环境等信息

## 测试

调试时可以：
1. 使用 Revit 附加模块管理器进行热重载
2. 查看 `CastorPlugin.log` 日志文件
3. 使用 Visual Studio 输出窗口查看 Serilog 调试输出