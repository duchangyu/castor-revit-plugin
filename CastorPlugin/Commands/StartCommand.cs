

using Autodesk.Revit.Attributes;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using CastorPlugin.Views.Pages;
using Nice3point.Revit.Toolkit.External;
using Revit.Async;

namespace CastorPlugin.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class StartCommand : ExternalCommand
{
    public override void Execute()
    {
        // Always initialize RevitTask ahead of time within Revit API context
        if (RevitApi.UiApplication == null)
        {
            RevitApi.UiApplication = UiApplication;
        }

        RevitTask.Initialize(RevitApi.UiApplication);

        Host.GetService<ICastorService>().Show<DashboardView>();



    }
}
