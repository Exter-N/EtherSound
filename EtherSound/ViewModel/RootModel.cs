using EtherSound.Settings;
using EtherSound.View;
using Reactivity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;

namespace EtherSound.ViewModel
{
    class RootModel : ViewModel
    {
        static readonly IList<IKeyedRx<RootModel>> Properties = new List<IKeyedRx<RootModel>>();

        readonly RootSettings settings;

        public event EventHandler<SessionPropertyChangedEventArgs> SessionPropertyChanged;
        public event EventHandler<ChannelPropertyChangedEventArgs> ChannelPropertyChanged;

        public RootSettings Settings => settings;

        SessionModel[] sessions;
        static readonly IWritableKeyedRx<RootModel, SessionModel[]> SessionsProperty = Register(Properties, null, KeyedRx.Data(
            Storage<RootModel>.Create(key => key.sessions, (key, value) => { lock (key) key.sessions = value; }),
            key => new SessionModel[0],
            new CollectionEqualityComparer<SessionModel>()))
            .Watch((key, newSessions, oldSessions) =>
            {
                (ISet<SessionModel> removedSessions, ISet<SessionModel> keptSessions, ISet<SessionModel> addedSessions) = oldSessions.Diff(newSessions);
                foreach (SessionModel session in removedSessions)
                {
                    session.SettingsUpdated -= key.Session_SettingsUpdated;
                    session.ChannelPropertyChanged -= key.Session_ChannelPropertyChanged;
                    session.PropertyChanged -= key.Session_PropertyChanged;
                }
                foreach (SessionModel session in addedSessions)
                {
                    session.PropertyChanged += key.Session_PropertyChanged;
                    session.ChannelPropertyChanged += key.Session_ChannelPropertyChanged;
                    session.SettingsUpdated += key.Session_SettingsUpdated;
                }
                key.OnPropertyChanged(nameof(Sessions), (removedSessions, keptSessions, addedSessions));
            });

        public SessionModel[] Sessions
        {
            get => SessionsProperty[this];
            set => SessionsProperty[this] = value;
        }

        SessionModel[] validSessions;
        static readonly IKeyedRx<RootModel, SessionModel[]> ValidSessionsProperty = Register(Properties, nameof(ValidSessions), KeyedRx.Computed(
            SessionsProperty,
            Storage<RootModel>.Create(key => key.validSessions, (key, value) => key.validSessions = value),
            (key, sessions) => Array.FindAll(sessions, s => s.Valid),
            new CollectionEqualityComparer<SessionModel>()));

        public SessionModel[] ValidSessions => ValidSessionsProperty[this];

        SessionModel[] mixerSessions;
        static readonly IKeyedRx<RootModel, SessionModel[]> MixerSessionsProperty = Register(Properties, nameof(MixerSessions), KeyedRx.Computed(
            SessionsProperty,
            Storage<RootModel>.Create(key => key.mixerSessions, (key, value) => key.mixerSessions = value),
            (key, validSessions) =>
            {
                SessionModel[] mixerSessions = Array.FindAll(validSessions, s => s.ShowInMixer);
                Array.Reverse(mixerSessions);

                return mixerSessions;
            },
            new CollectionEqualityComparer<SessionModel>()))
            .Watch(key => MutedProperty.Update(key));

        public SessionModel[] MixerSessions => MixerSessionsProperty[this];

        SessionModel selectedSession;
        static readonly IWritableKeyedRx<RootModel, SessionModel> SelectedSessionProperty = Register(Properties, nameof(SelectedSession), KeyedRx.Data(
            Storage<RootModel>.Create(key => key.selectedSession, (key, value) => key.selectedSession = value),
            null));

        public SessionModel SelectedSession
        {
            get => SelectedSessionProperty[this];
            set => SelectedSessionProperty[this] = value;
        }

        bool canMoveSessionUp;
        static readonly IKeyedRx<RootModel, bool> CanMoveSessionUpProperty = Register(Properties, nameof(CanMoveSessionUp), KeyedRx.Computed(
            SessionsProperty, SelectedSessionProperty,
            Storage<RootModel>.Create(key => key.canMoveSessionUp, (key, value) => key.canMoveSessionUp = value),
            (key, sessions, selectedSession) => Array.IndexOf(sessions, selectedSession) > 0));

        public bool CanMoveSessionUp => CanMoveSessionUpProperty[this];

        bool canMoveSessionDown;
        static readonly IKeyedRx<RootModel, bool> CanMoveSessionDownProperty = Register(Properties, nameof(CanMoveSessionDown), KeyedRx.Computed(
            SessionsProperty, SelectedSessionProperty,
            Storage<RootModel>.Create(key => key.canMoveSessionDown, (key, value) => key.canMoveSessionDown = value),
            (key, sessions, selectedSession) =>
            {
                int i = Array.IndexOf(sessions, selectedSession);

                return i >= 0 && i < sessions.Length - 1;
            }));

        public bool CanMoveSessionDown => CanMoveSessionDownProperty[this];

        double mixerHeight;
        static readonly IKeyedRx<RootModel, double> MixerHeightProperty = Register(Properties, nameof(MixerHeight), KeyedRx.Computed(
            MixerSessionsProperty,
            Storage<RootModel>.Create(key => key.mixerHeight, (key, value) => key.mixerHeight = value),
            (key, mixerSessions) => 48 * mixerSessions.Length + 8));

        public double MixerHeight => MixerHeightProperty[this];

        float masterVolume;
        static readonly IKeyedRx<RootModel, float> MasterVolumeProperty = Register(Properties, nameof(MasterVolume), KeyedRx.Computed(
            MixerSessionsProperty,
            Storage<RootModel>.Create(key => key.masterVolume, (key, value) => key.masterVolume = value),
            (key, validSessions) =>
            {
                float masterVolume = 0.0f;
                foreach (SessionModel session in validSessions)
                {
                    if (!session.Muted)
                    {
                        masterVolume = Math.Max(masterVolume, session.MasterVolume);
                    }
                }

                return masterVolume;
            }));

        [WebSocketExposed]
        public double MasterVolume => MasterVolumeProperty[this];

        bool muted;
        static readonly IWritableKeyedRx<RootModel, bool> MutedProperty = Register(Properties, nameof(Muted), KeyedRx.TwoWayBound(
            Storage<RootModel>.Create(key => key.muted, (key, value) => key.muted = value),
            key => Array.TrueForAll(key.MixerSessions, session => session.Muted),
            (key, value) =>
            {
                if (value == key.muted)
                {
                    return;
                }
                if (value)
                {
                    foreach (SessionModel session in key.MixerSessions)
                    {
                        session.Settings.SavedMuted = session.Muted;
                        session.Muted = true;
                    }
                }
                else
                {
                    foreach (SessionModel session in key.MixerSessions)
                    {
                        session.Muted = session.Settings.SavedMuted;
                    }
                }
            }));

        [WebSocketExposed]
        public bool Muted
        {
            get => MutedProperty[this];
            set => MutedProperty[this] = value;
        }

        string version;
        static readonly IKeyedRx<RootModel, string> VersionProperty = Register(Properties, nameof(Version), KeyedRx.Computed(
            Storage<RootModel>.Create(key => key.version, (key, value) => key.version = value),
            key =>
            {
                string version = "v" + Program.Version;
                if (version.EndsWith(".0"))
                {
                    version = version.Substring(0, version.Length - 2);
                }

                return version;
            }));

        [WebSocketExposed]
        public string Version => VersionProperty[this];

        bool useDarkMainIcon;
        static readonly IKeyedRx<RootModel, bool> UseDarkMainIconProperty = Register(Properties, nameof(UseDarkMainIcon), KeyedRx.Computed(
            Storage<RootModel>.Create(key => key.useDarkMainIcon, (key, value) => key.useDarkMainIcon = value),
            key =>
            {
                if (CompositionHelper.DwmUseAccentColor)
                {
                    Color accentColor = CompositionHelper.DwmAccentColor;

                    return accentColor.R * 2 + accentColor.G * 5 + accentColor.B <= 1024;
                }
                else
                {
                    return false;
                }
            }));

        public bool UseDarkMainIcon => UseDarkMainIconProperty[this];

        public RootModel(RootSettings settings)
        {
            this.settings = settings;

            Initialize(this, Properties);
        }

        public SessionModel GetSession(int id)
        {
            return Array.Find(sessions, s => s.Id == id);
        }

        public bool HasSession(SessionModel session)
        {
            return Array.IndexOf(sessions, session) >= 0;
        }

        public void AddSession(SessionModel session)
        {
            int i = Array.IndexOf(sessions, session);
            if (i < 0)
            {
                SessionModel[] newSessions = new SessionModel[sessions.Length + 1];
                Array.Copy(sessions, newSessions, sessions.Length);
                newSessions[sessions.Length] = session;

                Sessions = newSessions;

                if (null == selectedSession)
                {
                    SelectedSession = session;
                }
            }
        }

        public void RemoveSession(SessionModel session)
        {
            int i = Array.IndexOf(sessions, session);
            if (i >= 0)
            {
                SessionModel[] newSessions = new SessionModel[sessions.Length - 1];
                Array.Copy(sessions, newSessions, i);
                Array.Copy(sessions, i + 1, newSessions, i, newSessions.Length - i);

                bool selectOther = session == selectedSession;

                Sessions = newSessions;

                if (selectOther)
                {
                    i = Math.Min(i, newSessions.Length - 1);
                    SelectedSession = (i >= 0) ? newSessions[i] : null;
                }
            }
        }

        public void SetSessionPosition(SessionModel session, int position, bool relative = false)
        {
            int i = Array.IndexOf(sessions, session);
            if (i < 0)
            {
                throw new InvalidOperationException();
            }

            if (relative)
            {
                position += i;
            }
            if (position < 0 || position >= sessions.Length)
            {
                throw new ArgumentOutOfRangeException("position");
            }
            if (position == i)
            {
                return;
            }

            SessionModel[] newSessions = new SessionModel[sessions.Length];
            Array.Copy(sessions, newSessions, Math.Min(i, position));
            newSessions[position] = sessions[i];
            if (position < i)
            {
                Array.Copy(sessions, position, newSessions, position + 1, i - position);
            }
            else
            {
                Array.Copy(sessions, i + 1, newSessions, i, position - i);
            }
            int after = Math.Max(i, position) + 1;
            Array.Copy(sessions, after, newSessions, after, sessions.Length - after);

            Sessions = newSessions;
        }

        void Session_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SessionModel session = (SessionModel)sender;
            switch (e.PropertyName)
            {
                case nameof(SessionModel.Valid):
                    ValidSessionsProperty.Update(this);
                    break;
                case nameof(SessionModel.ShowInMixer):
                    MixerSessionsProperty.Update(this);
                    break;
                case nameof(SessionModel.MasterVolume):
                    if (session.Valid)
                    {
                        MasterVolumeProperty.Update(this);
                    }
                    break;
                case nameof(SessionModel.Muted):
                    if (session.Valid)
                    {
                        MasterVolumeProperty.Update(this);
                        MutedProperty.Update(this);
                    }
                    break;
            }
            OnSessionPropertyChanged(new SessionPropertyChangedEventArgs(session, e));
        }

        void Session_ChannelPropertyChanged(object sender, ChannelPropertyChangedEventArgs e)
        {
            OnChannelPropertyChanged(e);
        }

        void Session_SettingsUpdated(object sender, EventArgs e)
        {
            settings.Dirty = true;
        }

        public void UpdateIcon()
        {
            UseDarkMainIconProperty.Update(this);
        }

        public void Poll()
        {
            foreach (SessionModel session in sessions)
            {
                session.Poll();
            }
        }

        public void PollTap()
        {
            SessionModel[] sessions;
            lock (this)
            {
                sessions = this.sessions;
            }
            foreach (SessionModel session in sessions)
            {
                session.PollTap();
            }
        }

        public void UpdateCursor()
        {
            foreach (SessionModel session in sessions)
            {
                session.UpdateCursor();
            }
        }

        protected override bool DoUpdateSettings()
        {
            bool anyChanged = false;

            foreach (SessionModel session in sessions)
            {
                anyChanged |= session.UpdateSettings();
            }

            SessionSettings[] sSettings = Array.ConvertAll(sessions, s => s.Settings);
            if (sSettings.Length != settings.Sessions.Count || sSettings.ExistsZip(settings.Sessions, (s1, s2) => s1 != s2))
            {
                settings.Sessions.Clear();
                settings.Sessions.AddRange(sSettings);
                anyChanged = true;
            }

            return anyChanged;
        }

        protected override void OnSettingsUpdated(EventArgs e)
        {
            settings.Dirty = true;
            base.OnSettingsUpdated(e);
        }

        protected virtual void OnSessionPropertyChanged(SessionPropertyChangedEventArgs e)
        {
            SessionPropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnChannelPropertyChanged(ChannelPropertyChangedEventArgs e)
        {
            ChannelPropertyChanged?.Invoke(this, e);
        }
    }
}
