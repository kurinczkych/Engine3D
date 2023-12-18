using Assimp;
using Assimp.Unmanaged;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
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

        public void UpdateSkeleton(Bone? bone = null, AnimationClip? animation = null)
        {
            if (NumOfBones == 0) return;

            if (bone == null)
                bone = RootBone;

            Matrix4 animMatrix = Matrix4.Identity;
            if(animation != null)
                animMatrix = animation.GetAnimMatrixForBone(bone, animation.GetLocalTimer());

            switch (Engine.editorData.matrixType)
            {
                case (0):
                    bone.GlobalTransform = animMatrix * bone.ParentTransform * bone.Transform * bone.LocalTransform;
                    bone.FinalTransform = InverseGlobal * bone.GlobalTransform * bone.BoneOffset;
                    break;
                case (1):
                    bone.GlobalTransform = bone.ParentTransform * animMatrix * bone.Transform * bone.LocalTransform;
                    bone.FinalTransform = InverseGlobal * bone.GlobalTransform * bone.BoneOffset;
                    break;
                case (2):
                    bone.GlobalTransform = bone.ParentTransform * bone.Transform * animMatrix * bone.LocalTransform;
                    bone.FinalTransform = InverseGlobal * bone.GlobalTransform * bone.BoneOffset;
                    break;
                case (3):
                    bone.GlobalTransform = bone.ParentTransform * bone.Transform * bone.LocalTransform * animMatrix;
                    bone.FinalTransform = InverseGlobal * bone.GlobalTransform * bone.BoneOffset;
                    break;
                case (4):
                    bone.GlobalTransform = bone.ParentTransform * bone.Transform * bone.LocalTransform;
                    bone.FinalTransform = InverseGlobal * bone.GlobalTransform * bone.BoneOffset * animMatrix;
                    break;
                case (5):
                    bone.GlobalTransform = bone.ParentTransform * bone.Transform * bone.LocalTransform;
                    bone.FinalTransform = InverseGlobal * bone.GlobalTransform * animMatrix * bone.BoneOffset;
                    break;
                case (6):
                    bone.GlobalTransform = bone.ParentTransform * bone.Transform * bone.LocalTransform;
                    bone.FinalTransform = InverseGlobal * animMatrix * bone.GlobalTransform * bone.BoneOffset;
                    break;
                case (7):
                    bone.GlobalTransform = bone.ParentTransform * bone.Transform * bone.LocalTransform;
                    bone.FinalTransform = animMatrix * InverseGlobal * bone.GlobalTransform * bone.BoneOffset;
                    break;
            }

            //bone.GlobalTransform = bone.ParentTransform * bone.Transform * bone.LocalTransform;
            //bone.FinalTransform = InverseGlobal * bone.GlobalTransform * bone.BoneOffset;

            for (int i = 0; i < bone.Children.Count; i++)
            {
                UpdateSkeleton(bone.Children[i], animation);
            }
        }

        public void SendToGpu(Bone? bone, ref Dictionary<string, int> uniformAnimLocations, ref List<int> indexes)
        {
            if (bone == null)
                bone = RootBone;

            if (bone.BoneIndex > -1)
            {
                Matrix4 final = bone.FinalTransform;

                indexes.Add(bone.BoneIndex);
                GL.UniformMatrix4(uniformAnimLocations["boneMatrices"] + bone.BoneIndex, true, ref final);
            }

            for (int i = 0; i < bone.Children.Count; i++)
            {
                SendToGpu(bone.Children[i], ref uniformAnimLocations, ref indexes);
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
            {
                bone = RootBone;
            }
            bone.Name = boneName;

            bone.Children = BoneListResize(bone.Children, node.ChildCount);

            if(BoneMapping.ContainsKey(boneName))
            {
                hasBone = true;
            }

            for (int i = 0; i < node.ChildCount; i++)
            {
                //bool importRes = false;
                //if(hasBone)
                //{
                //    bone.Children.Add(new Bone() { Name = node.Children[i].Name, Parent = bone });
                //    importRes = ImportSkeletonBone(node.Children[i], bone.Children[i]);
                //}
                //else
                //{
                //    importRes = ImportSkeletonBone(node.Children[i], bone.Children[i]);
                //}

                bone.Children[i].Parent = bone;
                bool importRes = ImportSkeletonBone(node.Children[i], bone.Children[i]);

                if (importRes)
                    hasUsefulChild = true;
            }

            if(hasUsefulChild || hasBone)
            {
                string nodeName = boneName;

                if (hasUsefulChild && !hasBone)
                    return true;

                bone.BoneOffset = BoneMapping[bone.Name].Offset;
                bone.BoneIndex = BoneMapping[bone.Name].Index;

                if (bone.Name == boneName)
                    bone.Transform = AssimpManager.AssimpMatrix4(node.Transform);

                return true;
            }

            return false;
        }

        private List<Bone> BoneListResize(List<Bone> list, int addCount)
        {
            List<Bone> a = new List<Bone>();
            int listCount = list==null?0:list.Count;
            for (int i = 0; i < listCount; i++)
            {
                a.Add(list[i]);
            }
            for (int i = 0; i < addCount; i++)
            {
                a.Add(new Bone());
            }
            return a;
        }

        public int GetNumberOfBones()
        {
            if (NumOfBones != -1)
                return NumOfBones;

            NumOfBones = TraverseGetNumOfBones(RootBone);

            return NumOfBones;
        }

        public void UpdateBoneMatrices(ref List<Matrix4> boneMatrices, Bone? bone = null)
        {
            if(bone == null)
                bone = RootBone;

            if (bone.BoneIndex >= 0)
                boneMatrices[bone.BoneIndex] = bone.FinalTransform;

            for (int i = 0; i < bone.Children.Count; i++)
            {
                UpdateBoneMatrices(ref boneMatrices, bone.Children[i]);
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
                if (child != null)
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
