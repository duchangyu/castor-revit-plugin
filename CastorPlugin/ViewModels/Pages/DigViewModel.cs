using CastorPlugin.Core;
using CastorPlugin.Services;
using CastorPlugin.ViewModels.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui;

namespace CastorPlugin.ViewModels.Pages
{
    public sealed partial class DigViewModel(
      INavigationService navigationService,
      NotificationService notificationService,
      IServiceProvider serviceProvider)
      : ObservableObject
    {


        [RelayCommand]
        public  void Dig()
        {
             RevitApi.ScanFamilies();
        }


       

    }
}
