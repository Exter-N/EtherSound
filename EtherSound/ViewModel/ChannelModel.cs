using EtherSound.Settings;
using Reactivity;
using System.Collections.Generic;
using WASCap;

namespace EtherSound.ViewModel
{
    class ChannelModel : ViewModel
    {
        static readonly IList<IKeyedRx<ChannelModel>> Properties = new List<IKeyedRx<ChannelModel>>();

        readonly ControlStructure.Channel channel;
        readonly SessionSettings settings;

        public Channel Id => channel.Id;

        float cachedVolume;

        static readonly IWritableKeyedRx<ChannelModel, float> VolumeProperty = Register(Properties, nameof(Volume), KeyedRx.TwoWayBound(
            Storage<ChannelModel>.Create(key => key.cachedVolume, (key, value) => key.cachedVolume = value),
            key => key.channel.Volume,
            (key, value) => key.channel.Volume = value));

        [WebSocketExposed]
        public double Volume
        {
            get => VolumeProperty[this];
            set => VolumeProperty[this] = (float)value;
        }

        public ChannelModel(ControlStructure.Channel channel, SessionSettings settings)
        {
            this.channel = channel;
            this.settings = settings;

            Initialize(this, Properties);

            Volume = (float)settings.ChannelVolumes[SessionSettings.GetChannelIndex(channel.Id)];
        }

        protected override bool DoUpdateSettings()
        {
            bool anyChanged = false;

            double volume = channel.Volume;
            int index = SessionSettings.GetChannelIndex(channel.Id);
            if (volume != settings.ChannelVolumes[index])
            {
                settings.ChannelVolumes[index] = volume;
                anyChanged = true;
            }

            return anyChanged;
        }
    }
}
