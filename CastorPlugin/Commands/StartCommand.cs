

using Autodesk.Revit.Attributes;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using CastorPlugin.Utils;
using CastorPlugin.ViewModels;
using CastorPlugin.Views;
using CastorPlugin.Views.Pages;
using Nice3point.Revit.Toolkit.External;
using System.Windows;

namespace CastorPlugin.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class StartCommand : ExternalCommand
{
    public override void Execute()
    {

        if (WindowController.Focus<WindowMain>()) return;

        Host.GetService<IWindow>().Visibility = Visibility.Visible;

    }
}