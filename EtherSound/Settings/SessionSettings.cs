using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

using WASCap;

namespace EtherSound.Settings
{
    class SessionSettings
    {
        [JsonIgnore]
        public bool RestartPending { get; set; }

        public Guid Id { get; set; }

        public int? SampleRate { get; set; } = null;
        public Channel Channels { get; set; } = 0;

        public WASSourceSettings Source { get; set; } = new WASSourceSettings();

        public WASSinkSettings WASSink { get; set; }
        public NetworkSinkSettings NetworkSink { get; set; }

        public Color Color { get; set; } = Color.FromRgb(0xFF, 0xFF, 0xFF);

        public double MaxMasterVolume { get; set; } = 1.0;

        public double MasterVolume { get; set; } = 1.0;
        public bool Muted { get; set; } = false;

        public double[] ChannelVolumes { get; set; } = new double[ControlStructure.MaxChannels];

        public double SaturationThreshold { get; set; } = double.PositiveInfinity;

        public double SilenceThreshold { get; set; } = 0.0;

        public double AveragingWeight { get; set; } = 0.0;

        public double SaturationDebounceFactor { get; set; } = 1.0;

        public double SaturationRecoveryFactor { get; set; } = double.PositiveInfinity;

        public SessionSettings()
        {
            for (int i = 0; i < ControlStructure.MaxChannels; ++i)
            {
                ChannelVolumes[i] = 1.0;
            }
        }

        public static SessionSettings CreateNew()
        {
            return new SessionSettings()
            {
                Id = Guid.NewGuid(),
            };
        }

        #region Newtonsoft.Json serialization control
        public bool ShouldSerializeSampleRate()
        {
            return SampleRate.HasValue;
        }
        
        public bool ShouldSerializeChannels()
        {
            return Channels != 0;
        }

        public bool ShouldSerializeSource()
        {
            return Source != null && Source.ShouldSerialize();
        }

        public bool ShouldSerializeWASSink()
        {
            return WASSink != null;
        }

        public bool ShouldSerializeNetworkSink()
        {
            return NetworkSink != null;
        }

        public bool ShouldSerializeColor()
        {
            return Color != Color.FromRgb(0xFF, 0xFF, 0xFF);
        }

        public bool ShouldSerializeMaxMasterVolume()
        {
            return MaxMasterVolume != 1.0;
        }

        public bool ShouldSerializeMasterVolume()
        {
            return MasterVolume != 1.0;
        }

        public bool ShouldSerializeMuted()
        {
            return Muted;
        }

        public bool ShouldSerializeChannelVolumes()
        {
            return ChannelVolumes != null && (ChannelVolumes.Length != 32 || !Array.TrueForAll(ChannelVolumes, vol => vol == 1.0));
        }

        public bool ShouldSerializeSaturationThreshold()
        {
            return SaturationThreshold != double.PositiveInfinity;
        }

        public bool ShouldSerializeSilenceThreshold()
        {
            return SilenceThreshold != 0.0;
        }

        public bool ShouldSerializeAveragingWeight()
        {
            return AveragingWeight != 0.0;
        }

        public bool ShouldSerializeSaturationDebounceFactor()
        {
            return SaturationDebounceFactor != 1.0;
        }

        public bool ShouldSerializeSaturationRecoveryFactor()
        {
            return SaturationRecoveryFactor != double.PositiveInfinity;
        }
        #endregion

        public static int GetChannelIndex(Channel channel)
        {
            return ((int)channel).CountTrailingZeros();
        }
    }
}
