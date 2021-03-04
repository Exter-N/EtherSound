using EtherSound.Settings;
using Reactivity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using WASCap;

namespace EtherSound.ViewModel
{
    class SessionModel : ViewModel
    {
        private const int MonitorCapacity = 6;

        static readonly IList<IKeyedRx<SessionModel>> Properties = new List<IKeyedRx<SessionModel>>();

        static int nextId = 0;

        readonly int id;
        readonly SessionSettings settings;
        readonly ControlStructure ctlS;
        readonly BufferConsole console;

        readonly float[] monitorBuffer;
        int monitorCursor;

        Stream tapStream;
        EventHandler<TapDataEventArgs> tapData;

        public event EventHandler<ChannelPropertyChangedEventArgs> ChannelPropertyChanged;
        public event EventHandler<TapDataEventArgs> TapData
        {
            add
            {
                if (null == value)
                {
                    return;
                }
                lock (this)
                {
                    if (null == tapData && null == tapStream)
                    {
                        tapStream = ctlS.OpenTapStream();
                    }
                    tapData += value;
                }
                Program.Dispatcher.Invoke(() => WithTapStreamProperty.Update(this));
            }
            remove
            {
                if (null == value)
                {
                    return;
                }
                lock (this)
                {
                    tapData -= value;
                    if (null == tapData && null != tapStream)
                    {
                        tapStream.Close();
                        tapStream = null;
                    }
                }
                Program.Dispatcher.Invoke(() => WithTapStreamProperty.Update(this));
            }
        }

        [WebSocketExposed]
        public Guid PersistentId => settings.Id;

        public int Id => id;

        public SessionSettings Settings => settings;

        public ControlStructure ControlStructure => ctlS;

        string sourceName;
        static readonly IWritableKeyedRx<SessionModel, string> SourceNameProperty = Register(Properties, nameof(SourceName), KeyedRx.Data(
            Storage<SessionModel>.Create(key => key.sourceName, (key, value) => key.sourceName = value),
            null));

        [WebSocketExposed(Writable = false)]
        public string SourceName
        {
            get => SourceNameProperty[this];
            set => SourceNameProperty[this] = value;
        }

        bool withWASSink;
        static readonly IWritableKeyedRx<SessionModel, bool> WithWASSinkProperty = Register(Properties, nameof(WithWASSink), KeyedRx.Data(
            Storage<SessionModel>.Create(key => key.withWASSink, (key, value) => key.withWASSink = value),
            null));

        [WebSocketExposed(Writable = false)]
        public bool WithWASSink
        {
            get => WithWASSinkProperty[this];
            set => WithWASSinkProperty[this] = value;
        }

        bool withNetworkSink;
        static readonly IWritableKeyedRx<SessionModel, bool> WithNetworkSinkProperty = Register(Properties, nameof(WithNetworkSink), KeyedRx.Data(
            Storage<SessionModel>.Create(key => key.withNetworkSink, (key, value) => key.withNetworkSink = value),
            null));

        [WebSocketExposed(Writable = false)]
        public bool WithNetworkSink
        {
            get => WithNetworkSinkProperty[this];
            set => WithNetworkSinkProperty[this] = value;
        }

        bool withTapStream;
        static readonly IKeyedRx<SessionModel, bool> WithTapStreamProperty = Register(Properties, nameof(WithTapStream), KeyedRx.Computed(
            Storage<SessionModel>.Create(key => key.withTapStream, (key, value) => key.withTapStream = value),
            key =>
            {
                lock (key)
                {
                    return null != key.tapStream;
                }
            }));

        [WebSocketExposed]
        public bool WithTapStream => WithTapStreamProperty[this];

        string customName;
        static readonly IWritableKeyedRx<SessionModel, string> CustomNameProperty = Register(Properties, nameof(CustomName), KeyedRx.Data(
            Storage<SessionModel>.Create(key => key.customName, (key, value) => key.customName = value),
            key => key.settings.CustomName));

        [WebSocketExposed]
        public string CustomName
        {
            get => CustomNameProperty[this];
            set => CustomNameProperty[this] = value;
        }

        string name;
        static readonly IKeyedRx<SessionModel, string> NameProperty = Register(Properties, nameof(Name), KeyedRx.Computed(
            SourceNameProperty, CustomNameProperty,
            Storage<SessionModel>.Create(key => key.name, (key, value) => key.name = value),
            (key, sourceName, customName) => ((string.IsNullOrWhiteSpace(customName) ? sourceName : customName) ?? string.Empty).Trim()));

        [WebSocketExposed]
        public string Name => NameProperty[this];

        bool valid;
        static readonly IWritableKeyedRx<SessionModel, bool> ValidProperty = Register(Properties, nameof(Valid), KeyedRx.Data(
            Storage<SessionModel>.Create(key => key.valid, (key, value) => key.valid = value),
            key => false));

        [WebSocketExposed(Writable = false)]
        public bool Valid
        {
            get => ValidProperty[this];
            set => ValidProperty[this] = value;
        }

        bool showInMixer;
        static readonly IWritableKeyedRx<SessionModel, bool> ShowInMixerProperty = Register(Properties, nameof(ShowInMixer), KeyedRx.TwoWayBound(
            Storage<SessionModel>.Create(key => key.showInMixer, (key, value) => key.showInMixer = value),
            key => key.settings.ShowInMixer,
            (key, value) => key.settings.ShowInMixer = value));

        [WebSocketExposed]
        public bool ShowInMixer
        {
            get => ShowInMixerProperty[this];
            set => ShowInMixerProperty[this] = value;
        }

        Color color;
        static readonly IWritableKeyedRx<SessionModel, Color> ColorProperty = Register(Properties, nameof(Color), KeyedRx.Data(
            Storage<SessionModel>.Create(key => key.color, (key, value) => key.color = value),
            key => key.settings.Color));

        public Color Color
        {
            get => ColorProperty[this];
            set => ColorProperty[this] = value;
        }

        int colorRgb;
        static readonly IWritableKeyedRx<SessionModel, int> ColorRgbProperty = Register(Properties, nameof(Color), KeyedRx.TwoWayBound(
            ColorProperty,
            (key => key.colorRgb, (key, value) => key.colorRgb = value),
            color => (color.R << 16) | (color.G << 8) | color.B,
            (rgb, oldColor) => unchecked(Color.FromArgb(oldColor.A, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb))));

        [WebSocketExposed(Name = "Color")]
        public int ColorRgb
        {
            get => ColorRgbProperty[this];
            set => ColorRgbProperty[this] = value;
        }

        bool canSwap;
        static readonly IWritableKeyedRx<SessionModel, bool> CanSwapProperty = Register(Properties, nameof(CanSwap), KeyedRx.Data(
            Storage<SessionModel>.Create(key => key.canSwap, (key, value) => key.canSwap = value),
            key => false));

        public bool CanSwap
        {
            get => CanSwapProperty[this];
            set => CanSwapProperty[this] = value;
        }

        float masterVolume;
        static readonly IWritableKeyedRx<SessionModel, float> MasterVolumeProperty = Register(Properties, nameof(MasterVolume), KeyedRx.TwoWayBound(
            Storage<SessionModel>.Create(key => key.masterVolume, (key, value) => key.masterVolume = value),
            key => key.ctlS.MasterVolume / key.MaxMasterVolume,
            (key, value) => key.ctlS.MasterVolume = value * key.MaxMasterVolume));

        [WebSocketExposed]
        public float MasterVolume
        {
            get => MasterVolumeProperty[this];
            set => MasterVolumeProperty[this] = value;
        }

        bool muted;
        static readonly IWritableKeyedRx<SessionModel, bool> MutedProperty = Register(Properties, nameof(Muted), KeyedRx.TwoWayBound(
            Storage<SessionModel>.Create(key => key.muted, (key, value) => key.muted = value),
            key => !key.ctlS.Enabled,
            (key, value) => key.ctlS.Enabled = !value));

        [WebSocketExposed]
        public bool Muted
        {
            get => MutedProperty[this];
            set => MutedProperty[this] = value;
        }

        int sampleRate;
        static readonly IKeyedRx<SessionModel, int> SampleRateProperty = Register(Properties, nameof(SampleRate), KeyedRx.Computed(
            Storage<SessionModel>.Create(key => key.sampleRate, (key, value) => key.sampleRate = value),
            key => key.ctlS.SampleRate));

        [WebSocketExposed]
        public int SampleRate => SampleRateProperty[this];

        Channel channelMask;
        static readonly IKeyedRx<SessionModel, Channel> ChannelMaskProperty = Register(Properties, nameof(ChannelMask), KeyedRx.Computed(
            Storage<SessionModel>.Create(key => key.channelMask, (key, value) => key.channelMask = value),
            key => key.ctlS.ChannelMask));

        [WebSocketExposed]
        public Channel ChannelMask => ChannelMaskProperty[this];

        ControlStructure.Channel[] rawChannels;
        static readonly IKeyedRx<SessionModel, ControlStructure.Channel[]> RawChannelsProperty = Register(Properties, null, KeyedRx.Computed(
            Storage<SessionModel>.Create(key => key.rawChannels, (key, value) => key.rawChannels = value),
            key => key.ctlS.Channels));

        ChannelModel[] channels;
        static readonly IKeyedRx<SessionModel, ChannelModel[]> ChannelsProperty = Register(Properties, null, KeyedRx.Computed(
            RawChannelsProperty,
            Storage<SessionModel>.Create(key => key.channels, (key, value) => key.channels = value),
            (key, newChannels) => Array.ConvertAll(newChannels,
                ch => Array.Find(key.channels, chM => chM.Id == ch.Id) ?? new ChannelModel(ch, key.settings))))
            .Watch((key, newChannelModels, oldChannelModels) =>
            {
                (ISet<ChannelModel> removedChannels, ISet<ChannelModel> keptChannels, ISet<ChannelModel> addedChannels) = oldChannelModels.Diff(newChannelModels);
                foreach (ChannelModel channel in removedChannels)
                {
                    channel.PropertyChanged -= key.Channel_PropertyChanged;
                }
                foreach (ChannelModel channel in addedChannels)
                {
                    channel.PropertyChanged += key.Channel_PropertyChanged;
                }
                key.OnPropertyChanged(nameof(Channels), (removedChannels, keptChannels, addedChannels));
            });

        public ChannelModel[] Channels => ChannelsProperty[this];

        float maxMasterVolume;
        static readonly IWritableKeyedRx<SessionModel, float> MaxMasterVolumeProperty = Register(Properties, nameof(MaxMasterVolume), KeyedRx.Data(
            Storage<SessionModel>.Create(key => key.maxMasterVolume, (key, value) => key.maxMasterVolume = value),
            key => (float)key.settings.MaxMasterVolume))
            .Watch(key =>
            {
                MasterVolumeProperty.Update(key);
                key.MasterVolume = Math.Min(1.0f, key.MasterVolume);
            });

        [WebSocketExposed]
        public float MaxMasterVolume
        {
            get => MaxMasterVolumeProperty[this];
            set => MaxMasterVolumeProperty[this] = value;
        }

        float silenceThreshold;
        static readonly IWritableKeyedRx<SessionModel, float> SilenceThresholdProperty = Register(Properties, nameof(SilenceThreshold), KeyedRx.TwoWayBound(
            Storage<SessionModel>.Create(key => key.silenceThreshold, (key, value) => key.silenceThreshold = value),
            key => key.ctlS.SilenceThreshold,
            (key, value) => key.ctlS.SilenceThreshold = value));

        [WebSocketExposed]
        public float SilenceThreshold
        {
            get => SilenceThresholdProperty[this];
            set => SilenceThresholdProperty[this] = value;
        }

        float averagingWeight;
        static readonly IWritableKeyedRx<SessionModel, float> AveragingWeightProperty = Register(Properties, nameof(AveragingWeight), KeyedRx.TwoWayBound(
            Storage<SessionModel>.Create(key => key.averagingWeight, (key, value) => key.averagingWeight = value),
            key => key.ctlS.AveragingWeight,
            (key, value) => key.ctlS.AveragingWeight = value));

        [WebSocketExposed]
        public float AveragingWeight
        {
            get => AveragingWeightProperty[this];
            set => AveragingWeightProperty[this] = value;
        }

        float saturationThreshold;
        static readonly IWritableKeyedRx<SessionModel, float> SaturationThresholdProperty = Register(Properties, nameof(SaturationThreshold), KeyedRx.TwoWayBound(
            Storage<SessionModel>.Create(key => key.saturationThreshold, (key, value) => key.saturationThreshold = value),
            key => key.ctlS.SaturationThreshold,
            (key, value) => key.ctlS.SaturationThreshold = value));

        [WebSocketExposed]
        public float SaturationThreshold
        {
            get => SaturationThresholdProperty[this];
            set => SaturationThresholdProperty[this] = value;
        }

        float saturationDebounceFactor;
        static readonly IWritableKeyedRx<SessionModel, float> SaturationDebounceFactorProperty = Register(Properties, nameof(SaturationDebounceFactor), KeyedRx.TwoWayBound(
            Storage<SessionModel>.Create(key => key.saturationDebounceFactor, (key, value) => key.saturationDebounceFactor = value),
            key => key.ctlS.SaturationDebounceFactor,
            (key, value) => key.ctlS.SaturationDebounceFactor = value));

        [WebSocketExposed]
        public float SaturationDebounceFactor
        {
            get => SaturationDebounceFactorProperty[this];
            set => SaturationDebounceFactorProperty[this] = value;
        }

        float saturationRecoveryFactor;
        static readonly IWritableKeyedRx<SessionModel, float> SaturationRecoveryFactorProperty = Register(Properties, nameof(SaturationRecoveryFactor), KeyedRx.TwoWayBound(
            Storage<SessionModel>.Create(key => key.saturationRecoveryFactor, (key, value) => key.saturationRecoveryFactor = value),
            key => key.ctlS.SaturationRecoveryFactor,
            (key, value) => key.ctlS.SaturationRecoveryFactor = value));

        [WebSocketExposed]
        public float SaturationRecoveryFactor
        {
            get => SaturationRecoveryFactorProperty[this];
            set => SaturationRecoveryFactorProperty[this] = value;
        }

        float saturationEffectiveVolume;
        static readonly IKeyedRx<SessionModel, float> SaturationEffectiveVolumeProperty = Register(Properties, nameof(SaturationEffectiveVolume), KeyedRx.Computed(
            Storage<SessionModel>.Create(key => key.saturationEffectiveVolume, (key, value) => key.saturationEffectiveVolume = value),
            key => key.ctlS.SaturationEffectiveVolume));

        [WebSocketExposed]
        public float SaturationEffectiveVolume => SaturationEffectiveVolumeProperty[this];

        public BufferConsole Console => console;

        long lastFrame;
        static readonly IKeyedRx<SessionModel, long> LastFrameTickCountProperty = Register(Properties, nameof(LastFrameTickCount), KeyedRx.Computed(
            Storage<SessionModel>.Create(key => key.lastFrame, (key, value) => key.lastFrame = value),
            key => key.ctlS.LastFrameTickCount))
            .Watch(key =>
            {
                key.monitorBuffer[key.monitorCursor] = key.lastMaxAmp * key.ctlS.SaturationEffectiveVolume * key.ctlS.MasterVolume;
                key.monitorCursor = (key.monitorCursor + 1) % MonitorCapacity;
                MonitorMaxProperty.Update(key);
            });

        public long LastFrameTickCount => LastFrameTickCountProperty[this];

        float lastMaxAmp;
        static readonly IKeyedRx<SessionModel, float> LastFrameMaxAmplitudeProperty = Register(Properties, null, KeyedRx.Computed(
            Storage<SessionModel>.Create(key => key.lastMaxAmp, (key, value) => key.lastMaxAmp = value),
            key => key.ctlS.LastFrameMaxAmplitude));

        float monitorMax;
        static readonly IKeyedRx<SessionModel, float> MonitorMaxProperty = Register(Properties, null, KeyedRx.Computed(
            Storage<SessionModel>.Create(key => key.monitorMax, (key, value) => key.monitorMax = value),
            key =>
            {
                float monitorMax = key.monitorBuffer[0];
                for (int i = 1; i < MonitorCapacity; ++i)
                {
                    monitorMax = Math.Max(monitorMax, key.monitorBuffer[i]);
                }

                return monitorMax;
            }));

        float monitorVolume;
        static readonly IKeyedRx<SessionModel, float> MonitorVolumeProperty = Register(Properties, nameof(MonitorVolume), KeyedRx.Computed(
            MutedProperty, MasterVolumeProperty, MonitorMaxProperty, MaxMasterVolumeProperty,
            Storage<SessionModel>.Create(key => key.monitorVolume, (key, value) => key.monitorVolume = value),
            (key, muted, masterVolume, monitorMax, maxMasterVolume) => muted ? 0.0f : Math.Min(masterVolume, monitorMax / maxMasterVolume)));

        [WebSocketExposed]
        public float MonitorVolume => MonitorVolumeProperty[this];

        int lastCursor;
        static readonly IKeyedRx<SessionModel, int> LastFrameTapWriteCursorProperty = Register(Properties, nameof(LastFrameTapWriteCursor), KeyedRx.Computed(
            Storage<SessionModel>.Create(key => key.lastCursor, (key, value) => key.lastCursor = value),
            key => key.ctlS.TapWriteCursor))
            .Watch((key, newLastCursor, oldLastCursor) =>
            {
                int tapCapacity = key.ctlS.TapCapacity;
                TapWriteCursorDeltaProperty[key] = (newLastCursor + tapCapacity - oldLastCursor) % tapCapacity;
            });

        public int LastFrameTapWriteCursor => LastFrameTapWriteCursorProperty[this];

        int cursorDelta;
        static readonly IWritableKeyedRx<SessionModel, int> TapWriteCursorDeltaProperty = Register(Properties, nameof(TapWriteCursorDelta), KeyedRx.Data(
            Storage<SessionModel>.Create(key => key.cursorDelta, (key, value) => key.cursorDelta = value),
            null));

        [WebSocketExposed]
        public int TapWriteCursorDelta => TapWriteCursorDeltaProperty[this];

        public SessionModel(SessionSettings settings, ControlStructure ctlS, BufferConsole console)
        {
            id = ++nextId;

            this.settings = settings;
            this.ctlS = ctlS;
            this.console = console;

            monitorBuffer = new float[MonitorCapacity];
            monitorCursor = 0;

            channels = new ChannelModel[0];

            Initialize(this, Properties);

            Muted = settings.Muted;
            MasterVolume = (float)settings.MasterVolume;
            SaturationThreshold = (float)settings.SaturationThreshold;
            SilenceThreshold = (float)settings.SilenceThreshold;
            AveragingWeight = (float)settings.AveragingWeight;
            SaturationDebounceFactor = (float)settings.SaturationDebounceFactor;
            SaturationRecoveryFactor = (float)settings.SaturationRecoveryFactor;
            ResetSaturation();
            ctlS.Initialized = true;
        }

        public ChannelModel GetChannel(Channel id)
        {
            return Array.Find(channels, c => c.Id == id);
        }

        public void Poll()
        {
            SaturationEffectiveVolumeProperty.Update(this);
            LastFrameMaxAmplitudeProperty.Update(this);
            LastFrameTickCountProperty.Update(this);
            SampleRateProperty.Update(this);
            ChannelMaskProperty.Update(this);
            RawChannelsProperty.Update(this);
        }

        public void PollTap()
        {
            byte[] data = null;
            lock (this)
            {
                if (null != tapStream)
                {
                    using (MemoryStream buffer = new MemoryStream())
                    {
                        byte[] page = new byte[Environment.SystemPageSize];
                        int n;
                        while ((n = tapStream.Read(page, 0, page.Length)) > 0)
                        {
                            buffer.Write(page, 0, n);
                        }
                        if (buffer.Length == 0L)
                        {
                            return;
                        }
                        data = buffer.ToArray();
                    }
                }
            }
            if (null != data)
            {
                OnTapData(new TapDataEventArgs(data));
            }
        }

        public void UpdateCursor()
        {
            if (!LastFrameTapWriteCursorProperty.Update(this))
            {
                TapWriteCursorDeltaProperty[this] = 0;
            }
        }

        public void ResetSaturation()
        {
            ctlS.SaturationEffectiveVolume = 1.0f;
            ctlS.SaturationDebounceVolume = 1.0f;
            SaturationEffectiveVolumeProperty.Update(this);
        }

        void Channel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnChannelPropertyChanged(new ChannelPropertyChangedEventArgs(this, (ChannelModel)sender, e));
        }

        protected override bool DoUpdateSettings()
        {
            bool anyChanged = false;

            if (customName != settings.CustomName)
            {
                settings.CustomName = customName;
                anyChanged = true;
            }

            if (color != settings.Color)
            {
                settings.Color = color;
                anyChanged = true;
            }

            double maxMasterVolume = MaxMasterVolume;
            if (maxMasterVolume != settings.MaxMasterVolume)
            {
                settings.MaxMasterVolume = maxMasterVolume;
                anyChanged = true;
            }

            double masterVolume = MasterVolume;
            if (masterVolume != settings.MasterVolume)
            {
                settings.MasterVolume = masterVolume;
                anyChanged = true;
            }

            bool muted = Muted;
            if (muted != settings.Muted)
            {
                settings.Muted = muted;
                anyChanged = true;
            }

            foreach (ChannelModel channel in channels)
            {
                if (channel.UpdateSettings())
                {
                    anyChanged = true;
                }
            }

            double saturationThreshold = SaturationThreshold;
            if (saturationThreshold != settings.SaturationThreshold)
            {
                settings.SaturationThreshold = saturationThreshold;
                anyChanged = true;
            }

            double silenceThreshold = SilenceThreshold;
            if (silenceThreshold != settings.SilenceThreshold)
            {
                settings.SilenceThreshold = silenceThreshold;
                anyChanged = true;
            }

            double averagingWeight = AveragingWeight;
            if (averagingWeight != settings.AveragingWeight)
            {
                settings.AveragingWeight = averagingWeight;
                anyChanged = true;
            }

            double saturationDebounceFactor = SaturationDebounceFactor;
            if (saturationDebounceFactor != settings.SaturationDebounceFactor)
            {
                settings.SaturationDebounceFactor = saturationDebounceFactor;
                anyChanged = true;
            }

            double saturationRecoveryFactor = SaturationRecoveryFactor;
            if (saturationRecoveryFactor != settings.SaturationRecoveryFactor)
            {
                settings.SaturationRecoveryFactor = saturationRecoveryFactor;
                anyChanged = true;
            }

            return anyChanged;
        }

        protected virtual void OnChannelPropertyChanged(ChannelPropertyChangedEventArgs e)
        {
            ChannelPropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnTapData(TapDataEventArgs e)
        {
            EventHandler<TapDataEventArgs> tapData;
            lock (this)
            {
                tapData = this.tapData;
            }
            tapData?.Invoke(this, e);
        }
    }
}
