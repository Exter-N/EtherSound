using EtherSound.View.Converters;
using EtherSound.ViewModel;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;

namespace EtherSound.View
{
    class TrayIcon : IDisposable
    {
        readonly RootModel model;

        readonly NotifyIcon trayIcon;
        readonly ToolStripMenuItem muteItem;

        public event EventHandler VolumeControlClicked;
        public event EventHandler SettingsClicked;
        public event EventHandler ResetClicked;

        public TrayIcon(RootModel model)
        {
            this.model = model;
            model.PropertyChanged += UpdateTray;

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            ToolStripMenuItem vcItem = new ToolStripMenuItem("Définir le volume", null, delegate
            {
                OnVolumeControlClicked(EventArgs.Empty);
            });
            vcItem.Font = new Font(vcItem.Font, vcItem.Font.Style | FontStyle.Bold);
            trayMenu.Items.Add(vcItem);
            muteItem = new ToolStripMenuItem("Tout mettre en sourdine", null, delegate
            {
                model.Muted = !model.Muted;
            });
            UpdateMuteItemChecked();
            trayMenu.Items.Add(muteItem);
            trayMenu.Items.Add(new ToolStripMenuItem("Basculer depuis/vers son local", null, delegate
            {
                foreach (SessionModel session in model.Sessions)
                {
                    if (session.Valid && session.ShowInMixer && session.CanSwap)
                    {
                        session.Muted = !session.Muted;
                    }
                }
                LocalSound.ToggleMute();
            }));
            trayMenu.Items.Add(new ToolStripMenuItem("Réinitialiser", null, delegate
            {
                OnResetClicked(EventArgs.Empty);
            }));
            trayMenu.Items.Add(new ToolStripMenuItem("Paramètres", null, delegate
            {
                OnSettingsClicked(EventArgs.Empty);
            }));
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(new ToolStripMenuItem("Arrêter EtherSound", null, delegate
            {
                if (MessageBox.Show("Voulez-vous vraiment arrêter EtherSound ?", "EtherSound", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    Application.ExitThread();
                }
            }));

            SystemEvents.UserPreferenceChanged += delegate
            {
                UpdateTrayIconIcon();
                model.UpdateIcon();
            };

            trayIcon = new NotifyIcon();
            trayIcon.MouseClick += (sender, e) =>
            {
                switch (e.Button)
                {
                    case MouseButtons.Left:
                        OnVolumeControlClicked(EventArgs.Empty);
                        break;
                    case MouseButtons.Middle:
                        model.Muted = !model.Muted;
                        break;
                }
            };
            trayIcon.ContextMenuStrip = trayMenu;
            UpdateTrayIconIcon();
            UpdateTrayIconText();
            trayIcon.Visible = true;
        }

        public void Dispose()
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            model.PropertyChanged -= UpdateTray;
        }

        protected virtual void OnVolumeControlClicked(EventArgs e)
        {
            VolumeControlClicked?.Invoke(this, e);
        }

        protected virtual void OnSettingsClicked(EventArgs e)
        {
            SettingsClicked?.Invoke(this, e);
        }

        protected virtual void OnResetClicked(EventArgs e)
        {
            ResetClicked?.Invoke(this, e);
        }

        void UpdateTray(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(model.MasterVolume):
                    UpdateTrayIconIcon();
                    UpdateTrayIconText();
                    break;
                case nameof(model.Muted):
                    UpdateMuteItemChecked();
                    UpdateTrayIconIcon();
                    UpdateTrayIconText();
                    break;
            }
        }

        void UpdateTrayIconIcon()
        {
            trayIcon.Icon = VolumeIconConverter.Convert<Icon>(model.Muted, model.MasterVolume);
        }

        void UpdateTrayIconText()
        {
            string displayMasterVolume = model.Muted ? "0 %" : RoundedPercentageConverter.Convert(model.MasterVolume, "%");
            trayIcon.Text = (displayMasterVolume == "0 %") ? "EtherSound : muet" : string.Format("EtherSound : {0}", displayMasterVolume);
        }

        void UpdateMuteItemChecked()
        {
            muteItem.Checked = model.Muted;
        }
    }
}
