using EtherSound.ViewModel;
using System.Windows;

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
