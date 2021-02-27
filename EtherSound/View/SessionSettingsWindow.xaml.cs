using EtherSound.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WASCap;

namespace EtherSound.View
{
    /// <summary>
    /// Logique d'interaction pour SessionSettingsWindow.xaml
    /// </summary>
    public partial class SessionSettingsWindow : Window
    {
        readonly SessionSettingsModel model;

        public KeyValuePair<DataFlow, string>[] SourceFlows => new KeyValuePair<DataFlow, string>[]
        {
            new KeyValuePair<DataFlow, string>(DataFlow.Render, Convert.ToString(TryFindResource(DataFlow.Render) ?? DataFlow.Render)),
            new KeyValuePair<DataFlow, string>(DataFlow.Capture, Convert.ToString(TryFindResource(DataFlow.Capture) ?? DataFlow.Capture)),
        };

        internal SessionSettingsWindow(SessionSettingsModel model)
        {
            this.model = model;
            InitializeComponent();
            DataContext = model;
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
    }
}
