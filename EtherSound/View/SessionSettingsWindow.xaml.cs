using EtherSound.ViewModel;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
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
    }
}
