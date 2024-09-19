using CastorPlugin.Core;
using CastorPlugin.ViewModels.Pages;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;
using Wpf.Ui.Controls;
using System.Windows.Controls;

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
            DataContext = this;
        }

        public DigViewModel ViewModel { get; }

        private async void InitWebView2Env()
        {
            if (webView != null)
            {

                if (webView.CoreWebView2 == null)
                {

                    string tempPath = Path.GetTempPath();

                    var env = await CoreWebView2Environment.CreateAsync(userDataFolder: tempPath);
                    await webView.EnsureCoreWebView2Async(env);

                    webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                   

                }


                
            }
        }


        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // not to open link in new window
            e.NewWindow = (CoreWebView2)sender;
        }

        private  void Button_Click(object sender, RoutedEventArgs e)
        {
            if (webView != null)
            {

                if (webView.CoreWebView2 != null)
                {

                    webView.CoreWebView2.Navigate(addressBar.Text);
                }
                else
                {
                    webView.Source = new Uri(addressBar.Text);
                    //MessageBox.Show("not ready yet");
                }
            }

        }

        private void btnDigTest_Click(object sender, RoutedEventArgs e)
        {
            //RevitApi.ScanFamilies();
          
        }
    }
}
