# WebView2Control 使用指南

WebView2Control 是一个封装了 Microsoft Edge WebView2 控件的自定义 WPF 用户控件。它提供了安全的 Web 内容嵌入和与 WPF 应用程序的双向通信功能。

## 基本用法

在 XAML 中添加 WebView2Control：
```
<uc:WebView2Control
x:Name="webView"
Source="https://www.example.com"
WebMessageReceived="WebView_WebMessageReceived" />

```

在代码中处理 WebView2 的 WebMessageReceived 事件：
```
private void WebView_WebMessageReceived(object sender, WebMessageReceivedEventArgs e)
{
    // 处理从 WebView2 接收到的消息
    MessageBox.Show($"收到来自 {e.Source} 的消息：{e.Message}");
}
```

## 方法


## 执行 JavaScript

使用 `ExecuteScriptAsync` 方法在 WebView2 中执行 JavaScript 代码。

### 示例

```

// 修改页面背景颜色
await webView.ExecuteScriptAsync("document.body.style.backgroundColor = 'lightblue';");
// 在页面中插入新元素
await webView.ExecuteScriptAsync(@"
var newDiv = document.createElement('div');
newDiv.innerHTML = '这是通过 C# 插入的新元素';
document.body.appendChild(newDiv);
");
// 获取页面标题
string result = await webView.ExecuteScriptAsync("document.title");
MessageBox.Show($"页面标题: {result.Trim('"')}");

``` 

### ExecuteScriptAsync


## 发送消息到 Web 页面

使用 `SendMessageToWebViewAsync` 方法向 Web 页面发送消息。

### 示例

```
// 发送简单的字符串消息
await webView.SendMessageToWebViewAsync("Hello from C#!");
// 发送包含多个字段的对象
await webView.SendMessageToWebViewAsync(new
{
type = "userInfo",
name = "张三",
age = 30,
isVIP = true
});
// 发送数组数据
await webView.SendMessageToWebViewAsync(new
{
type = "updateList",
items = new[] { "项目1", "项目2", "项目3" }
});
```

### SendMessageToWebViewAsync

### 其他方法


## 处理来自 Web 页面的消息

在 XAML 中为 WebView2Control 添加 `WebMessageReceived` 事件处理程序，然后在代码后台实现该方法。

### 示例

```
private void WebView_WebMessageReceived(object sender, WebMessageReceivedEventArgs e)
{
try
{
var jsonMessage = JsonSerializer.Deserialize<JsonElement>(e.Message);
if (jsonMessage.TryGetProperty("type", out var typeElement))
{
switch (typeElement.GetString())
{
case "greeting":
if (jsonMessage.TryGetProperty("content", out var contentElement))
{
MessageBox.Show($"收到问候：{contentElement.GetString()}");
}
break;
case "getData":
// 响应 Web 页面的数据请求
var responseData = new { type = "dataResponse", data = new[] { "数据1", "数据2", "数据3" } };
webView.SendMessageToWebViewAsync(responseData);
break;
case "error":
if (jsonMessage.TryGetProperty("message", out var errorElement))
{
MessageBox.Show($"Web 页面报告错误：{errorElement.GetString()}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
}
break;
default:
MessageBox.Show($"收到未知类型的消息：{e.Message}");
break;
}
}
else
{
MessageBox.Show($"收到的消息格式不正确：{e.Message}");
}
}
catch (JsonException)
{
MessageBox.Show($"收到的消息不是有效的 JSON 格式：{e.Message}");
}
}
```


## 安全注意事项

- WebView2Control 包含基本的脚本净化功能，但在处理不受信任的输入时仍需谨慎。
- 始终验证从 Web 页面接收的消息的来源和内容。
- 避免执行来自不受信任源的 JavaScript 代码。
- 定期更新 WebView2 运行时以获取最新的安全补丁。

