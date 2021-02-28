using EtherSound.Settings;
using EtherSound.ViewModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using WASCap;

namespace EtherSound
{
    class SessionManager : IDisposable, IEnumerable<SessionModel>
    {
        readonly RootSettings settings;
        readonly Dictionary<SessionModel, CaptureSession> sessions;
        Device[] devices;

        public Device[] Devices => devices;

        public SessionManager(RootSettings settings)
        {
            this.settings = settings;
            sessions = new Dictionary<SessionModel, CaptureSession>();
            devices = new Device[0];
        }

        public async Task RefreshDevices()
        {
            devices = await Driver.ListDevicesAsync(DataFlow.All, DeviceState.Active);
        }

        public void Dispose()
        {
            foreach (CaptureSession session in sessions.Values)
            {
                if (null != session)
                {
                    session.Dispose();
                }
            }
            sessions.Clear();
        }

        public bool HasSession(SessionModel model)
        {
            return sessions.ContainsKey(model);
        }

        public IEnumerator<SessionModel> GetEnumerator()
        {
            return sessions.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return sessions.Keys.GetEnumerator();
        }

        public void AddSession(SessionModel model)
        {
            if (sessions.ContainsKey(model))
            {
                throw new InvalidOperationException();
            }

            sessions.Add(model, StartCapture(model));
        }

        public void RemoveSession(SessionModel model)
        {
            if (!sessions.TryGetValue(model, out CaptureSession session))
            {
                throw new InvalidOperationException();
            }

            sessions.Remove(model);
            if (null != session)
            {
                session.Dispose();
            }
        }

        public void RestartAll()
        {
            RestartSessions(_ => true);
        }

        public void RestartPending()
        {
            RestartSessions(model => model.Settings.RestartPending);
        }

        void RestartSessions(Predicate<SessionModel> predicate)
        {
            List<SessionModel> pendingRestarts = new List<SessionModel>();
            foreach (SessionModel model in sessions.Keys)
            {
                if (predicate(model))
                {
                    pendingRestarts.Add(model);
                }
            }

            foreach (SessionModel model in pendingRestarts)
            {
                RestartSession(model);
            }
        }

        void RestartSession(SessionModel model)
        {
            if (!sessions.TryGetValue(model, out CaptureSession session))
            {
                return;
            }

            if (null != session)
            {
                session.Dispose();
            }
            sessions[model] = StartCapture(model);
        }

        CaptureSession StartCapture(SessionModel model)
        {
            SessionSettings settings = model.Settings;
            settings.RestartPending = false;

            bool sourceChanged = false;
            bool sinkChanged = false;
            CaptureParameters.SourceInfo source = settings.Source?.ToSourceInfo(devices, out sourceChanged);
            CaptureParameters.WASSinkInfo wasSink = settings.WASSink?.ToWASSinkInfo(devices, out sinkChanged);
            if (sourceChanged || sinkChanged)
            {
                this.settings.Dirty = true;
            }
            model.Valid = null != source && (null != wasSink || null == settings.WASSink);

            if (source is CaptureParameters.DefaultDeviceWASSourceInfo defSource)
            {
                Device device = Array.Find(devices, dev => dev.Flow == defSource.Flow && (dev.DefaultFor & defSource.Role) != 0);
                if (null == device)
                {
                    throw new InvalidOperationException();
                }
                model.SourceName = device.FriendlyName;
                model.CanSwap = defSource.Flow == DataFlow.Render && defSource.Role == Role.Console;
            }
            else if (source is CaptureParameters.DeviceWASSourceInfo devSource)
            {
                Device device = Array.Find(devices, dev => dev.Id == devSource.DeviceId);
                if (null == device)
                {
                    throw new InvalidOperationException();
                }
                model.SourceName = device.FriendlyName;
                model.CanSwap = device.Flow == DataFlow.Render && (device.DefaultFor & Role.Console) != 0;
            }
            else
            {
                model.SourceName = "???";
            }

            if (!model.Valid)
            {
                return null;
            }

            CaptureParameters parameters = new CaptureParameters
            {
                ControlStructure = model.ControlStructure,
                SampleRate = settings.SampleRate,
                Channels = settings.Channels,
                Source = source,
                WASSink = wasSink,
                NetworkSink = settings.NetworkSink?.ToNetworkSinkInfo(this.settings.NetworkSinkDefaults),
            };

            return Driver.StartCapture(parameters, model.Console, delegate
            {
                settings.Source?.ToSourceInfo(devices, out sourceChanged);
                settings.WASSink?.ToWASSinkInfo(devices, out sinkChanged);
                if (sourceChanged || sinkChanged)
                {
                    settings.RestartPending = true;
                    this.settings.Dirty = true;
                }
                if (settings.RestartPending)
                {
                    RestartSession(model);
                }
            });
        }
    }
}
