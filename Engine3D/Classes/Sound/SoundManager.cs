using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Compute.OpenCL;
using OpenTK.Mathematics;

namespace Engine3D
{
    public class SoundManager
    {
        public List<SoundEmitter> soundEmitters = new List<SoundEmitter>();

        private ALDevice device;
        private ALContext context;

        public SoundManager()
        {
            device = ALC.OpenDevice(null);  // null means the default device
            if (device != ALDevice.Null)
            {
                context = ALC.CreateContext(device, new int[0]);  // null means default attributes
                if (context == ALContext.Null)
                {
                    throw new Exception("Failed to create context");
                }

                if (!ALC.MakeContextCurrent(context))
                {
                    throw new Exception("Failed to make context current");
                }

                AL.DistanceModel(ALDistanceModel.LinearDistance);
                //AL.DistanceModel(ALDistanceModel.InverseDistanceClamped);
                AL.Listener(ALListenerf.Gain, 1.0f);
            }
            else
            {
                // Error handling
            }
        }

        public SoundEmitter? CreateSoundEmitter(string filePath)
        {
            if (device == ALDevice.Null)
                throw new Exception("There is no opened sound device!");

            var emitter = new SoundEmitter(filePath, out bool success);

            if (success)
            {
                soundEmitters.Add(emitter);
                return emitter;
            }
            else
                return null;
        }

        public SoundEmitter CreateSoundEmitter(string filePath, Vector3 position)
        {
            if (device == ALDevice.Null)
                throw new Exception("There is no opened sound device!");

            var emitter = new SoundEmitter(filePath, position, out bool success);

            if (success)
            {
                soundEmitters.Add(emitter);
                return emitter;
            }
            else
                return null;
        }

        public void SetListener(Vector3 position)
        {
            if (device == ALDevice.Null)
            {
                // Error handling

                return;
            }

            AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z);
            foreach (var emitter in soundEmitters)
            {
                emitter.UpdateGain(position);
            }
        }

        public void PlayAll()
        {
            if (device == ALDevice.Null)
            {
                // Error handling

                return;
            }

            foreach (var emitter in soundEmitters)
            {
                emitter.Play();
            }
        }

        public void StopAll()
        {
            if (device == ALDevice.Null)
            {
                // Error handling

                return;
            }

            foreach (var emitter in soundEmitters)
            {
                emitter.Stop();
            }
        }

        ~SoundManager()
        {
            ALC.MakeContextCurrent(context);  // Deselect the current context
            ALC.DestroyContext(context);  // Destroy the context
            ALC.CloseDevice(device);  // Close the device
        }
    }
}
