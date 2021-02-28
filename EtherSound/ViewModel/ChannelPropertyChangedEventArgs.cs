using System.ComponentModel;

namespace EtherSound.ViewModel
{
    class ChannelPropertyChangedEventArgs : SessionPropertyChangedEventArgs
    {
        readonly ChannelModel channel;

        public ChannelModel Channel => channel;

        public ChannelPropertyChangedEventArgs(SessionModel session, ChannelModel channel, PropertyChangedEventArgs e) : base(session, e)
        {
            this.channel = channel;
        }
    }
}
