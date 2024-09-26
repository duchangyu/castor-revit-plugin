using CastorPlugin.Core;
using CastorPlugin.ViewModels.Pages;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;
using Wpf.Ui.Controls;
using System.Windows.Controls;
using CastorPlugin.UserControls;
using System;

namespace CastorPlugin.Views.Pages
{
    /// <summary>
    /// DigView.xaml 的交互逻辑
    /// </summary>
    public partial class DigView : Page
    {

        public DigView(DigViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;

            // Initialize WebView2 when the page is loaded
            Loaded += DigView_Loaded;
        }

        public DigViewModel ViewModel { get; }

        private async void DigView_Loaded(object sender, RoutedEventArgs e)
        {
            //await InitializeWebView2();
            await ViewModel.UpdateDigCounts();
        }


        private void WebView_WebMessageReceived(object sender, WebMessageReceivedEventArgs e)
        {
            // 处理接收到的消息
            // 例如：
            // MessageBox.Show($"收到消息：{e.Message}，来源：{e.Source}");
        }
    }
}
