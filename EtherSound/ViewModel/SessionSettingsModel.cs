using EtherSound.Settings;
using Reactivity;
using System;
using System.Collections.Generic;
using WASCap;

namespace EtherSound.ViewModel
{
    class SessionSettingsModel : SettingsModel
    {
        new static readonly IList<IKeyedRx<SessionSettingsModel>> Properties = new List<IKeyedRx<SessionSettingsModel>>(SettingsModel.Properties);

        readonly RootSettings settings;
        readonly SessionSettings session;
        readonly DeviceModel[] renderDevices;
        readonly DeviceModel[] captureDevices;

        bool isNew;
        static readonly IWritableKeyedRx<SessionSettingsModel, bool> IsNewProperty = Register(Properties, nameof(IsNew), KeyedRx.Data(
            Storage<SessionSettingsModel>.Create(key => key.isNew, (key, value) => key.isNew = value),
            null));

        public bool IsNew
        {
            get => IsNewProperty[this];
            set => IsNewProperty[this] = value;
        }

        DataFlow sourceFlow;
        static readonly IWritableKeyedRx<SessionSettingsModel, DataFlow> SourceFlowProperty = Register(Properties, nameof(SourceFlow), KeyedRx.Data(
            Storage<SessionSettingsModel>.Create(key => key.sourceFlow, (key, value) => key.sourceFlow = value),
            key => key.session.Source?.Flow ?? DataFlow.Render));

        public DataFlow SourceFlow
        {
            get => SourceFlowProperty[this];
            set => SourceFlowProperty[this] = value;
        }

        DeviceModel[] sourceDevices;
        static readonly IKeyedRx<SessionSettingsModel, DeviceModel[]> SourceDevicesProperty = Register(Properties, nameof(SourceDevices), KeyedRx.Computed(
            SourceFlowProperty,
            Storage<SessionSettingsModel>.Create(key => key.sourceDevices, (key, value) => key.sourceDevices = value),
            (key, sourceFlow) => (sourceFlow == DataFlow.Capture) ? key.captureDevices : key.renderDevices))
            .Watch((key, sourceDevices) => key.SourceDevice = sourceDevices[0]);

        public DeviceModel[] SourceDevices => SourceDevicesProperty[this];

        DeviceModel sourceDevice;
        static readonly IWritableKeyedRx<SessionSettingsModel, DeviceModel> SourceDeviceProperty = Register(Properties, nameof(SourceDevice), KeyedRx.Data(
            Storage<SessionSettingsModel>.Create(key => key.sourceDevice, (key, value) => key.sourceDevice = value),
            key => Array.Find(key.SourceDevices, dev => dev.IsMatch(key.session.Source)) ?? key.SourceDevices[0]))
            .Watch((key, sourceDevice) => key.Channels = sourceDevice?.Channels ?? 0);

        public DeviceModel SourceDevice
        {
            get => SourceDeviceProperty[this];
            set => SourceDeviceProperty[this] = value;
        }

        int? samplerate;
        static readonly IWritableKeyedRx<SessionSettingsModel, int?> SampleRateProperty = Register(Properties, nameof(SampleRate), KeyedRx.Data(
            Storage<SessionSettingsModel>.Create(key => key.samplerate, (key, value) => key.samplerate = value),
            key => key.session.SampleRate));

        public int? SampleRate
        {
            get => SampleRateProperty[this];
            set => SampleRateProperty[this] = value;
        }

        SessionChannelModel[] channelObjects;
        static readonly IKeyedRx<SessionSettingsModel, SessionChannelModel[]> ChannelObjectsProperty = Register(Properties, nameof(ChannelObjects), KeyedRx.Computed(
            SourceDeviceProperty,
            Storage<SessionSettingsModel>.Create(key => key.channelObjects, (key, value) => key.channelObjects = value),
            (key, sourceDevice) => key.CalculateChannelObjects()));

        public SessionChannelModel[] ChannelObjects => ChannelObjectsProperty[this];

        Channel channels;
        static readonly IWritableKeyedRx<SessionSettingsModel, Channel> ChannelsProperty = Register(Properties, nameof(Channels), KeyedRx.Data(
            Storage<SessionSettingsModel>.Create(key => key.channels, (key, value) => key.channels = value),
            key => (key.session.Channels == 0) ? key.sourceDevice.Channels : key.session.Channels));

        public Channel Channels
        {
            get => ChannelsProperty[this];
            set => ChannelsProperty[this] = value;
        }

        bool wasSink;
        static readonly IWritableKeyedRx<SessionSettingsModel, bool> WithWASSinkProperty = Register(Properties, nameof(WithWASSink), KeyedRx.Data(
            Storage<SessionSettingsModel>.Create(key => key.wasSink, (key, value) => key.wasSink = value),
            key => key.session.WASSink != null));

        public bool WithWASSink
        {
            get => WithWASSinkProperty[this];
            set => WithWASSinkProperty[this] = value;
        }

        public DeviceModel[] SinkDevices => renderDevices;

        DeviceModel sinkDevice;
        static readonly IWritableKeyedRx<SessionSettingsModel, DeviceModel> SinkDeviceProperty = Register(Properties, nameof(SinkDevice), KeyedRx.Data(
            Storage<SessionSettingsModel>.Create(key => key.sinkDevice, (key, value) => key.sinkDevice = value),
            key => key.wasSink ? (Array.Find(key.SinkDevices, dev => dev.IsMatch(key.session.WASSink)) ?? key.SinkDevices[0]) : key.SinkDevices[0]));

        public DeviceModel SinkDevice
        {
            get => SinkDeviceProperty[this];
            set => SinkDeviceProperty[this] = value;
        }

        bool networkSink;
        static readonly IWritableKeyedRx<SessionSettingsModel, bool> WithNetworkSinkProperty = Register(Properties, nameof(WithNetworkSink), KeyedRx.Data(
            Storage<SessionSettingsModel>.Create(key => key.networkSink, (key, value) => key.networkSink = value),
            key => key.session.NetworkSink != null));

        public bool WithNetworkSink
        {
            get => WithNetworkSinkProperty[this];
            set => WithNetworkSinkProperty[this] = value;
        }

        bool sourceDirty;
        static readonly IKeyedRx<SessionSettingsModel, bool> SourceDirtyProperty = Register(Properties, null, KeyedRx.Computed(
            SourceFlowProperty, SourceDeviceProperty, SampleRateProperty, ChannelsProperty,
            Storage<SessionSettingsModel>.Create(key => key.sourceDirty, (key, value) => key.sourceDirty = value),
            (key, sourceFlow, sourceDevice, sampleRate, channels) =>
                   sourceFlow != (key.session.Source?.Flow ?? DataFlow.Render)
                || !sourceDevice.IsMatch(key.session.Source)
                || sampleRate != key.session.SampleRate
                || channels != ((key.session.Channels == 0) ? sourceDevice.Channels : key.session.Channels)));

        bool nsDirty;
        static readonly IKeyedRx<SessionSettingsModel, bool> NetworkSinkDirtyProperty = Register(Properties, null, KeyedRx.Computed(
            WithNetworkSinkProperty, BindAddressProperty, PeerAddressProperty, PeerServiceProperty,
            Storage<SessionSettingsModel>.Create(key => key.nsDirty, (key, value) => key.nsDirty = value),
            (key, withNetworkSink, bindAddress, peerAddress, peerService) =>
                   withNetworkSink != (key.session.NetworkSink != null)
                || (withNetworkSink && (bindAddress != key.session.NetworkSink.BindAddress
                                     || peerAddress != key.session.NetworkSink.PeerAddress
                                     || peerService != key.session.NetworkSink.PeerService))));

        bool dirty;
        static readonly IKeyedRx<SessionSettingsModel, bool> DirtyProperty = Register(Properties, nameof(Dirty), KeyedRx.Computed(
            IsNewProperty, SourceDirtyProperty, WithWASSinkProperty, SinkDeviceProperty, NetworkSinkDirtyProperty,
            Storage<SessionSettingsModel>.Create(key => key.dirty, (key, value) => key.dirty = value),
            (key, isNew, sourceDirty, withWASSink, sinkDevice, networkSinkDirty) =>
                   isNew
                || sourceDirty
                || withWASSink != (key.session.WASSink != null)
                || (withWASSink && !sinkDevice.IsMatch(key.session.WASSink))
                || networkSinkDirty));

        public override bool Dirty => DirtyProperty[this];

        public SessionSettingsModel(RootSettings settings, SessionSettings session, Device[] devices, bool isNew)
        {
            this.settings = settings;
            this.session = session;
            this.isNew = isNew;

            renderDevices = CalculateDevices(devices, DataFlow.Render);
            captureDevices = CalculateDevices(devices, DataFlow.Capture);

            Initialize(this, Properties);

            if (WithNetworkSink)
            {
                BindAddress = session.NetworkSink.BindAddress;
                PeerAddress = session.NetworkSink.PeerAddress;
                PeerService = session.NetworkSink.PeerService;
            }
        }

        static DeviceModel[] CalculateDevices(Device[] devices, DataFlow flow)
        {
            devices = Array.FindAll(devices, dev => (dev.Flow & flow) != 0);
            List<DeviceModel> models = new List<DeviceModel>();
            Device consoleDefault = Array.Find(devices, dev => (dev.DefaultFor & Role.Console) != 0);
            models.Add(new DeviceModel(Role.Console, null, string.Format("Périphérique par défaut : {0}", consoleDefault?.FriendlyName ?? "???"), consoleDefault?.Channels ?? 0));
            Device multimediaDefault = Array.Find(devices, dev => (dev.DefaultFor & Role.Multimedia) != 0);
            models.Add(new DeviceModel(Role.Multimedia, null, string.Format("Périphérique multimédia par défaut : {0}", multimediaDefault?.FriendlyName ?? "???"), multimediaDefault?.Channels ?? 0));
            Device communicationsDefault = Array.Find(devices, dev => (dev.DefaultFor & Role.Communications) != 0);
            models.Add(new DeviceModel(Role.Communications, null, string.Format("Périphérique de communications par défaut : {0}", communicationsDefault?.FriendlyName ?? "???"), communicationsDefault?.Channels ?? 0));
            foreach (Device device in devices)
            {
                models.Add(new DeviceModel(0, device.Id, device.FriendlyName, device.Channels));
            }

            return models.ToArray();
        }

        SessionChannelModel[] CalculateChannelObjects()
        {
            List<SessionChannelModel> channels = new List<SessionChannelModel>();
            int remaining = (int)(sourceDevice?.Channels ?? 0);
            while (0 != remaining)
            {
                channels.Add(new SessionChannelModel(this, (Channel)(remaining & -remaining)));
                remaining &= remaining - 1;
            }

            return channels.ToArray();
        }

        protected override bool DoUpdateSettings()
        {
            bool anyChanged = IsNew;

            WASSourceSettings source = session.Source;
            if (null == source)
            {
                source = new WASSourceSettings();
                session.Source = source;
            }

            if (SourceFlow != source.Flow)
            {
                source.Flow = SourceFlow;
                anyChanged = true;
            }

            if (!SourceDevice.IsMatch(source))
            {
                SourceDevice.CopyTo(source);
                anyChanged = true;
            }

            if (SampleRate != session.SampleRate)
            {
                session.SampleRate = SampleRate;
                anyChanged = true;
            }

            Channel channels = (Channels == sourceDevice.Channels) ? 0 : Channels;
            if (channels != session.Channels)
            {
                session.Channels = channels;
                anyChanged = true;
            }

            SourceDirtyProperty.Update(this);

            if (WithWASSink)
            {
                WASSinkSettings wasSink = session.WASSink;
                if (null == wasSink)
                {
                    wasSink = new WASSinkSettings();
                    session.WASSink = wasSink;
                    anyChanged = true;
                }

                if (!SinkDevice.IsMatch(wasSink))
                {
                    SinkDevice.CopyTo(wasSink);
                    anyChanged = true;
                }
            }
            else
            {
                if (null != session.WASSink)
                {
                    session.WASSink = null;
                    anyChanged = true;
                }
            }

            if (WithNetworkSink)
            {
                NetworkSinkSettings networkSink = session.NetworkSink;
                if (null == networkSink)
                {
                    networkSink = new NetworkSinkSettings();
                    session.NetworkSink = networkSink;
                    anyChanged = true;
                }

                if (string.IsNullOrWhiteSpace(BindAddress))
                {
                    BindAddress = null;
                }
                if (BindAddress != networkSink.BindAddress)
                {
                    networkSink.BindAddress = BindAddress;
                    anyChanged = true;
                }

                if (string.IsNullOrWhiteSpace(PeerAddress))
                {
                    PeerAddress = null;
                }
                if (PeerAddress != networkSink.PeerAddress)
                {
                    networkSink.PeerAddress = PeerAddress;
                    anyChanged = true;
                }

                if (string.IsNullOrWhiteSpace(PeerService))
                {
                    PeerService = null;
                }
                if (PeerService != networkSink.PeerService)
                {
                    networkSink.PeerService = PeerService;
                    anyChanged = true;
                }
            }
            else
            {
                if (null != session.NetworkSink)
                {
                    session.NetworkSink = null;
                    anyChanged = true;
                }
            }

            NetworkSinkDirtyProperty.Update(this);

            DirtyProperty.Update(this);

            return anyChanged;
        }

        protected override void OnSettingsUpdated(EventArgs e)
        {
            session.RestartPending = true;
            settings.Dirty = true;
            base.OnSettingsUpdated(e);
        }

        public class DeviceModel
        {
            readonly Role role;
            readonly string id;
            readonly string name;
            readonly Channel channels;

            public Role Role => role;
            public string Id => id;
            public string Name => name;
            public Channel Channels => channels;

            public DeviceModel(Role role, string id, string name, Channel channels)
            {
                this.role = role;
                this.id = id;
                this.name = name;
                this.channels = channels;
            }

            public bool IsMatch(WASSourceSettings settings)
            {
                if (id != null)
                {
                    return settings.Id == id;
                }
                else
                {
                    return settings.Id == null && settings.Role == role;
                }
            }

            public bool IsMatch(WASSinkSettings settings)
            {
                if (id != null)
                {
                    return settings.Id == id;
                }
                else
                {
                    return settings.Id == null && settings.Role == role;
                }
            }

            public void CopyTo(WASSourceSettings settings)
            {
                settings.Role = role;
                settings.Id = id;
                settings.FriendlyName = name;
            }

            public void CopyTo(WASSinkSettings settings)
            {
                settings.Role = role;
                settings.Id = id;
                settings.FriendlyName = name;
            }

            public override string ToString()
            {
                return name;
            }
        }

        public class SessionChannelModel
        {
            readonly SessionSettingsModel owner;
            readonly Channel id;

            public Channel Id => id;

            public bool Enabled
            {
                get => (owner.Channels & id) != 0;
                set
                {
                    if (value)
                    {
                        owner.Channels |= id;
                    }
                    else
                    {
                        owner.Channels &= ~id;
                    }
                }
            }

            public SessionChannelModel(SessionSettingsModel owner, Channel id)
            {
                this.owner = owner;
                this.id = id;
            }
        }
    }
}
