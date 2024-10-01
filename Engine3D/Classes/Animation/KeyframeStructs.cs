using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{

    public struct TranslationKeyFrame
    {
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 Translation;

        private double time;
        public double GetTime()
        {
            return time;
        }

        public TranslationKeyFrame(Vector3 translation, double time)
        {
            Translation = translation;
            this.time = time;
        }
    }

    public struct RotationKeyFrame
    {
        public Quaternion Rotation;

        private double time;
        public double GetTime()
        {
            return time;
        }

        public RotationKeyFrame(Quaternion rotation, double time)
        {
            Rotation = rotation;
            this.time = time;
        }
    }


    public struct ScalingKeyFrame
    {
        public Vector3 Scaling;

        private double time;
        public double GetTime()
        {
            return time;
        }

        public ScalingKeyFrame(Vector3 scaling, double time)
        {
            Scaling = scaling;
            this.time = time;
        }
    }
}
