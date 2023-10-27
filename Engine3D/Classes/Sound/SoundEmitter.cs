using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using NVorbis;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Engine3D
{
    public class SoundEmitter
    {
        public string audioFile;

        public int _source;
        private int _buffer;

        public Vector3 Position;
        private float soundDistance = 20f;

        public SoundEmitter(string audioFile)
        {
            _buffer = AL.GenBuffer();
            _source = AL.GenSource();

            Position = Vector3.Zero;
            this.audioFile = audioFile;

            LoadBufferWithSoundData(_buffer);
            AL.Source(_source, ALSourcei.Buffer, _buffer);

            AL.Source(_source, ALSourceb.SourceRelative, true);
        }

        public SoundEmitter(string audioFile, Vector3 position)
        {
            _buffer = AL.GenBuffer();
            _source = AL.GenSource();

            Position = position;
            this.audioFile = audioFile;

            LoadBufferWithSoundData(_buffer);
            AL.Source(_source, ALSourcei.Buffer, _buffer);

            AL.Source(_source, ALSourceb.SourceRelative, false);
            AL.Source(_source, ALSource3f.Position, Position.X, Position.Y, Position.Z);
            AL.Source(_source, ALSource3f.Velocity, 0, 0, 0);

            AL.Source(_source, ALSourcef.RolloffFactor, 1.0f);  // Try increasing this if needed
            AL.Source(_source, ALSourcef.ReferenceDistance, 1.0f);  // Base distance for attenuation
            AL.Source(_source, ALSourcef.Gain, 0f);  // Source gain

        }

        public void UpdatePosition(Vector3 newPosition)
        {
            Position = newPosition;
            AL.Source(_source, ALSource3f.Position, Position.X, Position.Y, Position.Z);
        }

        public void SetSoundDistance(float dist)
        {
            soundDistance = dist;
        }

        public void UpdateGain(Vector3 listenerPosition)
        {
            ALSourceState state = (ALSourceState)AL.GetSource(_source, ALGetSourcei.SourceState);
            if (state == ALSourceState.Initial)
                return;

            float length = (listenerPosition - Position).Length;
            if (length > soundDistance)
            {
                if(state != ALSourceState.Paused)
                    AL.SourcePause(_source);
            }
            else
            {
                if (state != ALSourceState.Playing)
                    AL.SourcePlay(_source);

                AL.Source(_source, ALSourcef.Gain, 1.0f - length/soundDistance);  // Source gain
            }
        }

        public void Play()
        {
            AL.SourcePlay(_source);
        }

        public void Pause()
        {
            AL.SourcePause(_source);
        }

        public void Stop()
        {
            AL.SourceStop(_source);
        }

        private void LoadBufferWithSoundData(int buffer)
        {
            var oggStream = FileManager.GetFileStream(audioFile, FileType.Audio);

            using (var vorbis = new VorbisReader(oggStream, true))
            {
                var channels = vorbis.Channels == 2 ? ALFormat.Stereo16 : ALFormat.Mono16;
                var sampleRate = vorbis.SampleRate;

                var totalSamples = (int)(vorbis.TotalSamples * vorbis.Channels);
                var floatSamples = new float[totalSamples];
                var pcmSamples = new short[totalSamples];

                int offset = 0;
                while (offset < totalSamples)
                {
                    offset += vorbis.ReadSamples(floatSamples, offset, totalSamples - offset);
                }

                // Convert float samples to 16-bit PCM samples
                for (int i = 0; i < floatSamples.Length; i++)
                {
                    var sampleVal = (int)(floatSamples[i] * short.MaxValue);
                    pcmSamples[i] = (short)Math.Clamp(sampleVal, short.MinValue, short.MaxValue);
                }

                // Using GCHandle to pin the memory and get a pointer to it
                GCHandle handle = GCHandle.Alloc(pcmSamples, GCHandleType.Pinned);
                try
                {
                    IntPtr pointer = handle.AddrOfPinnedObject();
                    AL.BufferData(buffer, channels, pointer, pcmSamples.Length * sizeof(short), sampleRate);
                }
                finally
                {
                    handle.Free();  // Always free the handle
                }
            }
        }

        ~SoundEmitter()
        {
            // Cleanup
            AL.DeleteSource(_source);
            AL.DeleteBuffer(_buffer);
        }
    }
}
