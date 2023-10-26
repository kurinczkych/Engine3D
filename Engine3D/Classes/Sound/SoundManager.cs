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
            if (device == null)
            {
                throw new Exception("Failed to open device");
            }

            context = ALC.CreateContext(device, new int[0]);  // null means default attributes
            if (context == null)
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

        public SoundEmitter CreateSoundEmitter(string filePath)
        {
            var emitter = new SoundEmitter(filePath);
            soundEmitters.Add(emitter);
            return emitter;
        }

        public SoundEmitter CreateSoundEmitter(string filePath, Vector3 position)
        {
            var emitter = new SoundEmitter(filePath, position);
            soundEmitters.Add(emitter);
            return emitter;
        }

        public void SetListener(Vector3 position)
        {
            AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z);
            foreach (var emitter in soundEmitters)
            {
                emitter.UpdateGain(position);
            }
        }

        public void PlayAll()
        {
            foreach (var emitter in soundEmitters)
            {
                emitter.Play();
            }
        }

        public void StopAll()
        {
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
