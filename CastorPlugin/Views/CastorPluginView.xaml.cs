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
    }
}