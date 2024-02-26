

using Autodesk.Revit.Attributes;
using CastorPlugin.Utils;
using CastorPlugin.ViewModels;
using CastorPlugin.Views;
using Nice3point.Revit.Toolkit.External;

namespace CastorPlugin.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class StartCommand : ExternalCommand
{
    public override void Execute()
    {
        //Host.GetService<ILookupService>().Show<DashboardView>();

        if (WindowController.Focus<WindowMain>()) return;

        //var viewModel = new WindowMainViewModel();
        var view = new WindowMain();
        WindowController.Show(view, UiApplication.MainWindowHandle);
    }
}