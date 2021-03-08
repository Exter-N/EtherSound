using EtherSound.Settings;
using EtherSound.ViewModel;
using Newtonsoft.Json.Linq;
using Reactivity;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using WASCap;

namespace EtherSound.WebSocket
{
    class ClientHandler : WebSocketHandler
    {
        private const int UnknownObject = RemoteException.ServerErrorMax - 2;
        private const int PermissionDenied = RemoteException.ServerErrorMax - 3;
        private const int UnknownProperty = RemoteException.ServerErrorMax - 4;
        private const int InvalidPropertyAccess = RemoteException.ServerErrorMax - 5;

        static readonly Dictionary<string, (MethodInfo, MethodInfo)> RootProperties;
        static readonly Dictionary<string, string> RootPropertyNames;
        static readonly Dictionary<string, (MethodInfo, MethodInfo)> SessionProperties;
        static readonly Dictionary<string, string> SessionPropertyNames;
        static readonly Dictionary<string, (MethodInfo, MethodInfo)> ChannelProperties;
        static readonly Dictionary<string, string> ChannelPropertyNames;

        readonly RootModel model;
        readonly SessionManager sessionManager;
        readonly Server server;
        readonly Action onDisposed;
        readonly ISet<string> subscribedRootProperties;
        readonly ISet<string> globallySubscribedSessionProperties;
        readonly ISet<string> globallySubscribedChannelProperties;
        readonly Dictionary<SessionModel, ISet<string>> subscribedSessionProperties;
        readonly Dictionary<SessionModel, ISet<string>> subscribedChannelProperties;
        readonly WebSocketPermissions intents;
        readonly IRx<SessionModel[]> sessions;
        readonly ISet<SessionModel> tapSessions;
        WebSocketPermissions perms;
        bool authenticated;
        bool canAuthenticate;
        bool initiallySentSessions;

        static ClientHandler()
        {
            (RootProperties, RootPropertyNames) = GetProperties(typeof(RootModel));
            (SessionProperties, SessionPropertyNames) = GetProperties(typeof(SessionModel));
            (ChannelProperties, ChannelPropertyNames) = GetProperties(typeof(ChannelModel));
        }

        public ClientHandler(HttpListenerWebSocketContext ctx, RootModel model, SessionManager sessionManager, Server server, Action onDisposed) : base(ctx)
        {
            this.model = model;
            this.sessionManager = sessionManager;
            this.server = server;
            this.onDisposed = onDisposed;
            subscribedRootProperties = new HashSet<string>();
            globallySubscribedSessionProperties = new HashSet<string>();
            globallySubscribedChannelProperties = new HashSet<string>();
            subscribedSessionProperties = new Dictionary<SessionModel, ISet<string>>();
            subscribedChannelProperties = new Dictionary<SessionModel, ISet<string>>();
            intents = CalculateIntents(ctx);
            sessions = Rx.Computed<SessionModel[]>(
                () => ((perms & WebSocketPermissions.ConfigureSessions) != 0) ? model.Sessions : model.ValidSessions,
                new CollectionEqualityComparer<SessionModel>())
                .Watch((newSessions, oldSessions) =>
                {
                    (ISet<SessionModel> removed, _, ISet<SessionModel> added) = oldSessions.Diff(newSessions);
                    foreach (SessionModel session in removed)
                    {
                        subscribedSessionProperties.Remove(session);
                        subscribedChannelProperties.Remove(session);
                        session.TapData -= Session_TapData;
                        lock (tapSessions)
                        {
                            tapSessions.Remove(session);
                        }
                    }
                    NotifySessionsChanged(added);
                });
            tapSessions = new HashSet<SessionModel>();

            RegisterMethod(nameof(Authenticate), Authenticate);

            RegisterMethod(nameof(WatchRootProperty), WatchRootProperty);
            RegisterMethod(nameof(WatchSessionProperty), WatchSessionProperty);
            RegisterMethod(nameof(WatchChannelProperty), WatchChannelProperty);
            RegisterMethod(nameof(UnwatchRootProperty), UnwatchRootProperty);
            RegisterMethod(nameof(UnwatchSessionProperty), UnwatchSessionProperty);
            RegisterMethod(nameof(UnwatchChannelProperty), UnwatchChannelProperty);

            RegisterMethod(nameof(SetRootProperty), SetRootProperty);
            RegisterMethod(nameof(SetSessionProperty), SetSessionProperty);
            RegisterMethod(nameof(SetChannelProperty), SetChannelProperty);

            RegisterMethod(nameof(AddSession), AddSession);
            RegisterMethod(nameof(RemoveSession), RemoveSession);
            RegisterMethod(nameof(QuerySessionConfiguration), QuerySessionConfiguration);
            RegisterMethod(nameof(ConfigureSession), ConfigureSession);
            RegisterMethod(nameof(SetSessionPosition), SetSessionPosition);
            RegisterMethod(nameof(RestartSession), RestartSession);
            RegisterMethod(nameof(RestartAllSessions), RestartAllSessions);
            RegisterMethod(nameof(EnumerateDevices), EnumerateDevices);

            RegisterMethod(nameof(OpenTapStream), OpenTapStream);
            RegisterMethod(nameof(CloseTapStream), CloseTapStream);
            RegisterMethod(nameof(QueryDirectTapInfo), QueryDirectTapInfo);

            UpdatePerms(notify: true);
            if (!initiallySentSessions)
            {
                NotifySessionsChanged(null);
            }
        }

        (JToken result, bool binary) Authenticate(JToken @params)
        {
            if (!canAuthenticate)
            {
                return (false, false);
            }

            if ((string)@params["Secret"] == server.PreSharedSecret)
            {
                authenticated = true;
                UpdatePerms();

                return (true, false);
            }

            return (false, false);
        }

        (JToken result, bool binary) WatchRootProperty(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.Read);
            string name = (string)@params["Property"];
            NotifyRootPropertyChanged(name);
            subscribedRootProperties.Add(name);

            return (null, false);
        }

        (JToken result, bool binary) WatchSessionProperty(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.Read);
            string name = (string)@params["Property"];
            int? sessionId = (int?)@params["Session"];
            if (sessionId.HasValue)
            {
                SessionModel session = model.GetSession(sessionId.GetValueOrDefault()) ?? throw new RemoteException(UnknownObject, "Unknown session");
                if (!session.Valid)
                {
                    RequirePermissions(WebSocketPermissions.ConfigureSessions);
                }
                NotifySessionPropertyChanged(session, name);
                if (!globallySubscribedSessionProperties.Contains(name))
                {
                    if (!subscribedSessionProperties.TryGetValue(session, out ISet<string> props))
                    {
                        props = new HashSet<string>();
                        subscribedSessionProperties.Add(session, props);
                    }
                    props.Add(name);
                }
            }
            else
            {
                foreach (SessionModel session in sessions.Value)
                {
                    NotifySessionPropertyChanged(session, name);
                }
                globallySubscribedSessionProperties.Add(name);
                RemoveLocalSubscriptions(subscribedSessionProperties, name);
            }

            return (null, false);
        }

        (JToken result, bool binary) WatchChannelProperty(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.Read);
            string name = (string)@params["Property"];
            int? sessionId = (int?)@params["Session"];
            if (sessionId.HasValue)
            {
                SessionModel session = model.GetSession(sessionId.GetValueOrDefault()) ?? throw new RemoteException(UnknownObject, "Unknown session");
                if (!session.Valid)
                {
                    RequirePermissions(WebSocketPermissions.ConfigureSessions);
                }
                foreach (ChannelModel channel in session.Channels)
                {
                    NotifyChannelPropertyChanged(session, channel, name);
                }
                if (!globallySubscribedChannelProperties.Contains(name))
                {
                    if (!subscribedChannelProperties.TryGetValue(session, out ISet<string> props))
                    {
                        props = new HashSet<string>();
                        subscribedChannelProperties.Add(session, props);
                    }
                    props.Add(name);
                }
            }
            else
            {
                foreach (SessionModel session in sessions.Value)
                {
                    foreach (ChannelModel channel in session.Channels)
                    {
                        NotifyChannelPropertyChanged(session, channel, name);
                    }
                }
                globallySubscribedChannelProperties.Add(name);
                RemoveLocalSubscriptions(subscribedChannelProperties, name);
            }

            return (null, false);
        }

        (JToken result, bool binary) UnwatchRootProperty(JToken @params)
        {
            string name = (string)@params["Property"];
            subscribedRootProperties.Remove(name);

            return (null, false);
        }

        (JToken result, bool binary) UnwatchSessionProperty(JToken @params)
        {
            string name = (string)@params["Property"];
            int? sessionId = (int?)@params["Session"];
            if (sessionId.HasValue)
            {
                SessionModel session = model.GetSession(sessionId.GetValueOrDefault());
                if (null != session)
                {
                    if (subscribedSessionProperties.TryGetValue(session, out ISet<string> props))
                    {
                        props.Remove(name);
                        if (props.Count == 0)
                        {
                            subscribedSessionProperties.Remove(session);
                        }
                    }
                }
            }
            else
            {
                globallySubscribedSessionProperties.Remove(name);
                RemoveLocalSubscriptions(subscribedSessionProperties, name);
            }

            return (null, false);
        }

        (JToken result, bool binary) UnwatchChannelProperty(JToken @params)
        {
            string name = (string)@params["Property"];
            int? sessionId = (int?)@params["Session"];
            if (sessionId.HasValue)
            {
                SessionModel session = model.GetSession(sessionId.GetValueOrDefault());
                if (null != session)
                {
                    if (subscribedChannelProperties.TryGetValue(session, out ISet<string> props))
                    {
                        props.Remove(name);
                        if (props.Count == 0)
                        {
                            subscribedChannelProperties.Remove(session);
                        }
                    }
                }
            }
            else
            {
                globallySubscribedChannelProperties.Remove(name);
                RemoveLocalSubscriptions(subscribedChannelProperties, name);
            }

            return (null, false);
        }

        static void RemoveLocalSubscriptions(Dictionary<SessionModel, ISet<string>> subscribedProperties, string property)
        {
            ISet<SessionModel> toClean = new HashSet<SessionModel>();
            foreach (KeyValuePair<SessionModel, ISet<string>> entry in subscribedProperties)
            {
                entry.Value.Remove(property);
                if (entry.Value.Count == 0)
                {
                    toClean.Add(entry.Key);
                }
            }
            foreach (SessionModel session in toClean)
            {
                subscribedProperties.Remove(session);
            }
        }

        (JToken result, bool binary) SetRootProperty(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.WriteProperties);
            MethodInfo setter = GetSetter(RootProperties, (string)@params["Property"]);
            setter.Invoke(model, new object[] { @params["Value"].ToObject(setter.GetParameters()[0].ParameterType) });

            return (null, false);
        }

        (JToken result, bool binary) SetSessionProperty(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.WriteProperties);
            SessionModel session = model.GetSession((int)@params["Session"]) ?? throw new RemoteException(UnknownObject, "Unknown session");
            MethodInfo setter = GetSetter(SessionProperties, (string)@params["Property"]);
            setter.Invoke(session, new object[] { @params["Value"].ToObject(setter.GetParameters()[0].ParameterType) });

            return (null, false);
        }

        (JToken result, bool binary) SetChannelProperty(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.WriteProperties);
            SessionModel session = model.GetSession((int)@params["Session"]) ?? throw new RemoteException(UnknownObject, "Unknown session");
            ChannelModel channel = session.GetChannel((Channel)(int)@params["Channel"]) ?? throw new RemoteException(UnknownObject, "Unknown channel");
            MethodInfo setter = GetSetter(ChannelProperties, (string)@params["Property"]);
            setter.Invoke(channel, new object[] { @params["Value"].ToObject(setter.GetParameters()[0].ParameterType) });

            return (null, false);
        }

        (JToken result, bool binary) AddSession(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.ConfigureSessions);

            SessionSettings settings = SessionSettings.CreateNew();

            if (null != @params["SampleRate"])
            {
                settings.SampleRate = (int?)@params["SampleRate"];
            }
            if (null != @params["Channels"])
            {
                settings.Channels = (Channel)(int)@params["Channels"];
            }
            if (null != @params["Source"])
            {
                settings.Source = @params["Source"].ToObject<WASSourceSettings>();
            }
            if (null != @params["WASSink"])
            {
                settings.WASSink = @params["WASSink"].ToObject<WASSinkSettings>();
            }
            if (null != @params["NetworkSink"])
            {
                settings.NetworkSink = @params["NetworkSink"].ToObject<NetworkSinkSettings>();
            }

            SessionModel session = Program.CreateSessionModel(settings);
            model.AddSession(session);

            return (session.Id, false);
        }

        (JToken result, bool binary) RemoveSession(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.ConfigureSessions);

            SessionModel session = model.GetSession((int)@params["Session"]) ?? throw new RemoteException(UnknownObject, "Unknown session");
            model.RemoveSession(session);

            return (null, false);
        }

        (JToken result, bool binary) QuerySessionConfiguration(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.ConfigureSessions);

            SessionModel session = model.GetSession((int)@params["Session"]) ?? throw new RemoteException(UnknownObject, "Unknown session");
            SessionSettings settings = session.Settings;

            JObject result = new JObject();
            if (settings.ShouldSerializeSampleRate())
            {
                result["SampleRate"] = settings.SampleRate;
            }
            if (settings.ShouldSerializeChannels())
            {
                result["Channels"] = (int)settings.Channels;
            }
            if (settings.ShouldSerializeSource())
            {
                result["Source"] = JToken.FromObject(settings.Source);
            }
            if (settings.ShouldSerializeWASSink())
            {
                result["WASSink"] = JToken.FromObject(settings.WASSink);
            }
            if (settings.ShouldSerializeNetworkSink())
            {
                result["NetworkSink"] = JToken.FromObject(settings.NetworkSink);
            }

            return (result, false);
        }

        (JToken result, bool binary) ConfigureSession(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.ConfigureSessions);

            SessionModel session = model.GetSession((int)@params["Session"]) ?? throw new RemoteException(UnknownObject, "Unknown session");
            SessionSettings settings = session.Settings;

            if (null != @params["SampleRate"])
            {
                settings.SampleRate = (int?)@params["SampleRate"];
            }
            if (null != @params["Channels"])
            {
                settings.Channels = (Channel)(int)@params["Channels"];
            }
            if (null != @params["Source"])
            {
                settings.Source = @params["Source"].ToObject<WASSourceSettings>();
            }
            if (null != @params["WASSink"])
            {
                settings.WASSink = @params["WASSink"].ToObject<WASSinkSettings>();
            }
            if (null != @params["NetworkSink"])
            {
                settings.NetworkSink = @params["NetworkSink"].ToObject<NetworkSinkSettings>();
            }

            model.Settings.Dirty = true;
            settings.RestartPending = true;
            sessionManager.RestartPending();

            return (null, false);
        }

        (JToken result, bool binary) SetSessionPosition(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.ConfigureSessions);

            SessionModel session = model.GetSession((int)@params["Session"]) ?? throw new RemoteException(UnknownObject, "Unknown session");
            model.SetSessionPosition(session, (int)@params["Position"], ((bool?)@params["Relative"]).GetValueOrDefault());

            return (null, false);
        }

        (JToken result, bool binary) RestartSession(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.ConfigureSessions);

            SessionModel session = model.GetSession((int)@params["Session"]) ?? throw new RemoteException(UnknownObject, "Unknown session");
            session.Settings.RestartPending = true;
            sessionManager.RestartPending();

            return (null, false);
        }

        (JToken result, bool binary) RestartAllSessions(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.ConfigureSessions);

            sessionManager.RestartAll();

            return (null, false);
        }

        async Task<(JToken result, bool binary)> EnumerateDevices(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.ConfigureSessions);

            await sessionManager.RefreshDevices();

            return (JToken.FromObject(sessionManager.Devices), false);
        }

        (JToken result, bool binary) OpenTapStream(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.TapStream);

            SessionModel session = model.GetSession((int)@params["Session"]) ?? throw new RemoteException(UnknownObject, "Unknown session");
            lock (tapSessions)
            {
                if (tapSessions.Add(session))
                {
                    session.TapData += Session_TapData;
                }
            }

            return (null, false);
        }

        (JToken result, bool binary) CloseTapStream(JToken @params)
        {
            SessionModel session = model.GetSession((int)@params["Session"]) ?? throw new RemoteException(UnknownObject, "Unknown session");
            session.TapData -= Session_TapData;
            lock (tapSessions)
            {
                tapSessions.Remove(session);
            }

            return (null, false);
        }

        (JToken result, bool binary) QueryDirectTapInfo(JToken @params)
        {
            RequirePermissions(WebSocketPermissions.Read | WebSocketPermissions.WriteProperties | WebSocketPermissions.TapStream);
            if (!Context.IsLocal)
            {
                throw new RemoteException(PermissionDenied, "Permission denied");
            }

            SessionModel session = model.GetSession((int)@params["Session"]) ?? throw new RemoteException(UnknownObject, "Unknown session");

            JObject result = new JObject
            {
                ["SharedMemoryName"] = session.ControlStructure.Name,
                ["TapOffset"] = session.ControlStructure.TapOffset,
                ["TapWriteCursorOffset"] = ControlStructure.TapWriteCursorOffset.ToInt64(),
                ["TapCapacityOffset"] = ControlStructure.TapCapacityOffset.ToInt64(),
                ["SampleRateOffset"] = ControlStructure.SampleRateOffset.ToInt64(),
                ["ChannelMaskOffset"] = ControlStructure.ChannelMaskOffset.ToInt64()
            };

            return (result, false);
        }

        void Session_TapData(object sender, TapDataEventArgs e)
        {
            SessionModel session = (SessionModel)sender;

            Notify("TapData", new JObject
            {
                ["Session"] = session.Id,
                ["Data"] = e.Data,
            }, true);
        }

        public void OnSecurityChanged(bool pssChanged, bool permsChanged)
        {
            if (pssChanged && authenticated)
            {
                UpdatePerms(deauth: true);
            }
            else if (permsChanged)
            {
                UpdatePerms();
            }
        }

        public void OnRootPropertyChanged(PropertyChangedEventArgs e)
        {
            if (RootPropertyNames.TryGetValue(e.PropertyName, out string name))
            {
                if (subscribedRootProperties.Contains(name))
                {
                    NotifyRootPropertyChanged(name);
                }
            }

            switch (e.PropertyName)
            {
                case nameof(RootModel.Sessions):
                case nameof(RootModel.ValidSessions):
                    sessions.Update();
                    break;
            }
        }

        public void OnSessionPropertyChanged(SessionPropertyChangedEventArgs e)
        {
            if (SessionPropertyNames.TryGetValue(e.PropertyName, out string name))
            {
                if (globallySubscribedSessionProperties.Contains(name) || subscribedSessionProperties.TryGetValue(e.Session, out ISet<string> props) && props.Contains(name))
                {
                    NotifySessionPropertyChanged(e.Session, name);
                }
            }

            switch (e.PropertyName)
            {
                case nameof(SessionModel.Channels):
                    if (e.EventArgs is CollectionPropertyChangedEventArgs<ChannelModel> innerE)
                    {
                        ISet<string> channelProps = new HashSet<string>(globallySubscribedChannelProperties);
                        if (subscribedChannelProperties.TryGetValue(e.Session, out ISet<string> props))
                        {
                            channelProps.UnionWith(props);
                        }
                        if (channelProps.Count > 0)
                        {
                            foreach (ChannelModel ch in innerE.Added)
                            {
                                foreach (string chPropName in channelProps)
                                {
                                    NotifyChannelPropertyChanged(e.Session, ch, chPropName);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public void OnChannelPropertyChanged(ChannelPropertyChangedEventArgs e)
        {
            if (ChannelPropertyNames.TryGetValue(e.PropertyName, out string name))
            {
                if (globallySubscribedChannelProperties.Contains(name) || subscribedChannelProperties.TryGetValue(e.Session, out ISet<string> props) && props.Contains(name))
                {
                    NotifyChannelPropertyChanged(e.Session, e.Channel, name);
                }
            }
        }

        static (Dictionary<string, (MethodInfo, MethodInfo)>, Dictionary<string, string>) GetProperties(Type t)
        {
            Dictionary<string, (MethodInfo, MethodInfo)> props = new Dictionary<string, (MethodInfo, MethodInfo)>();
            Dictionary<string, string> names = new Dictionary<string, string>();
            foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                WebSocketExposedAttribute exposed = prop.GetCustomAttribute<WebSocketExposedAttribute>();
                if (null == exposed)
                {
                    continue;
                }
                props.Add(exposed.Name ?? prop.Name, (prop.GetGetMethod(), exposed.Writable ? prop.GetSetMethod() : null));
                names.Add(prop.Name, exposed.Name ?? prop.Name);
            }

            return (props, names);
        }

        static WebSocketPermissions CalculateIntents(HttpListenerWebSocketContext ctx)
        {
            WebSocketPermissions intents = WebSocketPermissions.All;
            if (!string.IsNullOrWhiteSpace(ctx.RequestUri.Query))
            {
                NameValueCollection query = HttpUtility.ParseQueryString(ctx.RequestUri.Query);
                foreach (string intentsParam in query.GetValues("intents") ?? new string[0])
                {
                    if (int.TryParse(intentsParam, out int intentsMask))
                    {
                        intents &= (WebSocketPermissions)intentsMask;
                    }
                }
            }
            foreach (string intentsHeader in ctx.Headers.GetValues("EtherSound-Intents") ?? new string[0])
            {
                if (int.TryParse(intentsHeader, out int intentsMask))
                {
                    intents &= (WebSocketPermissions)intentsMask;
                }
            }

            return intents;
        }

        void UpdatePerms(bool deauth = false, bool notify = false)
        {
            if (deauth)
            {
                authenticated = false;
            }
            WebSocketPermissions perms = server.GlobalPermissions & intents;
            bool canAuthenticate = false;
            if (!Context.IsLocal)
            {
                perms &= server.NetworkPermissions;
            }
            if (!authenticated)
            {
                canAuthenticate = null != server.PreSharedSecret && (perms & server.UnauthenticatedPermissions) != perms;
                perms &= server.UnauthenticatedPermissions;
            }
            if (this.perms != perms || this.canAuthenticate != canAuthenticate)
            {
                this.canAuthenticate = canAuthenticate;
                this.perms = perms;
                notify = true;
            }
            if (notify)
            {
                NotifyPermissionsChanged();
            }
            if ((perms & WebSocketPermissions.Read) == 0)
            {
                subscribedRootProperties.Clear();
                globallySubscribedSessionProperties.Clear();
                globallySubscribedChannelProperties.Clear();
                subscribedSessionProperties.Clear();
                subscribedChannelProperties.Clear();
            }
            if ((perms & WebSocketPermissions.TapStream) == 0)
            {
                lock (tapSessions)
                {
                    foreach (SessionModel session in tapSessions)
                    {
                        session.TapData -= Session_TapData;
                    }
                    tapSessions.Clear();
                }
            }
            sessions.Update();
        }

        void NotifySessionsChanged(ISet<SessionModel> newSessions)
        {
            initiallySentSessions = true;

            Notify("SessionsChanged", new JObject
            {
                ["Ids"] = new JArray(Array.ConvertAll(sessions.Value, s => (object)s.Id)),
            });

            if (null != newSessions)
            {
                foreach (SessionModel session in newSessions)
                {
                    foreach (string property in globallySubscribedSessionProperties)
                    {
                        NotifySessionPropertyChanged(session, property);
                    }

                    foreach (string property in globallySubscribedChannelProperties)
                    {
                        foreach (ChannelModel channel in session.Channels)
                        {
                            NotifyChannelPropertyChanged(session, channel, property);
                        }
                    }
                }
            }
        }

        void NotifyRootPropertyChanged(string property)
        {
            Notify("RootPropertyChanged", new JObject
            {
                ["Property"] = property,
                ["Value"] = JToken.FromObject(GetGetter(RootProperties, property).Invoke(model, new object[0])),
            });
        }

        void NotifySessionPropertyChanged(SessionModel session, string property)
        {
            Notify("SessionPropertyChanged", new JObject
            {
                ["Session"] = session.Id,
                ["Property"] = property,
                ["Value"] = JToken.FromObject(GetGetter(SessionProperties, property).Invoke(session, new object[0])),
            });
        }

        void NotifyChannelPropertyChanged(SessionModel session, ChannelModel channel, string property)
        {
            Notify("ChannelPropertyChanged", new JObject
            {
                ["Session"] = session.Id,
                ["Channel"] = (int)channel.Id,
                ["Property"] = property,
                ["Value"] = JToken.FromObject(GetGetter(ChannelProperties, property).Invoke(channel, new object[0])),
            });
        }

        void NotifyPermissionsChanged()
        {
            Notify("PermissionsChanged", new JObject
            {
                ["Permissions"] = (int)perms,
                ["CanAuthenticate"] = canAuthenticate,
            });
        }

        void RequirePermissions(WebSocketPermissions permissions)
        {
            if ((permissions & perms) != permissions)
            {
                throw new RemoteException(PermissionDenied, "Permission denied");
            }
        }

        static MethodInfo GetGetter(Dictionary<string, (MethodInfo, MethodInfo)> properties, string propertyName)
        {
            return GetProperty(properties, propertyName).Item1 ?? throw new RemoteException(InvalidPropertyAccess, "Property not readable");
        }

        static MethodInfo GetSetter(Dictionary<string, (MethodInfo, MethodInfo)> properties, string propertyName)
        {
            return GetProperty(properties, propertyName).Item2 ?? throw new RemoteException(InvalidPropertyAccess, "Property not writable");
        }

        static (MethodInfo, MethodInfo) GetProperty(Dictionary<string, (MethodInfo, MethodInfo)> properties, string propertyName)
        {
            if (properties.TryGetValue(propertyName, out (MethodInfo, MethodInfo) property))
            {
                return property;
            }
            else
            {
                throw new RemoteException(UnknownProperty, "Unknown property");
            }
        }

        protected override void OnDispose()
        {
            lock (tapSessions)
            {
                foreach (SessionModel session in tapSessions)
                {
                    session.TapData -= Session_TapData;
                }
                tapSessions.Clear();
            }
            onDisposed?.Invoke();
        }
    }
}
