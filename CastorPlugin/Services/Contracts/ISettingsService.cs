using System.ComponentModel;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CastorPlugin.Services.Contracts
{
    public interface ISettingsService : INotifyPropertyChanged
    {

        //User interface
        ApplicationTheme Theme { get; set; }
        WindowBackdropType Background { get; set; }
        int TransitionDuration { get; }

        //Window
        bool UseSizeRestoring { get; set; }
        double WindowWidth { get; set; }
        double WindowHeight { get; set; }
        bool IsLoading { get; set; }

        int ApplyTransition(bool value);
        void Save();

        string ApiUrl { get; set; }
    }
}
