using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using EChannel = WASCap.Channel;

namespace WASCap
{
    public class ControlStructure
    {
        private const int InitializedFlag = 1;
        private const int EnabledFlag = 2;
        private const int AbortRequestedFlag = 4;

        public const int MaxChannels = 32;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct ChannelVolumeArray
        {
            public float FrontLeft;
            public float FrontRight;
            public float FrontCenter;
            public float LowFrequency;
            public float BackLeft;
            public float BackRight;
            public float FrontLeftOfCenter;
            public float FrontRightOfCenter;
            public float BackCenter;
            public float SideLeft;
            public float SideRight;
            public float TopCenter;
            public float TopFrontLeft;
            public float TopFrontCenter;
            public float TopFrontRight;
            public float TopBackLeft;
            public float TopBackCenter;
            public float TopBackRight;
            float Channel18;
            float Channel19;
            float Channel20;
            float Channel21;
            float Channel22;
            float Channel23;
            float Channel24;
            float Channel25;
            float Channel26;
            float Channel27;
            float Channel28;
            float Channel29;
            float Channel30;
            float Channel31;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct ShmContents
        {
            public int Flags;
            public int TapOffset;
            public int TapWriteCursor;
            public int TapCapacity;
            public float MasterVolume;
            public ChannelVolumeArray ChannelVolumes;
            public float SaturationThreshold;
            public float SilenceThreshold;
            public float AveragingWeight;
            public float SaturationDebounceFactor;
            public float SaturationRecoveryFactor;
            public float SaturationDebounceVolume;
            public float SaturationEffectiveVolume;
            public int SampleRate;
            public int ChannelMask;
            public long LastFrameTickCount;
            public float LastFrameMaxAmplitude;
        }

        static readonly Dictionary<EChannel, ChannelFactory> channelFactories;

        MemoryMappedFile shm;
        MemoryMappedViewAccessor shmAccessor;
        SafeBuffer shmBuffer;
        unsafe ShmContents* shmBlock;
        int cachedChannelMask;
        Channel[] cachedChannels;

        public string Name { get; }

        public bool Initialized
        {
            get => TestFlags(InitializedFlag);
            set => SetFlags(InitializedFlag, value);
        }

        public bool Enabled
        {
            get => TestFlags(EnabledFlag);
            set => SetFlags(EnabledFlag, value);
        }

        internal bool AbortRequested
        {
            get => TestFlags(AbortRequestedFlag);
            set => SetFlags(AbortRequestedFlag, value);
        }

        public unsafe int TapWriteCursor
        {
            get => shmBlock->TapWriteCursor;
        }

        public unsafe int TapCapacity
        {
            get => shmBlock->TapCapacity;
        }

        public unsafe float MasterVolume
        {
            get => shmBlock->MasterVolume;
            set => shmBlock->MasterVolume = value;
        }

        public unsafe float FrontLeftVolume
        {
            get => shmBlock->ChannelVolumes.FrontLeft;
            set => shmBlock->ChannelVolumes.FrontLeft = value;
        }

        public unsafe float FrontRightVolume
        {
            get => shmBlock->ChannelVolumes.FrontRight;
            set => shmBlock->ChannelVolumes.FrontRight = value;
        }

        public unsafe float FrontCenterVolume
        {
            get => shmBlock->ChannelVolumes.FrontCenter;
            set => shmBlock->ChannelVolumes.FrontCenter = value;
        }

        public unsafe float LowFrequencyVolume
        {
            get => shmBlock->ChannelVolumes.LowFrequency;
            set => shmBlock->ChannelVolumes.LowFrequency = value;
        }

        public unsafe float BackLeftVolume
        {
            get => shmBlock->ChannelVolumes.BackLeft;
            set => shmBlock->ChannelVolumes.BackLeft = value;
        }

        public unsafe float BackRightVolume
        {
            get => shmBlock->ChannelVolumes.BackRight;
            set => shmBlock->ChannelVolumes.BackRight = value;
        }

        public unsafe float FrontLeftOfCenterVolume
        {
            get => shmBlock->ChannelVolumes.FrontLeftOfCenter;
            set => shmBlock->ChannelVolumes.FrontLeftOfCenter = value;
        }

        public unsafe float FrontRightOfCenterVolume
        {
            get => shmBlock->ChannelVolumes.FrontRightOfCenter;
            set => shmBlock->ChannelVolumes.FrontRightOfCenter = value;
        }

        public unsafe float BackCenterVolume
        {
            get => shmBlock->ChannelVolumes.BackCenter;
            set => shmBlock->ChannelVolumes.BackCenter = value;
        }

        public unsafe float SideLeftVolume
        {
            get => shmBlock->ChannelVolumes.SideLeft;
            set => shmBlock->ChannelVolumes.SideLeft = value;
        }

        public unsafe float SideRightVolume
        {
            get => shmBlock->ChannelVolumes.SideRight;
            set => shmBlock->ChannelVolumes.SideRight = value;
        }

        public unsafe float TopCenterVolume
        {
            get => shmBlock->ChannelVolumes.TopCenter;
            set => shmBlock->ChannelVolumes.TopCenter = value;
        }

        public unsafe float TopFrontLeftVolume
        {
            get => shmBlock->ChannelVolumes.TopFrontLeft;
            set => shmBlock->ChannelVolumes.TopFrontLeft = value;
        }

        public unsafe float TopFrontCenterVolume
        {
            get => shmBlock->ChannelVolumes.TopFrontCenter;
            set => shmBlock->ChannelVolumes.TopFrontCenter = value;
        }

        public unsafe float TopFrontRightVolume
        {
            get => shmBlock->ChannelVolumes.TopFrontRight;
            set => shmBlock->ChannelVolumes.TopFrontRight = value;
        }

        public unsafe float TopBackLeftVolume
        {
            get => shmBlock->ChannelVolumes.TopBackLeft;
            set => shmBlock->ChannelVolumes.TopBackLeft = value;
        }

        public unsafe float TopBackCenterVolume
        {
            get => shmBlock->ChannelVolumes.TopBackCenter;
            set => shmBlock->ChannelVolumes.TopBackCenter = value;
        }

        public unsafe float TopBackRightVolume
        {
            get => shmBlock->ChannelVolumes.TopBackRight;
            set => shmBlock->ChannelVolumes.TopBackRight = value;
        }

        public unsafe float SaturationThreshold
        {
            get => shmBlock->SaturationThreshold;
            set => shmBlock->SaturationThreshold = value;
        }

        public unsafe float SilenceThreshold
        {
            get => shmBlock->SilenceThreshold;
            set => shmBlock->SilenceThreshold = value;
        }

        public unsafe float AveragingWeight
        {
            get => shmBlock->AveragingWeight;
            set => shmBlock->AveragingWeight = value;
        }

        public unsafe float SaturationDebounceFactor
        {
            get => shmBlock->SaturationDebounceFactor;
            set => shmBlock->SaturationDebounceFactor = value;
        }

        public unsafe float SaturationRecoveryFactor
        {
            get => shmBlock->SaturationRecoveryFactor;
            set => shmBlock->SaturationRecoveryFactor = value;
        }

        public unsafe float SaturationDebounceVolume
        {
            get => shmBlock->SaturationDebounceVolume;
            set => shmBlock->SaturationDebounceVolume = value;
        }

        public unsafe float SaturationEffectiveVolume
        {
            get => shmBlock->SaturationEffectiveVolume;
            set => shmBlock->SaturationEffectiveVolume = value;
        }

        public unsafe int SampleRate
        {
            get => shmBlock->SampleRate;
        }

        public unsafe EChannel ChannelMask
        {
            get => (EChannel)shmBlock->ChannelMask;
        }

        public unsafe long LastFrameTickCount
        {
            get => shmBlock->LastFrameTickCount;
            set => shmBlock->LastFrameTickCount = value;
        }

        public unsafe float LastFrameMaxAmplitude
        {
            get => shmBlock->LastFrameMaxAmplitude;
            set => shmBlock->LastFrameMaxAmplitude = value;
        }

        public unsafe Channel[] Channels
        {
            get
            {
                int channelMask = shmBlock->ChannelMask;
                if (channelMask != cachedChannelMask)
                {
                    List<Channel> channels = new List<Channel>();
                    int remainingChannels = channelMask;
                    while (0 != remainingChannels)
                    {
                        EChannel channel = (EChannel)(remainingChannels & ~(remainingChannels - 1));
                        channels.Add(Array.Find(cachedChannels, ch => ch.Id == channel)
                            ?? channelFactories[channel].Create(this));
                        remainingChannels &= remainingChannels - 1;
                    }

                    cachedChannels = channels.ToArray();
                    cachedChannelMask = channelMask;
                }

                return cachedChannels;
            }
        }

        static ControlStructure()
        {
            channelFactories = new Dictionary<EChannel, ChannelFactory>();
            foreach (EChannel channel in (EChannel[])Enum.GetValues(typeof(EChannel)))
            {
                channelFactories[channel] = new ChannelFactory(channel);
            }
        }

        public unsafe ControlStructure(string name)
        {
            int shmSize = sizeof(ShmContents) + (8 << 20);
            int pageSize = Environment.SystemPageSize;
            if (shmSize % pageSize > 0)
            {
                shmSize += pageSize - shmSize % pageSize;
            }

            Name = name;
            shm = MemoryMappedFile.CreateOrOpen(name, shmSize, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, null, HandleInheritability.None);
            shmAccessor = shm.CreateViewAccessor(0, shmSize, MemoryMappedFileAccess.ReadWrite);
            shmBuffer = shmAccessor.SafeMemoryMappedViewHandle;
            shmBlock = null;
            fixed (ShmContents** ptrToShmBlockField = &shmBlock)
            {
                shmBuffer.AcquirePointer(ref *(byte**)ptrToShmBlockField);
            }
            if (0 == shmBlock->TapCapacity)
            {
                shmBlock->TapOffset = sizeof(ShmContents);
                shmBlock->TapWriteCursor = 0;
                shmBlock->TapCapacity = shmSize - sizeof(ShmContents);
            }
            cachedChannelMask = 0;
            cachedChannels = new Channel[0];
        }

        ~ControlStructure()
        {
            shmBuffer.ReleasePointer();
        }

        unsafe bool TestFlags(int mask)
        {
            return (shmBlock->Flags & mask) == mask;
        }

        unsafe void SetFlags(int mask, bool value)
        {
            int originalFlags = shmBlock->Flags, flags;
            do
            {
                if ((originalFlags & mask) == (value ? mask : 0))
                {
                    break;
                }
                if (value)
                {
                    flags = originalFlags | mask;
                }
                else
                {
                    flags = originalFlags & ~mask;
                }
            } while (originalFlags != (originalFlags = Interlocked.CompareExchange(ref shmBlock->Flags, flags, originalFlags)));
        }

        public Stream OpenTapStream()
        {
            return new TapStream(this);
        }

        public class Channel
        {
            readonly Func<float> volumeGetter;
            readonly Action<float> volumeSetter;

            public EChannel Id { get; }

            public float Volume
            {
                get => volumeGetter();
                set => volumeSetter(value);
            }

            public Channel(EChannel id, Func<float> volumeGetter, Action<float> volumeSetter)
            {
                Id = id;
                this.volumeGetter = volumeGetter;
                this.volumeSetter = volumeSetter;
            }
        }

        private class ChannelFactory
        {
            readonly MethodInfo volumeGetter;
            readonly MethodInfo volumeSetter;

            public EChannel ID { get; }

            public ChannelFactory(EChannel id)
            {
                ID = id;
                PropertyInfo volume = typeof(ControlStructure).GetProperty(id.ToString() + "Volume", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                volumeGetter = volume.GetGetMethod();
                volumeSetter = volume.GetSetMethod();
            }

            public Channel Create(ControlStructure shm)
            {
                return new Channel(ID, (Func<float>)volumeGetter.CreateDelegate(typeof(Func<float>), shm), (Action<float>)volumeSetter.CreateDelegate(typeof(Action<float>), shm));
            }
        }

        private class TapStream : Stream
        {
            readonly ControlStructure owner;
            readonly unsafe byte* buffer;
            readonly int capacity;
            int readCursor;

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public unsafe TapStream(ControlStructure owner)
            {
                this.owner = owner;
                buffer = ((byte*)owner.shmBlock) + owner.shmBlock->TapOffset;
                capacity = owner.shmBlock->TapCapacity;
                readCursor = owner.shmBlock->TapWriteCursor;
            }

            public override void Flush()
            {
            }

            public override unsafe int Read(byte[] buffer, int offset, int count)
            {
                if (null == buffer)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                if (offset < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }
                if (offset + count > buffer.Length)
                {
                    throw new ArgumentException();
                }
                int readCount = 0;
                while (count > 0)
                {
                    int writeCursor = owner.shmBlock->TapWriteCursor;
                    if (writeCursor == readCursor)
                    {
                        break;
                    }
                    int segmentLength = Math.Min(count, (readCursor < writeCursor) ? (writeCursor - readCursor) : (capacity - readCursor));
                    Marshal.Copy(new IntPtr(this.buffer + readCursor), buffer, offset, segmentLength);
                    readCursor = (readCursor + segmentLength) % capacity;
                    offset += segmentLength;
                    count -= segmentLength;
                    readCount += segmentLength;
                }

                return readCount;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
