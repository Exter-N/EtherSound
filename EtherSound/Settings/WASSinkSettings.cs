using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WASCap;

namespace EtherSound.Settings
{
    class WASSinkSettings
    {
        public string Id { get; set; }

        public string FriendlyName { get; set; }

        public Role Role { get; set; } = Role.Console;

        public WASSinkSettings()
        {
        }

        public WASSinkSettings(Role role = Role.Console) : this()
        {
            Role = role;
        }

        public WASSinkSettings(string id, string friendlyName = null) : this()
        {
            Id = id;
            FriendlyName = friendlyName;
        }

        #region Newtonsoft.Json serialization control
        public bool ShouldSerializeId()
        {
            return Id != null;
        }

        public bool ShouldSerializeFriendlyName()
        {
            return FriendlyName != null;
        }

        public bool ShouldSerializeRole()
        {
            return Id == null && Role != Role.Console;
        }
        #endregion

        public CaptureParameters.WASSinkInfo ToWASSinkInfo(Device[] devices, out bool changed)
        {
            if (null != Id)
            {
                Device device = SettingsHelper.Resolve(devices, Id, FriendlyName, DataFlow.Render);
                if (null == device)
                {
                    changed = false;

                    return null;
                }

                changed = device.Id != Id || device.FriendlyName != FriendlyName;
                if (changed)
                {
                    Id = device.Id;
                    FriendlyName = device.FriendlyName;
                }

                return new CaptureParameters.DeviceWASSinkInfo(device.Id);
            }

            changed = false;

            return new CaptureParameters.DefaultDeviceWASSinkInfo(Role);
        }
    }
}
