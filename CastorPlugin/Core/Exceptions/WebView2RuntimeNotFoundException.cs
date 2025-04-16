using System;

namespace CastorPlugin.Core.Exceptions
{
    /// <summary>
    /// 表示 WebView2 运行时未安装时抛出的异常
    /// </summary>
    public class WebView2RuntimeNotFoundException : Exception
    {
        /// <summary>
        /// 初始化 WebView2RuntimeNotFoundException 类的新实例
        /// </summary>
        public WebView2RuntimeNotFoundException()
            : base("WebView2 运行时未安装。请安装最新版本的 WebView2 运行时。")
        {
        }

        /// <summary>
        /// 使用指定的错误消息初始化 WebView2RuntimeNotFoundException 类的新实例
        /// </summary>
        /// <param name="message">描述异常的消息</param>
        public WebView2RuntimeNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 WebView2RuntimeNotFoundException 类的新实例
        /// </summary>
        /// <param name="message">描述异常的消息</param>
        /// <param name="innerException">导致当前异常的异常</param>
        public WebView2RuntimeNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
} 