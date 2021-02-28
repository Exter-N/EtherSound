using WASCap;

namespace EtherSound.Settings
{
    class NetworkSinkSettings
    {
        public string BindAddress { get; set; }

        public string PeerAddress { get; set; }

        public string PeerService { get; set; }

        public CaptureParameters.NetworkSinkInfo ToNetworkSinkInfo(NetworkSinkSettings defaults = null)
        {
            return new CaptureParameters.NetworkSinkInfo
            {
                BindAddress = BindAddress ?? defaults?.BindAddress,
                PeerAddress = PeerAddress ?? defaults?.PeerAddress,
                PeerService = PeerService ?? defaults?.PeerService,
            };
        }

        public bool ShouldSerialize()
        {
            return ShouldSerializeBindAddress() || ShouldSerializePeerAddress() || ShouldSerializePeerService();
        }

        #region Newtonsoft.Json serialization control
        public bool ShouldSerializeBindAddress()
        {
            return BindAddress != null;
        }

        public bool ShouldSerializePeerAddress()
        {
            return PeerAddress != null;
        }

        public bool ShouldSerializePeerService()
        {
            return PeerService != null;
        }
        #endregion
    }
}
