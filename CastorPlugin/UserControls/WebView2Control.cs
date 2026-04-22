using System;
using System.Windows.Controls;

namespace CastorPlugin.UserControls
{
    /// <summary>
    /// WebView2Control的部分类定义，实现IDisposable接口
    /// </summary>
    public partial class WebView2Control : UserControl, IDisposable
    {
        /// <summary>
        /// 指示资源是否已释放
        /// </summary>
        private bool disposed = false;


        /// <summary>
        /// 释放托管和非托管资源
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    try
                    {
                        if (_fadeTokenSource != null)
                        {
                            _fadeTokenSource.Cancel();
                            _fadeTokenSource.Dispose();
                            _fadeTokenSource = null;
                        }

                        // 调用已有的清理方法
                        CleanupWebView2(markDisposed: true);
                    }
                    catch (Exception ex)
                    {
                        //// 记录异常但不抛出，确保Dispose总是能够完成
                        //if (Log != null)
                        //{
                        //    Log.Error($"在Dispose过程中发生异常: {ex.Message}");
                        //}
                    }
                }

                // 释放非托管资源（如有）

                disposed = true;
            }
        }

        /// <summary>
        /// 终结器
        /// </summary>
        ~WebView2Control()
        {
            Dispose(false);
        }
    }
}
