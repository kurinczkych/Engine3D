using MagicPhysX;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class BoneAnimation
    {
        public string Name;
        public Dictionary<int,Vector3> Positions = new Dictionary<int, Vector3>();
        public Dictionary<int,Quaternion> Rotations = new Dictionary<int, Quaternion>();
        public Dictionary<int,Vector3> Scalings = new Dictionary<int, Vector3>();
        public Dictionary<int,Matrix4> Transformations = new Dictionary<int,Matrix4>();

        public Matrix4 GetInterpolatedTransform(int startTime, int endTime, int currentTime)
        {
            if (currentTime < 0 || currentTime > endTime)
            {
                throw new ArgumentOutOfRangeException(nameof(currentTime), "Current time must be within the range of 0 to maxTime.");
            }
            float t = (float)(currentTime - startTime) / (endTime - startTime);

            Vector3 interpolatedPosition = Vector3.Lerp(Positions[startTime], Positions[endTime], t);
            Quaternion interpolatedRotation = Quaternion.Slerp(Rotations[startTime], Rotations[endTime], t);
            Vector3 interpolatedScale = Vector3.Lerp(Scalings[startTime], Scalings[endTime], t);

            Matrix4 transformationMatrix = Matrix4.CreateTranslation(interpolatedScale) *
                                           Matrix4.CreateFromQuaternion(interpolatedRotation) * 
                                           Matrix4.CreateTranslation(interpolatedPosition);
            return transformationMatrix;
        }
    }

    public class Animation
    {
        public double DurationInTicks;
        public double TicksPerSecond;
        public List<BoneAnimation> boneAnimations = new List<BoneAnimation>();
    }
}
