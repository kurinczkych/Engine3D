using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class AnimationPose
    {
        public bool AlreadyInterpolated = false;

        private List<TranslationKeyFrame> Translations = new List<TranslationKeyFrame>();
        public int TranslationsSize { get { return Translations.Count; } }
        private List<RotationKeyFrame> Rotations = new List<RotationKeyFrame>();
        public int RotationsSize { get { return Rotations.Count; } }
        private List<ScalingKeyFrame> Scales = new List<ScalingKeyFrame>();
        public int ScalesSize { get { return Scales.Count; } }

        public void AddTranslationKey(Vector3 translation, double time)
        {
            Translations.Add(new TranslationKeyFrame(translation, time));
        }
        public void AddRotationKey(Quaternion rot, double time)
        {
            Rotations.Add(new RotationKeyFrame(rot, time));
        }
        public void AddScalingKey(Vector3 scaling, double time)
        {
            Scales.Add(new ScalingKeyFrame(scaling, time));
        }

        public TranslationKeyFrame GetTranslationKeyFrame(int keyFrameIndex)
        {
            return Translations[keyFrameIndex];
        }

        public RotationKeyFrame GetRotationKeyFrame(int keyFrameIndex)
        {
            return Rotations[keyFrameIndex];
        }

        public ScalingKeyFrame GetScalingKeyFrame(int keyFrameIndex)
        {
            return Scales[keyFrameIndex];
        }

        public Vector3 GetInterpolatedTranslationKeyFrame(double time)
        {
            Vector3 lerped = new Vector3();
            if(TranslationsSize > 0)
            {
                double total = 0.0f;
                double t = 0.0f;
                int currKey = FindTranslationKeyFrame(time);
                int nextKey = currKey + 1;

                TranslationKeyFrame currFrame = GetTranslationKeyFrame(currKey);
                TranslationKeyFrame nextFrame = GetTranslationKeyFrame(nextKey);

                total = nextFrame.GetTime() - currFrame.GetTime();
                t = (time - currFrame.GetTime()) / total;

                Vector3 vi = currFrame.Translation;
                Vector3 vf = nextFrame.Translation;
                var v1 = Vector3.Multiply(vi, (float)(1.0f - t));
                var v2 = Vector3.Multiply(vf, (float)t);
                lerped = v1 + v2;
            }

            return lerped;
        }

        public Quaternion GetInterpolatedRotationKeyFrame(double time)
        {
            Quaternion slerped = Quaternion.Identity;
            if(RotationsSize > 0)
            {
                double total = 0.0f;
                double t = 0.0f;
                int currKey = FindTranslationKeyFrame(time);
                int nextKey = currKey + 1;

                RotationKeyFrame currFrame = GetRotationKeyFrame(currKey);
                RotationKeyFrame nextFrame = GetRotationKeyFrame(nextKey);

                total = nextFrame.GetTime() - currFrame.GetTime();
                t = (time - currFrame.GetTime()) / total;

                Quaternion qi = currFrame.Rotation;
                Quaternion qf = nextFrame.Rotation;
                slerped = Quaternion.Slerp(qi, qf, (float)t);
            }

            return slerped;
        }

        public Vector3 GetInterpolatedScalingKeyFrame(double time)
        {
            Vector3 lerped = new Vector3();
            if (TranslationsSize > 0)
            {
                double total = 0.0f;
                double t = 0.0f;
                int currKey = FindScalingKeyFrame(time);
                int nextKey = currKey + 1;

                ScalingKeyFrame currFrame = GetScalingKeyFrame(currKey);
                ScalingKeyFrame nextFrame = GetScalingKeyFrame(nextKey);

                total = nextFrame.GetTime() - currFrame.GetTime();
                t = (time - currFrame.GetTime()) / total;

                Vector3 vi = currFrame.Scaling;
                Vector3 vf = nextFrame.Scaling;
                var v1 = Vector3.Multiply(vi, (float)(1.0f - t));
                var v2 = Vector3.Multiply(vf, (float)t);
                lerped = v1 + v2;
            }

            return lerped;
        }

        public int FindTranslationKeyFrame(double time)
        {
            int currKey = 0;
            for (int i = 0; i < TranslationsSize; i++)
            {
                currKey = i;
                if (GetTranslationKeyFrame(currKey + 1).GetTime() > time)
                    break;
            }

            return currKey;
        }

        public int FindRotationKeyFrame(double time)
        {
            int currKey = 0;
            for (int i = 0; i < RotationsSize; i++)
            {
                currKey = i;
                if (GetRotationKeyFrame(currKey + 1).GetTime() > time)
                    break;
            }

            return currKey;
        }

        public int FindScalingKeyFrame(double time)
        {
            int currKey = 0;
            for (int i = 0; i < ScalesSize; i++)
            {
                currKey = i;
                if (GetScalingKeyFrame(currKey + 1).GetTime() > time)
                    break;
            }

            return currKey;
        }
    }
}
