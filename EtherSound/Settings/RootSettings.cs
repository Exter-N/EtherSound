using Newtonsoft.Json;
using System.Collections.Generic;

namespace EtherSound.Settings
{
    class RootSettings
    {
        [JsonIgnore]
        public bool Dirty { get; set; }

        public NetworkSinkSettings NetworkSinkDefaults { get; set; }

        public WebSocketSettings WebSocketEndpoint { get; set; }

        public List<SessionSettings> Sessions { get; set; } = new List<SessionSettings>();

        #region Newtonsoft.Json serialization control
        public bool ShouldSerializeNetworkSinkDefaults()
        {
            return NetworkSinkDefaults != null && NetworkSinkDefaults.ShouldSerialize();
        }

        public bool ShouldSerializeWebSocketEndpoint()
        {
            return WebSocketEndpoint != null && WebSocketEndpoint.ShouldSerialize();
        }

        public bool ShouldSerializeSessions()
        {
            return Sessions.Count > 0;
        }
        #endregion
    }
}
