using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CastorPlugin.UserControls
{
    public partial class LoadingIndicator : UserControl, INotifyPropertyChanged
    {
        private BitmapImage _currentImage;

        public BitmapImage CurrentImage
        {
            get => _currentImage;
            set
            {
                if (_currentImage != value)
                {
                    _currentImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public LoadingIndicator()
        {
            InitializeComponent();
            ShowDefaultImage();
            BaseImage.DataContext = this;
        }

        public void ShowSuccessImage()
        {
            CurrentImage = new BitmapImage(new Uri("pack://application:,,,/CastorPlugin;component/Resources/Images/dig-success.png"));
        }

        public void ShowDefaultImage()
        {
            CurrentImage = new BitmapImage(new Uri("pack://application:,,,/CastorPlugin;component/Resources/Images/base-digging.png"));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}