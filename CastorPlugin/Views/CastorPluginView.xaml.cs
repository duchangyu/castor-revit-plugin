using CastorPlugin.Core;
using CastorPlugin.ViewModels;

namespace CastorPlugin.Views
{
    public partial class CastorPluginView
    {

        public CastorPluginView(CastorPluginViewModel viewModel)
        {

            InitializeComponent();
            DataContext = viewModel;
          
        }

      
      
        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {


        }
    }
}