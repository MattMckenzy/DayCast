using DayCastClient.Properties;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Threading;

namespace DayCastClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Properties

        private double WindowWidth = 800;
        private double WindowHeight = 400;

        private NotifyIcon NotifyIcon = null;
        public CastInterface CastInterface { get; private set; } = new CastInterface();

        #endregion

        #region Factory Methods

        public MainWindow()
        {
            InitializeComponent();
            Width = WindowWidth;
            Height = WindowHeight;
        }

        #endregion

        #region Events

        private async void Window_Initialized(object sender, EventArgs e)
        {
            Visibility = Visibility.Hidden;
            NotifyIcon = new NotifyIcon();
            using (Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/DayCastClient;component/Resources/DayCast.ico")).Stream)
                NotifyIcon.Icon = new Icon(iconStream);
            
            NotifyIcon.ContextMenu = new ContextMenu(new MenuItem[]
            {
                new MenuItem("Exit", new EventHandler(Exit_OnClick))
            });

            NotifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            NotifyIcon.Visible = true;

            await CastInterface.RefreshAsync();

            CastInterface.PropertyChanged += CastInterface_PropertyChanged;

            if (!string.IsNullOrWhiteSpace(Settings.Default.PreferredDevice))
                CastInterface.SelectedReceiver = CastInterface.Receivers.FirstOrDefault(r => r.FriendlyName == Settings.Default.PreferredDevice);
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            System.Drawing.Point mousePoint = Control.MousePosition;
            bool FromLeft = mousePoint.X < SystemParameters.PrimaryScreenWidth / 2;
            bool FromUp = mousePoint.Y < SystemParameters.PrimaryScreenHeight / 2;

            if (FromLeft && FromUp)
            {
                Left = mousePoint.X;
                Top = mousePoint.Y;
            }
            else if (FromLeft && !FromUp)
            {
                Left = mousePoint.X;
                Top = mousePoint.Y - WindowHeight;
            }
            else if (!FromLeft && FromUp)
            {
                Left = mousePoint.X - WindowWidth;
                Top = mousePoint.Y;
            }
            else if (!FromLeft && !FromUp)
            {
                Left = mousePoint.X - WindowWidth;
                Top = mousePoint.Y - WindowHeight;
            }

            this.Visibility = Visibility.Visible;
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(delegate ()
                {
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                })
            );
        }
        
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            NotifyIcon.Visible = false;
            Settings.Default.Save();
            DayCastServer.TryStopServer();
        }
        
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Visibility = Visibility.Hidden;
                NotifyIcon.Visible = true;
            }
        }

        private void CastInterface_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CastInterface.SelectedReceiver))   
                Settings.Default.PreferredDevice = CastInterface.SelectedReceiver.FriendlyName;
        }

        private void Slider_MouseDown(object sender, RoutedEventArgs e)
        {
            CastInterface.SeekTimer.Stop();
        }

        private async void Slider_MouseUp(object sender, RoutedEventArgs e)
        {
            await CastInterface.SeekAsync();
        }

        private async void Volume_MouseUp(object sender, RoutedEventArgs e)
        {
            await CastInterface.SetVolumeAsync();
        }

        #endregion

        #region ContextMenu Clicks

        private void Exit_OnClick(object sender, EventArgs e) => this.Close();

        #endregion

        private async void QueueItemsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            await CastInterface.ChangeCurrentMediaAsync();
        }

        private async void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                await CastInterface.TryRefreshAsync();
            }
            catch { }
        }
    }

    public class DoubleSecondsToTimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (typeof(double) == value?.GetType())
                return TimeSpan.FromSeconds((double)value);
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (typeof(TimeSpan) == value?.GetType())
                return ((TimeSpan)value).TotalSeconds;
            else
                return 0;
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace((string)value))
                return Visibility.Hidden;
            else
                return Visibility.Visible;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVolumeIsMutedStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(bool)value)
                return System.Windows.Application.Current.MainWindow.Resources["VolumeImageVector"];
            else
                return System.Windows.Application.Current.MainWindow.Resources["VolumeMutedImageVector"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PlaybackRateToCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter != null && (double)value == double.Parse((string)parameter))
                return true;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}