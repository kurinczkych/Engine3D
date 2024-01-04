using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using Assimp.Configs;
using Assimp.Unmanaged;
using FontStashSharp;
using HtmlAgilityPack;
using OpenTK.Mathematics;
using static OpenTK.Graphics.OpenGL.GL;

namespace Engine3D
{

    public class AssimpManager
    {
        private AssimpContext context;
        public Dictionary<string, AnimationClip> animations = new Dictionary<string, AnimationClip>();

        public AssimpManager()
        {
            context = new AssimpContext();
        }

        public void ProcessAnimation(string relativeAnimationPath)
        {
            string filePath = Environment.CurrentDirectory + "\\Assets\\" + FileType.Animations.ToString() + "\\" + relativeAnimationPath;
            if (!File.Exists(filePath))
            {
                Engine.consoleManager.AddLog("File '" + relativeAnimationPath + "' not found!", LogType.Warning);
                return;
            }

            var scene = context.ImportFile("Assets\\" + FileType.Animations.ToString() + "\\" + relativeAnimationPath);

            if (scene.AnimationCount > 0)
            {
                foreach(var anim in scene.Animations)
                {
                    AnimationClip animClip = new AnimationClip();
                    animClip.DurationInTicks = anim.DurationInTicks;
                    animClip.TicksPerSecond = anim.TicksPerSecond;
                    animClip.Name = anim.Name;

                    foreach (var node in anim.NodeAnimationChannels)
                    {
                        AnimationPose animPose = new AnimationPose();

                        for (int i = 0; i < node.PositionKeyCount; i++)
                            animPose.AddTranslationKey(AssimpVector3(node.PositionKeys[i].Value), node.PositionKeys[i].Time);
                        for (int i = 0; i < node.RotationKeyCount; i++)
                            animPose.AddRotationKey(AssimpQuaternion(node.RotationKeys[i].Value), node.RotationKeys[i].Time);
                        for (int i = 0; i < node.ScalingKeyCount; i++)
                            animPose.AddScalingKey(AssimpVector3(node.ScalingKeys[i].Value), node.ScalingKeys[i].Time);

                        animClip.AddAnimationPose(node.NodeName, animPose);
                    }

                    animations.Add(anim.Name, animClip);
                }
            }

        }

        private void AddAnimation(Assimp.Animation anim)
        {
            AnimationClip animClip = new AnimationClip();
            animClip.DurationInTicks = anim.DurationInTicks;
            animClip.TicksPerSecond = anim.TicksPerSecond;
            animClip.Name = anim.Name;

            foreach (var node in anim.NodeAnimationChannels)
            {
                AnimationPose animPose = new AnimationPose();
                if (node.NodeName == "Spine")
                    ;

                for (int i = 0; i < node.PositionKeyCount; i++)
                    animPose.AddTranslationKey(AssimpVector3(node.PositionKeys[i].Value), node.PositionKeys[i].Time);
                for (int i = 0; i < node.RotationKeyCount; i++)
                    animPose.AddRotationKey(AssimpQuaternion(node.RotationKeys[i].Value), node.RotationKeys[i].Time);
                for (int i = 0; i < node.ScalingKeyCount; i++)
                    animPose.AddScalingKey(AssimpVector3(node.ScalingKeys[i].Value), node.ScalingKeys[i].Time);

                animClip.AddAnimationPose(node.NodeName, animPose);
                
            }

            animations.Add(anim.Name, animClip);
        }

        public ModelData? ProcessModel(string relativeModelPath, float cr = 1, float cg = 1, float cb = 1, float ca = 1)
        {
            ModelData modelData = new ModelData();

            Color4 color = new Color4(cr, cg, cb, ca);

            string filePath = Environment.CurrentDirectory + "\\Assets\\" + FileType.Models.ToString() + "\\" + relativeModelPath;
            if (!File.Exists(filePath))
            {
                Engine.consoleManager.AddLog("File '" + relativeModelPath + "' not found!", LogType.Warning);
                return null;
            }

            var scene = context.ImportFile("Assets\\" + FileType.Models.ToString() + "\\" + relativeModelPath, 
                /*PostProcessSteps.LimitBoneWeights |*/ PostProcessSteps.Triangulate /*| PostProcessSteps.JoinIdenticalVertices*/ );

            foreach (var anim in scene.Animations)
                AddAnimation(anim);

            modelData.skeleton = new Skeleton();

            ProcessMeshDataRec(scene.RootNode, ref scene, ref modelData, ref color);

            if(!modelData.skeleton.ImportSkeletonBone(scene.RootNode))
            {
                throw new Exception("Cannot import skeleton of the model!");
            }
            modelData.skeleton.InverseGlobal = AssimpMatrix4(scene.RootNode.Transform);

            if(modelData.skeleton.GetNumberOfBones() > 0)
            {
                int numberOfBones = modelData.skeleton.GetNumberOfBones();
                for (int i = 0; i < numberOfBones; i++)
                {
                    modelData.boneMatrices.Add(Matrix4.Identity); // Add a new Matrix4 instance to the list for each bone
                }
                modelData.skeleton.UpdateSkeleton();
                modelData.skeleton.UpdateBoneMatrices(ref modelData.boneMatrices);
            }

            return modelData;

        }

        private void ProcessMeshDataRec(Node node, ref Assimp.Scene scene, ref ModelData modelData, ref Color4 color)
        {
            for (int i = 0; i < node.MeshCount; i++)
            {
                Assimp.Mesh aiMesh = scene.Meshes[node.MeshIndices[i]];
                MeshData meshData = ProcessMeshData(aiMesh, ref color, ref modelData);
                modelData.meshes.Add(meshData);
            }

            for (int i = 0; i < node.ChildCount; i++)
            {
                ProcessMeshDataRec(node.Children[i], ref scene, ref modelData, ref color);
            }
        }

        private MeshData ProcessMeshData(Assimp.Mesh aiMesh, ref Color4 color, ref ModelData modelData)
        {
            MeshData meshData = new MeshData();

            Dictionary<int, List<(int, float)>> boneDict = new Dictionary<int, List<(int, float)>>();

            if (aiMesh.HasBones)
            {
                int boneCount = aiMesh.BoneCount;

                for (int i = 0; i < boneCount; i++)
                {
                    string boneName = aiMesh.Bones[i].Name;
                    int boneIndex = 0;

                    if (!modelData.skeleton.BoneMapping.ContainsKey(boneName))
                    {
                        BoneInfo boneInfo = new BoneInfo();
                        boneIndex = modelData.boneCount++;
                        boneInfo.Offset = AssimpMatrix4(aiMesh.Bones[i].OffsetMatrix);
                        boneInfo.Index = boneIndex;
                        modelData.skeleton.BoneMapping.Add(boneName, boneInfo);
                    }
                    else
                    {
                        boneIndex = modelData.skeleton.BoneMapping[boneName].Index;
                    }

                    foreach (VertexWeight vw in aiMesh.Bones[i].VertexWeights)
                    {
                        if (boneDict.ContainsKey(vw.VertexID))
                        {
                            boneDict[vw.VertexID].Add((boneIndex, vw.Weight));
                        }
                        else
                            boneDict.Add(vw.VertexID, new List<(int, float)>() { (boneIndex, vw.Weight) });
                    }
                }
            }
            if (aiMesh.Bones.Count > 100)
                throw new Exception("MAX_BONES is 100, bone count cannot be larger than that!");

            HashSet<int> normalsHash = new HashSet<int>();
            for (int i = 0; i < aiMesh.Normals.Count; i++)
            {
                Vector3 n = AssimpVector3(aiMesh.Normals[i]);
                var hash = n.GetHashCode();
                if (!normalsHash.Contains(hash))
                {
                    normalsHash.Add(hash);
                    meshData.normals.Add(n);
                }
            }

            HashSet<int> uvsHash = new HashSet<int>();
            for (int i = 0; i < aiMesh.TextureCoordinateChannels.First().Count; i++)
            {
                Vec2d uv = AssimpVec2d(aiMesh.TextureCoordinateChannels.First()[i]);
                var hash = uv.GetHashCode();
                if (!uvsHash.Contains(hash))
                {
                    uvsHash.Add(hash);
                    meshData.uvs.Add(uv);
                }
            }

            HashSet<Vector3> vertsHash = new HashSet<Vector3>();
            for (int i = 0; i < aiMesh.Vertices.Count; i++)
            {
                Vector3 n = AssimpVector3(aiMesh.Vertices[i]);
                if (!vertsHash.Contains(n))
                {
                    vertsHash.Add(n);
                    meshData.allVerts.Add(n);
                }
            }

            Dictionary<int, uint> vertexHash = new Dictionary<int, uint>();
            for (int i = 0; i < aiMesh.Faces.Count; i++)
            {
                var face = aiMesh.Faces[i];

                (Vector3 vv1, Vec2d uv1, Vector3 nv1) = AssimpGetElement(face.Indices[0], aiMesh);
                Vertex v1 = new Vertex(vv1, nv1, uv1) { c = color, pi = meshData.allVerts.IndexOf(vv1) };

                if (boneDict.ContainsKey(face.Indices[0]))
                {
                    v1.boneCount = boneDict[face.Indices[0]].Count;
                    if (v1.boneCount < 4)
                    {
                        while (boneDict[face.Indices[0]].Count < 4)
                            boneDict[face.Indices[0]].Add((0, 0));
                    }
                    v1.boneIDs = boneDict[face.Indices[0]].Select(x => x.Item1).ToList();
                    v1.boneWeights = boneDict[face.Indices[0]].Select(x => x.Item2).ToList();
                    float sum = 0;
                    for (int j = 0; j < v1.boneCount; j++)
                        sum += v1.boneWeights[j];

                    for (int j = 0; j < v1.boneCount; j++)
                        v1.boneWeights[j] /= sum;
                }
                else
                {
                    v1.boneCount = 0;
                    v1.boneIDs = new List<int>() { 0, 0, 0, 0 };
                    v1.boneWeights = new List<float>() { 0, 0, 0, 0 };
                }

                (Vector3 vv2, Vec2d uv2, Vector3 nv2) = AssimpGetElement(face.Indices[1], aiMesh);
                Vertex v2 = new Vertex(vv2, nv2, uv2) { c = color, pi = meshData.allVerts.IndexOf(vv2) };

                if (boneDict.ContainsKey(face.Indices[1]))
                {
                    v2.boneCount = boneDict[face.Indices[1]].Count;
                    if (v2.boneCount < 4)
                    {
                        while (boneDict[face.Indices[1]].Count < 4)
                            boneDict[face.Indices[1]].Add((0, 0));
                    }
                    v2.boneIDs = boneDict[face.Indices[1]].Select(x => x.Item1).ToList();
                    v2.boneWeights = boneDict[face.Indices[1]].Select(x => x.Item2).ToList();
                    float sum = 0;
                    for (int j = 0; j < v2.boneCount; j++)
                        sum += v2.boneWeights[j];

                    for (int j = 0; j < v2.boneCount; j++)
                        v2.boneWeights[j] /= sum;
                }
                else
                {
                    v2.boneCount = 0;
                    v2.boneIDs = new List<int>() { 0, 0, 0, 0 };
                    v2.boneWeights = new List<float>() { 0, 0, 0, 0 };
                }

                (Vector3 vv3, Vec2d uv3, Vector3 nv3) = AssimpGetElement(face.Indices[2], aiMesh);
                Vertex v3 = new Vertex(vv3, nv3, uv3) { c = color, pi = meshData.allVerts.IndexOf(vv3) };

                if (boneDict.ContainsKey(face.Indices[2]))
                {
                    v3.boneCount = boneDict[face.Indices[2]].Count;
                    if (v3.boneCount < 4)
                    {
                        while (boneDict[face.Indices[2]].Count < 4)
                            boneDict[face.Indices[2]].Add((0, 0));
                    }
                    v3.boneIDs = boneDict[face.Indices[2]].Select(x => x.Item1).ToList();
                    v3.boneWeights = boneDict[face.Indices[2]].Select(x => x.Item2).ToList();
                    float sum = 0;
                    for (int j = 0; j < v3.boneCount; j++)
                        sum += v3.boneWeights[j];

                    for (int j = 0; j < v3.boneCount; j++)
                        v3.boneWeights[j] /= sum;
                }
                else
                {
                    v3.boneCount = 0;
                    v3.boneIDs = new List<int>() { 0, 0, 0, 0 };
                    v3.boneWeights = new List<float>() { 0, 0, 0, 0 };
                }

                int v1h = v1.GetHashCode();
                if (!vertexHash.ContainsKey(v1h))
                {
                    meshData.uniqueVertices.Add(v1);
                    meshData.visibleVerticesData.AddRange(v1.GetData());
                    meshData.visibleVerticesDataWithAnim.AddRange(v1.GetDataWithAnim());
                    meshData.visibleVerticesDataOnlyPos.AddRange(v1.GetDataOnlyPos());
                    meshData.visibleVerticesDataOnlyPosAndNormal.AddRange(v1.GetDataOnlyPosAndNormal());
                    meshData.indices.Add((uint)meshData.uniqueVertices.Count - 1);
                    vertexHash.Add(v1h, (uint)meshData.uniqueVertices.Count - 1);
                    meshData.Bounds.Enclose(v1);
                }
                else
                {
                    meshData.indices.Add(vertexHash[v1h]);
                }
                int v2h = v2.GetHashCode();
                if (!vertexHash.ContainsKey(v2h))
                {
                    meshData.uniqueVertices.Add(v2);
                    meshData.visibleVerticesData.AddRange(v2.GetData());
                    meshData.visibleVerticesDataWithAnim.AddRange(v2.GetDataWithAnim());
                    meshData.visibleVerticesDataOnlyPos.AddRange(v2.GetDataOnlyPos());
                    meshData.visibleVerticesDataOnlyPosAndNormal.AddRange(v2.GetDataOnlyPosAndNormal());
                    meshData.indices.Add((uint)meshData.uniqueVertices.Count - 1);
                    vertexHash.Add(v2h, (uint)meshData.uniqueVertices.Count - 1);
                    meshData.Bounds.Enclose(v2);
                }
                else
                {
                    meshData.indices.Add(vertexHash[v2h]);
                }
                int v3h = v3.GetHashCode();
                if (!vertexHash.ContainsKey(v3h))
                {
                    meshData.uniqueVertices.Add(v3);
                    meshData.visibleVerticesData.AddRange(v3.GetData());
                    meshData.visibleVerticesDataWithAnim.AddRange(v3.GetDataWithAnim());
                    meshData.visibleVerticesDataOnlyPos.AddRange(v3.GetDataOnlyPos());
                    meshData.visibleVerticesDataOnlyPosAndNormal.AddRange(v3.GetDataOnlyPosAndNormal());
                    meshData.indices.Add((uint)meshData.uniqueVertices.Count - 1);
                    vertexHash.Add(v3h, (uint)meshData.uniqueVertices.Count - 1);
                    meshData.Bounds.Enclose(v3);
                }
                else
                {
                    meshData.indices.Add(vertexHash[v3h]);
                }
            }

            meshData.visibleIndices = new List<uint>(meshData.indices);
            meshData.hasIndices = true;
            meshData.CalculateGroupedIndices();

            return meshData;
        }

        private (Vector3, Vec2d, Vector3) AssimpGetElement(int index, Assimp.Mesh mesh)
        {
            return (AssimpVector3(mesh.Vertices[index]), 
                    AssimpVec2d(mesh.TextureCoordinateChannels.First().Count > 0 ? mesh.TextureCoordinateChannels.First()[index] : new Vector3D(0,0,0)), 
                    AssimpVector3(mesh.Normals[index]));
        }

        public static Vector3 AssimpVector3(Vector3D v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static Matrix4 AssimpMatrix4(Assimp.Matrix4x4 m)
        {
            Matrix4 to = new Matrix4();
            //for (int x = 0; x < 4; x++)
            //{
            //    for (int y = 0; y < 4; y++)
            //    {
            //        to[x, y] = m[x, y];
            //    }
            //}
            //matrix4.Transpose();

            to[0, 0] = m.A1; to[0, 1] = m.B1; to[0, 2] = m.C1; to[0, 3] = m.D1;
            to[1, 0] = m.A2; to[1, 1] = m.B2; to[1, 2] = m.C2; to[1, 3] = m.D2;
            to[2, 0] = m.A3; to[2, 1] = m.B3; to[2, 2] = m.C3; to[2, 3] = m.D3;
            to[3, 0] = m.A4; to[3, 1] = m.B4; to[3, 2] = m.C4; to[3, 3] = m.D4;
            to.Transpose();
            return to;
        }
        public static OpenTK.Mathematics.Quaternion AssimpQuaternion(Assimp.Quaternion quat)
        {
            return new OpenTK.Mathematics.Quaternion(quat.X, quat.Y, quat.Z, quat.W);
        }

        public static Vector3D AssimpVector3D(Vector3 v)
        {
            return new Vector3D(v.X, v.Y, v.Z);
        }

        public static Vec2d AssimpVec2d(Vector3D v)
        {
            return new Vec2d(v.X, v.Y);
        }
    }
}
