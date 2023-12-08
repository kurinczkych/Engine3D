using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class BoneInfo
    {
        public Matrix4 Offset = Matrix4.Identity;
        public int Index;
    }

    public class Bone
    {
        public Bone Parent;
        public List<Bone> Children = new List<Bone>();
        public string Name;
        public int BoneIndex = -1;

        public Matrix4 BoneOffset = Matrix4.Identity;
        public Matrix4 Transform = Matrix4.Identity;
        public Matrix4 GlobalTransform = Matrix4.Identity;
        public Matrix4 FinalTransform = Matrix4.Identity;
        public Matrix4 LocalTransform = Matrix4.Identity;

        public Matrix4 GetGlobalTransform()
        {
            return ParentTransform * BoneOffset.Inverted();
        }

        public Matrix4 GetWorldSpace(Matrix4 model)
        {
            return model * FinalTransform * BoneOffset.Inverted();
        }

        public Vector3 GetWorldSpacePosition(Matrix4 model)
        {
            Matrix4 m = model * FinalTransform * BoneOffset.Inverted();
            return m.ExtractTranslation();
        }

        public Matrix4 ParentTransform
        {
            get
            {
                if (Parent != null)
                    return Parent.GlobalTransform;
                return Matrix4.Identity;
            }
        }
    }
}
