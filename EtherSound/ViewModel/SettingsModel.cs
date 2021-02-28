using Reactivity;
using System.Collections.Generic;

namespace EtherSound.ViewModel
{
    abstract class SettingsModel : ViewModel
    {
        protected static readonly IList<IKeyedRx<SettingsModel>> Properties = new List<IKeyedRx<SettingsModel>>();

        public abstract bool Dirty
        {
            get;
        }

        string bindAddress;
        protected static readonly IWritableKeyedRx<SettingsModel, string> BindAddressProperty = Register(Properties, nameof(BindAddress), KeyedRx.Data(
            Storage<SettingsModel>.Create(key => key.bindAddress, (key, value) => key.bindAddress = value),
            null));

        public string BindAddress
        {
            get => BindAddressProperty[this];
            set => BindAddressProperty[this] = value;
        }

        string peerAddress;
        protected static readonly IWritableKeyedRx<SettingsModel, string> PeerAddressProperty = Register(Properties, nameof(PeerAddress), KeyedRx.Data(
            Storage<SettingsModel>.Create(key => key.peerAddress, (key, value) => key.peerAddress = value),
            null));

        public string PeerAddress
        {
            get => PeerAddressProperty[this];
            set => PeerAddressProperty[this] = value;
        }

        string peerService;
        protected static readonly IWritableKeyedRx<SettingsModel, string> PeerServiceProperty = Register(Properties, nameof(PeerService), KeyedRx.Data(
            Storage<SettingsModel>.Create(key => key.peerService, (key, value) => key.peerService = value),
            null));

        public string PeerService
        {
            get => PeerServiceProperty[this];
            set => PeerServiceProperty[this] = value;
        }
    }
}
