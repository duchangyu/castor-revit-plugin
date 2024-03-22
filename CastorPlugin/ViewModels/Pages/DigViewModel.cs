using CastorPlugin.Core;
using CastorPlugin.Services;
using CastorPlugin.Services.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nice3point.Revit.Toolkit.External.Handlers;
using Revit.Async;
using Wpf.Ui;

namespace CastorPlugin.ViewModels.Pages
{
    public sealed partial class DigViewModel(
      IDigService digService,
      INavigationService navigationService,
      NotificationService notificationService,
      IServiceProvider serviceProvider)
      : ObservableObject
    {


      

        [RelayCommand]
        public  void Dig()
        {

 

            RevitTask.RunAsync(app =>
            {
                try
                {
                    digService.Dig();
                }
                catch (Exception ex)
                {
                    notificationService.ShowError("Exception", ex.Message);
                }
            });
          

        }

        public event EventHandler DigEventHandler;


    

    }
}
