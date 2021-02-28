namespace EtherSound.Settings
{
    class WebSocketSettings
    {
        public string Uri { get; set; }

        public string PreSharedSecret { get; set; }

        public WebSocketPermissions GlobalPermissions { get; set; } = WebSocketPermissions.DefaultGlobal;

        public WebSocketPermissions NetworkPermissions { get; set; } = WebSocketPermissions.DefaultNetwork;

        public WebSocketPermissions UnauthenticatedPermissions { get; set; } = WebSocketPermissions.DefaultUnauthenticated;

        public bool ShouldSerialize()
        {
            return ShouldSerializeUri() || ShouldSerializePreSharedSecret() || ShouldSerializeGlobalPermissions() || ShouldSerializeNetworkPermissions() || ShouldSerializeUnauthenticatedPermissions();
        }

        #region Newtonsoft.Json serialization control
        public bool ShouldSerializeUri()
        {
            return null != Uri;
        }

        public bool ShouldSerializePreSharedSecret()
        {
            return null != PreSharedSecret;
        }

        public bool ShouldSerializeGlobalPermissions()
        {
            return WebSocketPermissions.DefaultGlobal != GlobalPermissions;
        }

        public bool ShouldSerializeNetworkPermissions()
        {
            return WebSocketPermissions.DefaultNetwork != NetworkPermissions;
        }

        public bool ShouldSerializeUnauthenticatedPermissions()
        {
            return WebSocketPermissions.DefaultUnauthenticated != UnauthenticatedPermissions;
        }
        #endregion
    }
}
