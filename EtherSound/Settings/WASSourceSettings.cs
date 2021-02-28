using WASCap;

namespace EtherSound.Settings
{
    class WASSourceSettings
    {
        public string Id { get; set; }

        public string FriendlyName { get; set; }

        public DataFlow Flow { get; set; } = DataFlow.Render;

        public Role Role { get; set; } = Role.Console;

        public WASSourceSettings()
        {
        }

        public WASSourceSettings(DataFlow flow = DataFlow.Render, Role role = Role.Console) : this()
        {
            Flow = flow;
            Role = role;
        }

        public WASSourceSettings(string id, string friendlyName = null, DataFlow flow = DataFlow.All) : this()
        {
            Id = id;
            FriendlyName = friendlyName;
            Flow = flow;
        }

        public bool ShouldSerialize()
        {
            return ShouldSerializeId() || ShouldSerializeFriendlyName() || ShouldSerializeFlow() || ShouldSerializeRole();
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

        public bool ShouldSerializeFlow()
        {
            return Flow != DataFlow.Render;
        }

        public bool ShouldSerializeRole()
        {
            return Id == null && Role != Role.Console;
        }
        #endregion

        public CaptureParameters.SourceInfo ToSourceInfo(Device[] devices, out bool changed)
        {
            if (null != Id)
            {
                Device device = SettingsHelper.Resolve(devices, Id, FriendlyName, Flow);
                if (null == device)
                {
                    changed = false;

                    return null;
                }

                changed = device.Id != Id || device.FriendlyName != FriendlyName || device.Flow != Flow;
                if (changed)
                {
                    Id = device.Id;
                    FriendlyName = device.FriendlyName;
                    Flow = device.Flow;
                }

                return new CaptureParameters.DeviceWASSourceInfo(device.Id);
            }

            changed = false;

            return new CaptureParameters.DefaultDeviceWASSourceInfo(Flow, Role);
        }
    }
}
