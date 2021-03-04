using EtherSound.Settings;
using Reactivity;
using System;
using System.Collections.Generic;

namespace EtherSound.ViewModel
{
    class NetworkSettingsModel : SettingsModel
    {
        new static readonly IList<IKeyedRx<NetworkSettingsModel>> Properties = new List<IKeyedRx<NetworkSettingsModel>>(SettingsModel.Properties);

        readonly RootModel root;
        readonly RootSettings settings;

        public RootModel Root => root;

        bool netDefaultsChanged;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> NetworkSinkDefaultsChangedProperty = Register(Properties, nameof(NetworkSinkDefaultsChanged), KeyedRx.Data(
            Storage<NetworkSettingsModel>.Create(key => key.netDefaultsChanged, (key, value) => key.netDefaultsChanged = value),
            null));

        public bool NetworkSinkDefaultsChanged
        {
            get => NetworkSinkDefaultsChangedProperty[this];
            set => NetworkSinkDefaultsChangedProperty[this] = value;
        }

        bool wsEndpointChanged;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketEndpointChangedProperty = Register(Properties, nameof(WebSocketEndpointChanged), KeyedRx.Data(
            Storage<NetworkSettingsModel>.Create(key => key.wsEndpointChanged, (key, value) => key.wsEndpointChanged = value),
            null));

        public bool WebSocketEndpointChanged
        {
            get => WebSocketEndpointChangedProperty[this];
            set => WebSocketEndpointChangedProperty[this] = value;
        }

        string wsUri;
        static readonly IWritableKeyedRx<NetworkSettingsModel, string> WebSocketUriProperty = Register(Properties, nameof(WebSocketUri), KeyedRx.Data(
            Storage<NetworkSettingsModel>.Create(key => key.wsUri, (key, value) => key.wsUri = value),
            key => key.settings.WebSocketEndpoint?.Uri));

        public string WebSocketUri
        {
            get => WebSocketUriProperty[this];
            set => WebSocketUriProperty[this] = value;
        }

        string wsPreSharedSecret;
        static readonly IWritableKeyedRx<NetworkSettingsModel, string> WebSocketPreSharedSecretProperty = Register(Properties, nameof(WebSocketPreSharedSecret), KeyedRx.Data(
            Storage<NetworkSettingsModel>.Create(key => key.wsPreSharedSecret, (key, value) => key.wsPreSharedSecret = value),
            key => key.settings.WebSocketEndpoint?.PreSharedSecret));

        public string WebSocketPreSharedSecret
        {
            get => WebSocketPreSharedSecretProperty[this];
            set => WebSocketPreSharedSecretProperty[this] = value;
        }

        WebSocketPermissions wsGlobalPermissions;
        static readonly IWritableKeyedRx<NetworkSettingsModel, WebSocketPermissions> WebSocketGlobalPermissionsProperty = Register(Properties, nameof(WebSocketGlobalPermissions), KeyedRx.Data(
            Storage<NetworkSettingsModel>.Create(key => key.wsGlobalPermissions, (key, value) => key.wsGlobalPermissions = value),
            key => key.settings.WebSocketEndpoint?.GlobalPermissions ?? WebSocketPermissions.DefaultGlobal));

        public WebSocketPermissions WebSocketGlobalPermissions
        {
            get => WebSocketGlobalPermissionsProperty[this];
            set => WebSocketGlobalPermissionsProperty[this] = value;
        }

        bool wsGlobalCanRead;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketGlobalCanReadProperty = Register(Properties, nameof(WebSocketGlobalCanRead), KeyedRx.TwoWayBound(
            WebSocketGlobalPermissionsProperty,
            (key => key.wsGlobalCanRead, (key, value) => key.wsGlobalCanRead = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.Read)));

        public bool WebSocketGlobalCanRead
        {
            get => WebSocketGlobalCanReadProperty[this];
            set => WebSocketGlobalCanReadProperty[this] = value;
        }

        bool wsGlobalCanWriteProperties;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketGlobalCanWritePropertiesProperty = Register(Properties, nameof(WebSocketGlobalCanWriteProperties), KeyedRx.TwoWayBound(
            WebSocketGlobalPermissionsProperty,
            (key => key.wsGlobalCanWriteProperties, (key, value) => key.wsGlobalCanWriteProperties = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.WriteProperties)));

        public bool WebSocketGlobalCanWriteProperties
        {
            get => WebSocketGlobalCanWritePropertiesProperty[this];
            set => WebSocketGlobalCanWritePropertiesProperty[this] = value;
        }

        bool wsGlobalCanConfigureSessions;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketGlobalCanConfigureSessionsProperty = Register(Properties, nameof(WebSocketGlobalCanConfigureSessions), KeyedRx.TwoWayBound(
            WebSocketGlobalPermissionsProperty,
            (key => key.wsGlobalCanConfigureSessions, (key, value) => key.wsGlobalCanConfigureSessions = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.ConfigureSessions)));

        public bool WebSocketGlobalCanConfigureSessions
        {
            get => WebSocketGlobalCanConfigureSessionsProperty[this];
            set => WebSocketGlobalCanConfigureSessionsProperty[this] = value;
        }

        bool wsGlobalCanTapStream;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketGlobalCanTapStreamProperty = Register(Properties, nameof(WebSocketGlobalCanTapStream), KeyedRx.TwoWayBound(
            WebSocketGlobalPermissionsProperty,
            (key => key.wsGlobalCanTapStream, (key, value) => key.wsGlobalCanTapStream = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.TapStream)));

        public bool WebSocketGlobalCanTapStream
        {
            get => WebSocketGlobalCanTapStreamProperty[this];
            set => WebSocketGlobalCanTapStreamProperty[this] = value;
        }

        WebSocketPermissions wsNetworkPermissions;
        static readonly IWritableKeyedRx<NetworkSettingsModel, WebSocketPermissions> WebSocketNetworkPermissionsProperty = Register(Properties, nameof(WebSocketNetworkPermissions), KeyedRx.Data(
            Storage<NetworkSettingsModel>.Create(key => key.wsNetworkPermissions, (key, value) => key.wsNetworkPermissions = value),
            key => key.settings.WebSocketEndpoint?.NetworkPermissions ?? WebSocketPermissions.DefaultNetwork));

        public WebSocketPermissions WebSocketNetworkPermissions
        {
            get => WebSocketNetworkPermissionsProperty[this];
            set => WebSocketNetworkPermissionsProperty[this] = value;
        }

        bool wsNetworkCanRead;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketNetworkCanReadProperty = Register(Properties, nameof(WebSocketNetworkCanRead), KeyedRx.TwoWayBound(
            WebSocketNetworkPermissionsProperty,
            (key => key.wsNetworkCanRead, (key, value) => key.wsNetworkCanRead = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.Read)));

        public bool WebSocketNetworkCanRead
        {
            get => WebSocketNetworkCanReadProperty[this];
            set => WebSocketNetworkCanReadProperty[this] = value;
        }

        bool wsNetworkCanWriteProperties;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketNetworkCanWritePropertiesProperty = Register(Properties, nameof(WebSocketNetworkCanWriteProperties), KeyedRx.TwoWayBound(
            WebSocketNetworkPermissionsProperty,
            (key => key.wsNetworkCanWriteProperties, (key, value) => key.wsNetworkCanWriteProperties = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.WriteProperties)));

        public bool WebSocketNetworkCanWriteProperties
        {
            get => WebSocketNetworkCanWritePropertiesProperty[this];
            set => WebSocketNetworkCanWritePropertiesProperty[this] = value;
        }

        bool wsNetworkCanConfigureSessions;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketNetworkCanConfigureSessionsProperty = Register(Properties, nameof(WebSocketNetworkCanConfigureSessions), KeyedRx.TwoWayBound(
            WebSocketNetworkPermissionsProperty,
            (key => key.wsNetworkCanConfigureSessions, (key, value) => key.wsNetworkCanConfigureSessions = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.ConfigureSessions)));

        public bool WebSocketNetworkCanConfigureSessions
        {
            get => WebSocketNetworkCanConfigureSessionsProperty[this];
            set => WebSocketNetworkCanConfigureSessionsProperty[this] = value;
        }

        bool wsNetworkCanTapStream;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketNetworkCanTapStreamProperty = Register(Properties, nameof(WebSocketNetworkCanTapStream), KeyedRx.TwoWayBound(
            WebSocketNetworkPermissionsProperty,
            (key => key.wsNetworkCanTapStream, (key, value) => key.wsNetworkCanTapStream = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.TapStream)));

        public bool WebSocketNetworkCanTapStream
        {
            get => WebSocketNetworkCanTapStreamProperty[this];
            set => WebSocketNetworkCanTapStreamProperty[this] = value;
        }

        WebSocketPermissions wsUnauthenticatedPermissions;
        static readonly IWritableKeyedRx<NetworkSettingsModel, WebSocketPermissions> WebSocketUnauthenticatedPermissionsProperty = Register(Properties, nameof(WebSocketUnauthenticatedPermissions), KeyedRx.Data(
            Storage<NetworkSettingsModel>.Create(key => key.wsUnauthenticatedPermissions, (key, value) => key.wsUnauthenticatedPermissions = value),
            key => key.settings.WebSocketEndpoint?.UnauthenticatedPermissions ?? WebSocketPermissions.DefaultUnauthenticated));

        public WebSocketPermissions WebSocketUnauthenticatedPermissions
        {
            get => WebSocketUnauthenticatedPermissionsProperty[this];
            set => WebSocketUnauthenticatedPermissionsProperty[this] = value;
        }

        bool wsUnauthenticatedCanRead;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketUnauthenticatedCanReadProperty = Register(Properties, nameof(WebSocketUnauthenticatedCanRead), KeyedRx.TwoWayBound(
            WebSocketUnauthenticatedPermissionsProperty,
            (key => key.wsUnauthenticatedCanRead, (key, value) => key.wsUnauthenticatedCanRead = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.Read)));

        public bool WebSocketUnauthenticatedCanRead
        {
            get => WebSocketUnauthenticatedCanReadProperty[this];
            set => WebSocketUnauthenticatedCanReadProperty[this] = value;
        }

        bool wsUnauthenticatedCanWriteProperties;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketUnauthenticatedCanWritePropertiesProperty = Register(Properties, nameof(WebSocketUnauthenticatedCanWriteProperties), KeyedRx.TwoWayBound(
            WebSocketUnauthenticatedPermissionsProperty,
            (key => key.wsUnauthenticatedCanWriteProperties, (key, value) => key.wsUnauthenticatedCanWriteProperties = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.WriteProperties)));

        public bool WebSocketUnauthenticatedCanWriteProperties
        {
            get => WebSocketUnauthenticatedCanWritePropertiesProperty[this];
            set => WebSocketUnauthenticatedCanWritePropertiesProperty[this] = value;
        }

        bool wsUnauthenticatedCanConfigureSessions;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketUnauthenticatedCanConfigureSessionsProperty = Register(Properties, nameof(WebSocketUnauthenticatedCanConfigureSessions), KeyedRx.TwoWayBound(
            WebSocketUnauthenticatedPermissionsProperty,
            (key => key.wsUnauthenticatedCanConfigureSessions, (key, value) => key.wsUnauthenticatedCanConfigureSessions = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.ConfigureSessions)));

        public bool WebSocketUnauthenticatedCanConfigureSessions
        {
            get => WebSocketUnauthenticatedCanConfigureSessionsProperty[this];
            set => WebSocketUnauthenticatedCanConfigureSessionsProperty[this] = value;
        }

        bool wsUnauthenticatedCanTapStream;
        static readonly IWritableKeyedRx<NetworkSettingsModel, bool> WebSocketUnauthenticatedCanTapStreamProperty = Register(Properties, nameof(WebSocketUnauthenticatedCanTapStream), KeyedRx.TwoWayBound(
            WebSocketUnauthenticatedPermissionsProperty,
            (key => key.wsUnauthenticatedCanTapStream, (key, value) => key.wsUnauthenticatedCanTapStream = value),
            new WebSocketPermissionsConverter(WebSocketPermissions.TapStream)));

        public bool WebSocketUnauthenticatedCanTapStream
        {
            get => WebSocketUnauthenticatedCanTapStreamProperty[this];
            set => WebSocketUnauthenticatedCanTapStreamProperty[this] = value;
        }

        bool nsdDirty;
        static readonly IKeyedRx<NetworkSettingsModel, bool> NetworkSinkDefaultsDirtyProperty = Register(Properties, null, KeyedRx.Computed(
            BindAddressProperty, PeerAddressProperty, PeerServiceProperty,
            Storage<NetworkSettingsModel>.Create(key => key.nsdDirty, (key, value) => key.nsdDirty = value),
            (key, bindAddress, peerAddress, peerService) =>
                   bindAddress != key.settings.NetworkSinkDefaults?.BindAddress
                || peerAddress != key.settings.NetworkSinkDefaults?.PeerAddress
                || peerService != key.settings.NetworkSinkDefaults?.PeerService));

        bool wsDirty;
        static readonly IKeyedRx<NetworkSettingsModel, bool> WebSocketDirtyProperty = Register(Properties, null, KeyedRx.Computed(
            WebSocketUriProperty, WebSocketPreSharedSecretProperty, WebSocketGlobalPermissionsProperty, WebSocketNetworkPermissionsProperty, WebSocketUnauthenticatedPermissionsProperty,
            Storage<NetworkSettingsModel>.Create(key => key.wsDirty, (key, value) => key.wsDirty = value),
            (key, webSocketUri, webSocketPreSharedSecret, webSocketGlobalPermissions, webSocketNetworkPermissions, webSocketUnauthenticatedPermissions) =>
                   webSocketUri != key.settings.WebSocketEndpoint?.Uri
                || webSocketPreSharedSecret != key.settings.WebSocketEndpoint?.PreSharedSecret
                || webSocketGlobalPermissions != (key.settings.WebSocketEndpoint?.GlobalPermissions ?? WebSocketPermissions.DefaultGlobal)
                || webSocketNetworkPermissions != (key.settings.WebSocketEndpoint?.NetworkPermissions ?? WebSocketPermissions.DefaultNetwork)
                || webSocketUnauthenticatedPermissions != (key.settings.WebSocketEndpoint?.UnauthenticatedPermissions ?? WebSocketPermissions.DefaultUnauthenticated)));

        bool dirty;
        static readonly IKeyedRx<NetworkSettingsModel, bool> DirtyProperty = Register(Properties, nameof(Dirty), KeyedRx.Computed(
            NetworkSinkDefaultsDirtyProperty, WebSocketDirtyProperty,
            Storage<NetworkSettingsModel>.Create(key => key.dirty, (key, value) => key.dirty = value),
            (key, networkSinkDefaultsDirty, webSocketDirty) =>
                   networkSinkDefaultsDirty
                || webSocketDirty));

        public override bool Dirty => DirtyProperty[this];

        public NetworkSettingsModel(RootModel root)
        {
            this.root = root;
            settings = root.Settings;

            BindAddress = settings.NetworkSinkDefaults?.BindAddress;
            PeerAddress = settings.NetworkSinkDefaults?.PeerAddress;
            PeerService = settings.NetworkSinkDefaults?.PeerService;

            Initialize(this, Properties);
        }

        protected override bool DoUpdateSettings()
        {
            bool anyChanged = false;

            NetworkSinkSettings netDefaults = settings.NetworkSinkDefaults;
            if (null == netDefaults)
            {
                netDefaults = new NetworkSinkSettings();
                settings.NetworkSinkDefaults = netDefaults;
            }

            if (string.IsNullOrWhiteSpace(BindAddress))
            {
                BindAddress = null;
            }
            if (BindAddress != netDefaults.BindAddress)
            {
                netDefaults.BindAddress = BindAddress;
                NetworkSinkDefaultsChanged = true;
                anyChanged = true;
            }

            if (string.IsNullOrWhiteSpace(PeerAddress))
            {
                PeerAddress = null;
            }
            if (PeerAddress != netDefaults.PeerAddress)
            {
                netDefaults.PeerAddress = PeerAddress;
                NetworkSinkDefaultsChanged = true;
                anyChanged = true;
            }

            if (string.IsNullOrWhiteSpace(PeerService))
            {
                PeerService = null;
            }
            if (PeerService != netDefaults.PeerService)
            {
                netDefaults.PeerService = PeerService;
                NetworkSinkDefaultsChanged = true;
                anyChanged = true;
            }

            NetworkSinkDefaultsDirtyProperty.Update(this);

            WebSocketSettings wsEndpoint = settings.WebSocketEndpoint;
            if (null == wsEndpoint)
            {
                wsEndpoint = new WebSocketSettings();
                settings.WebSocketEndpoint = wsEndpoint;
            }

            if (string.IsNullOrWhiteSpace(WebSocketUri))
            {
                WebSocketUri = null;
            }
            if (WebSocketUri != wsEndpoint.Uri)
            {
                wsEndpoint.Uri = WebSocketUri;
                WebSocketEndpointChanged = true;
                anyChanged = true;
            }

            if (string.IsNullOrWhiteSpace(WebSocketPreSharedSecret))
            {
                WebSocketPreSharedSecret = null;
            }
            if (WebSocketPreSharedSecret != wsEndpoint.PreSharedSecret)
            {
                wsEndpoint.PreSharedSecret = WebSocketPreSharedSecret;
                WebSocketEndpointChanged = true;
                anyChanged = true;
            }

            if (WebSocketGlobalPermissions != wsEndpoint.GlobalPermissions)
            {
                wsEndpoint.GlobalPermissions = WebSocketGlobalPermissions;
                WebSocketEndpointChanged = true;
                anyChanged = true;
            }

            if (WebSocketNetworkPermissions != wsEndpoint.NetworkPermissions)
            {
                wsEndpoint.NetworkPermissions = WebSocketNetworkPermissions;
                WebSocketEndpointChanged = true;
                anyChanged = true;
            }

            if (WebSocketUnauthenticatedPermissions != wsEndpoint.UnauthenticatedPermissions)
            {
                wsEndpoint.UnauthenticatedPermissions = WebSocketUnauthenticatedPermissions;
                WebSocketEndpointChanged = true;
                anyChanged = true;
            }

            WebSocketDirtyProperty.Update(this);

            return anyChanged;
        }

        protected override void OnSettingsUpdated(EventArgs e)
        {
            settings.Dirty = true;
            base.OnSettingsUpdated(e);
        }

        private class WebSocketPermissionsConverter : ITwoWayConverter<bool, WebSocketPermissions>
        {
            readonly WebSocketPermissions permission;

            public WebSocketPermissionsConverter(WebSocketPermissions permission)
            {
                this.permission = permission;
            }

            public bool Convert(WebSocketPermissions value)
            {
                return (value & permission) != 0;
            }

            public WebSocketPermissions ConvertBack(bool value, WebSocketPermissions oldValue)
            {
                return value
                    ? (oldValue | permission)
                    : (oldValue & ~permission);
            }
        }
    }
}
