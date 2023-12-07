using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Bone
    {
        public Matrix4 OffsetMatrix = Matrix4.Identity;
        public Matrix4 FinalMatrix = Matrix4.Identity;

        public Bone() { }

        public Bone(Matrix4 offsetMatrix)
        {
            OffsetMatrix = offsetMatrix;
        }
    }
}
