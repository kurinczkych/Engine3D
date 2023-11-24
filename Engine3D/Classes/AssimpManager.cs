using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using FontStashSharp;
using OpenTK.Mathematics;

namespace Engine3D
{
    public class Bone
    {
        public List<VertexWeight> weights;
        public Matrix4 offsetMatrix;

        public Bone(List<VertexWeight> weights, Matrix4x4 offsetMatrix)
        {
            this.weights = new List<VertexWeight>(weights);

            this.offsetMatrix = new Matrix4();
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    this.offsetMatrix[x, y] = offsetMatrix[x, y];
                }
            }
        }
    }

    public class MeshData
    {
        public List<Vec2d> uvs = new List<Vec2d>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector3> allVerts = new List<Vector3>();

        public List<Vertex> uniqueVertices = new List<Vertex>();
        public List<float> visibleVerticesData = new List<float>();
        public List<float> visibleVerticesDataOnlyPos = new List<float>();
        public List<float> visibleVerticesDataOnlyPosAndNormal = new List<float>();

        public List<uint> indices = new List<uint>();
        public List<uint> visibleIndices = new List<uint>();
        public uint maxVisibleIndex = 0;

        public bool hasIndices = false;
        public AABB Bounds = new AABB();

        public List<List<uint>> groupedIndices = new List<List<uint>>();

        public List<Bone> bones = new List<Bone>();

        public MeshData() { }

        public void CalculateGroupedIndices()
        {
            groupedIndices = indices
                   .Select((x, i) => new { Index = i, Value = x })
                   .GroupBy(x => x.Index / 3)
                   .Select(x => x.Select(v => v.Value).ToList())
                   .ToList();
        }

        public void TransformMeshData(Matrix4 trans)
        {
            visibleVerticesData.Clear();
            for (int i = 0; i < uniqueVertices.Count; i++)
            {
                var a = uniqueVertices[i];
                a.p = Vector3.TransformPosition(uniqueVertices[i].p, trans);
                uniqueVertices[i] = a;

                visibleVerticesData.AddRange(uniqueVertices[i].GetData());
                visibleVerticesDataOnlyPos.AddRange(uniqueVertices[i].GetDataOnlyPos());
                visibleVerticesDataOnlyPosAndNormal.AddRange(uniqueVertices[i].GetDataOnlyPosAndNormal());
            }
        }
    }

    public class ModelData
    {
        public List<MeshData> meshes = new List<MeshData>();

        public ModelData() { }
    }

    public class AssimpManager
    {
        private AssimpContext context;

        public AssimpManager()
        {
            context = new AssimpContext();
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

            var model = context.ImportFile("Assets\\" + FileType.Models.ToString() + "\\" + relativeModelPath, PostProcessSteps.Triangulate);

            foreach (var mesh in model.Meshes)
            {
                MeshData meshData = new MeshData();

                foreach(var bone in mesh.Bones)
                {
                    meshData.bones.Add(new Bone(bone.VertexWeights, bone.OffsetMatrix));
                }

                HashSet<int> normalsHash = new HashSet<int>();
                for (int i = 0; i < mesh.Normals.Count; i++)
                {
                    Vector3 n = AssimpVector3(mesh.Normals[i]);
                    var hash = n.GetHashCode();
                    if (!normalsHash.Contains(hash))
                    {
                        normalsHash.Add(hash);
                        meshData.normals.Add(n);
                    }
                }

                HashSet<int> uvsHash = new HashSet<int>();
                for (int i = 0; i < mesh.TextureCoordinateChannels.First().Count; i++)
                {
                    Vec2d uv = AssimpVec2d(mesh.TextureCoordinateChannels.First()[i]);
                    var hash = uv.GetHashCode();
                    if (!uvsHash.Contains(hash))
                    {
                        uvsHash.Add(hash);
                        meshData.uvs.Add(uv);
                    }
                }

                HashSet<Vector3> vertsHash = new HashSet<Vector3>();
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    Vector3 n = AssimpVector3(mesh.Vertices[i]);
                    if (!vertsHash.Contains(n))
                    {
                        vertsHash.Add(n);
                        meshData.allVerts.Add(n);
                    }
                }

                Dictionary<int, uint> vertexHash = new Dictionary<int, uint>();
                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    var face = mesh.Faces[i];

                    (Vector3 vv1, Vec2d uv1, Vector3 nv1) = AssimpGetElement(face.Indices[0], mesh);
                    Vertex v1 = new Vertex(vv1, nv1, uv1) { c = color, pi = meshData.allVerts.IndexOf(vv1) };
                    (Vector3 vv2, Vec2d uv2, Vector3 nv2) = AssimpGetElement(face.Indices[1], mesh);
                    Vertex v2 = new Vertex(vv2, nv2, uv2) { c = color, pi = meshData.allVerts.IndexOf(vv2) };
                    (Vector3 vv3, Vec2d uv3, Vector3 nv3) = AssimpGetElement(face.Indices[2], mesh);
                    Vertex v3 = new Vertex(vv3, nv3, uv3) { c = color, pi = meshData.allVerts.IndexOf(vv3) };

                    int v1h = v1.GetHashCode();
                    if (!vertexHash.ContainsKey(v1h))
                    {
                        meshData.uniqueVertices.Add(v1);
                        meshData.visibleVerticesData.AddRange(v1.GetData());
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
                modelData.meshes.Add(meshData);
            }

            return modelData;
        }

        private (Vector3, Vec2d, Vector3) AssimpGetElement(int index, Assimp.Mesh mesh)
        {
            return (AssimpVector3(mesh.Vertices[index]), AssimpVec2d(mesh.TextureCoordinateChannels.First()[index]), AssimpVector3(mesh.Normals[index]));
        }

        private Vector3 AssimpVector3(Vector3D v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        private Vector3D AssimpVector3D(Vector3 v)
        {
            return new Vector3D(v.X, v.Y, v.Z);
        }

        private Vec2d AssimpVec2d(Vector3D v)
        {
            return new Vec2d(v.X, v.Y);
        }
    }
}
