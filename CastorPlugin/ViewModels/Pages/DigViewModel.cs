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


        [ObservableProperty]
        private int assetCount;


        [RelayCommand]
        public async Task DigAsync()
        {

 

            var assetCount = await RevitTask.RunAsync<int>(app =>
            {
                try
                {
                    var count =  0;
                    digService.Dig();
                    return count;
                }
                catch (Exception ex)
                {
                    notificationService.ShowError("Exception", ex.Message);
                    return 0;
                }
            }); 


            this.AssetCount = assetCount;

        }







    }
}
