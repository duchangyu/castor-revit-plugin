using System.Windows.Controls;
using CastorPlugin.ViewModels.Contracts;

namespace CastorPlugin.Views.Pages
{
    public partial class DashboardView : Page
    {
        public DashboardView(IDashboardViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        public IDashboardViewModel ViewModel { get; }
    }
}
