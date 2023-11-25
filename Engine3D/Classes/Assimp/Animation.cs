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
        public List<Matrix4> Positions = new List<Matrix4>();
        public List<Matrix4> Rotations = new List<Matrix4>();
        public List<Matrix4> Scalings = new List<Matrix4>();
        public List<Matrix4> Transformations = new List<Matrix4>();
    }

    public class Animation
    {
        public double DurationInTicks;
        public double TicksPerSecond;
        public List<BoneAnimation> boneAnimations = new List<BoneAnimation>();
    }
}
