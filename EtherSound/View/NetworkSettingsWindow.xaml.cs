using EtherSound.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace EtherSound.View
{
    /// <summary>
    /// Logique d'interaction pour NetworkSettingsWindow.xaml
    /// </summary>
    public partial class NetworkSettingsWindow : Window
    {
        readonly NetworkSettingsModel model;

        internal NetworkSettingsWindow(NetworkSettingsModel model)
        {
            this.model = model;
            InitializeComponent();
            DataContext = model;
            WebSocketPreSharedSecret.Password = model.WebSocketPreSharedSecret;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                model.UpdateSettings();
                DialogResult = true;
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            base.OnPreviewKeyDown(e);
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            model.UpdateSettings();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            model.UpdateSettings();
        }

        private void WebSocketPreSharedSecret_PasswordChanged(object sender, RoutedEventArgs e)
        {
            model.WebSocketPreSharedSecret = WebSocketPreSharedSecret.Password;
        }
    }
}
