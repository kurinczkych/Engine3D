using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Skeleton
    {
        public Bone RootBone;
        public Dictionary<string, BoneInfo> BoneMapping = new Dictionary<string, BoneInfo>();
        public int NumOfBones = -1;
        public Matrix4 InverseGlobal = Matrix4.Identity;

        public Skeleton()
        {
            RootBone = new Bone();
        }

        public void UpdateSkeleton(Bone? bone)
        {
            if (NumOfBones == 0) return;

            if (bone == null)
                bone = RootBone;

            bone.GlobalTransform = bone.ParentTransform * bone.Transform * bone.LocalTransform;
            bone.FinalTransform = InverseGlobal * bone.GlobalTransform * bone.BoneOffset;

            for (int i = 0; i < bone.Children.Count; i++)
            {
                UpdateSkeleton(bone.Children[i]);
            }
        }

        public void TraversePositions(Bone? bone, Matrix4 model, ref List<Vector3> positions)
        {
            if (bone == null)
                bone = RootBone;

            positions.Add(bone.GetWorldSpacePosition(model));

            for (int i = 0; i < bone.Children.Count; i++)
            {
                TraversePositions(bone.Children[i], model, ref positions);
            }
        }

        public List<Vector3> GetBonePositions(Matrix4 model)
        {
            List<Vector3> positions = new List<Vector3>();

            TraversePositions(RootBone, model, ref positions);

            return positions;
        }

        public Vector3 GetBonePosition(string boneName, Matrix4 model)
        {
            Bone? bone = GetBone(boneName);

            if (bone != null)
                return bone.GetWorldSpacePosition(model);

            return Vector3.Zero;
        }

        public bool ImportSkeletonBone(Assimp.Node node, Bone? bone = null)
        {
            bool hasBone = false;
            bool hasUsefulChild = false;
            string boneName = node.Name;

            if (bone == null)
                bone = RootBone;

            if(BoneMapping.ContainsKey(boneName))
            {
                bone.Name = boneName;
                hasBone = true;
            }

            for (int i = 0; i < node.ChildCount; i++)
            {
                bone.Children[i].Parent = bone;

                bool importRes = ImportSkeletonBone(node.Children[i], bone.Children[i]);
                if (importRes)
                    hasUsefulChild = true;
            }

            if(hasUsefulChild || hasBone)
            {
                string nodeName = boneName;

                bone.BoneOffset = BoneMapping[bone.Name].Offset;
                bone.BoneIndex = BoneMapping[bone.Name].Index;

                if (bone.Name == boneName)
                    bone.Transform = AssimpManager.AssimpMatrix4(node.Transform);

                return true;
            }

            return false;
        }

        public int GetNumberOfBones()
        {
            if (NumOfBones != -1)
                return NumOfBones;

            NumOfBones = TraverseGetNumOfBones(RootBone);

            return NumOfBones;
        }

        public void UpdateAnimationMatrix(ref List<Matrix4> animMatrices, Bone? bone = null)
        {
            if(bone == null)
                bone = RootBone;

            if (bone.BoneIndex >= 0)
                animMatrices[bone.BoneIndex] = bone.FinalTransform;

            for (int i = 0; i < bone.Children.Count; i++)
            {
                UpdateAnimationMatrix(ref animMatrices, bone.Children[i]);
            }
        }

        public Bone? GetBone(int boneIndex, Bone? boneToFind = null)
        {
            if(boneToFind == null)
                boneToFind = RootBone;

            if(boneToFind.BoneIndex == boneIndex)
                return boneToFind;

            for (int i = 0; i < boneToFind.Children.Count; i++)
            {
                Bone? child = GetBone(boneIndex, boneToFind.Children[i]);
                if (child == null)
                    return child;
            }

            return null;
        }

        public Bone? GetBone(string nodeName, Bone? boneToFind = null)
        {
            if(boneToFind == null)
                boneToFind = RootBone;

            if(boneToFind.Name == nodeName)
                return boneToFind;

            for (int i = 0; i < boneToFind.Children.Count; i++)
            {
                Bone? child = GetBone(nodeName, boneToFind.Children[i]);
                if (child == null)
                    return child;
            }

            return null;
        }

        private int TraverseGetNumOfBones(Bone? bone)
        {
            if(bone == null)
                bone = RootBone;

            int counter = bone.BoneIndex > -1 ? 1 : 0;

            for (int i = 0; i < bone.Children.Count; i++)
            {
                counter += TraverseGetNumOfBones(bone.Children[i]);
            }

            return counter;
        }
    }
}
