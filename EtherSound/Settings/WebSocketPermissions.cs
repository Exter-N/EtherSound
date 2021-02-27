using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtherSound.Settings
{
    [Flags]
    enum WebSocketPermissions
    {
        Read = 1,
        WriteProperties = 2,
        ConfigureSessions = 4,
        TapStream = 8,

        All = Read | WriteProperties | ConfigureSessions | TapStream,

        DefaultGlobal = All,
        DefaultNetwork = Read | WriteProperties,
        DefaultUnauthenticated = Read | TapStream,
    }
}
