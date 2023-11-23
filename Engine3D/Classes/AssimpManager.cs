using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using FontStashSharp;
using OpenTK.Mathematics;

namespace Engine3D.Classes
{
    public class ModelData
    {
        public List<Vec2d> uvs = new List<Vec2d>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector3> allVerts = new List<Vector3>();

        public List<Vertex> uniqueVertices = new List<Vertex>();
        public List<float> visibleVerticesData = new List<float>();
        public List<float> visibleVerticesDataOnlyPos = new List<float>();
        public List<float> visibleVerticesDataOnlyPosAndNormal = new List<float>();
        public List<uint> indices = new List<uint>();

        public AABB Bounds = new AABB();

        public ModelData() { }

        /*
         List Vertex            uniqueVertices.Add(v1);
         List float             visibleVerticesData.AddRange(v1.GetData());
         List float             visibleVerticesDataOnlyPos.AddRange(v1.GetDataOnlyPos());
         List float             visibleVerticesDataOnlyPosAndNormal.AddRange(v1.GetDataOnlyPosAndNormal());
         List uint              indices.Add((uint)uniqueVertices.Count - 1);
         Dictionary<int,uint>   vertexHash.Add(v1h, (uint)uniqueVertices.Count - 1);
         AABB                   Bounds.Enclose(v1);
        */
    }

    public class AssimpManager
    {
        private AssimpContext context;

        public AssimpManager()
        {
            context = new AssimpContext();

            //ProcessModel("level2Rot.obj");
            //ProcessModel("cube.obj");
        }

        public ModelData ProcessModel(string relativeModelPath, float cr = 1, float cg = 1, float cb = 1, float ca = 1)
        {
            ModelData modelData = new ModelData();

            Color4 color = new Color4(cr, cg, cb, ca);

            string filePath = Environment.CurrentDirectory + "\\Assets\\" + FileType.Models.ToString() + "\\" + relativeModelPath;
            if (!File.Exists(filePath))
            {
                Engine.consoleManager.AddLog("File '" + relativeModelPath + "' not found!", LogType.Warning);
                return null;
            }

            var model = context.ImportFile("Assets\\" + FileType.Models.ToString() + "\\" + relativeModelPath);

            var mesh = model.Meshes.First();

            HashSet<int> normalsHash = new HashSet<int>();
            for (int i = 0; i < mesh.Normals.Count; i++)
            {
                Vector3 n = AssimpVector3(mesh.Normals[i]);
                var hash = n.GetHashCode();
                if (!normalsHash.Contains(hash))
                {
                    normalsHash.Add(hash);
                    modelData.normals.Add(n);
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
                    modelData.uvs.Add(uv);
                }
            }

            HashSet<Vector3> vertsHash = new HashSet<Vector3>();
            //for (int i = 0; i < mesh.Vertices.Count; i++)
            //{
            //    Vector3 n = AssimpVector3(mesh.Vertices[i]);
            //    if (!vertsHash.Contains(n))
            //    {
            //        vertsHash.Add(n);
            //        modelData.allVerts.Add(n);
            //    }
            //}
            ;

            Dictionary<int, uint> vertexHash = new Dictionary<int, uint>();
            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                var face = mesh.Faces[i];
                (Vector3 vv1, Vec2d uv1, Vector3 nv1) = AssimpGetElement(face.Indices[0], mesh);
                Vertex v1 = new Vertex(vv1, nv1, uv1) { c = color };
                (Vector3 vv2, Vec2d uv2, Vector3 nv2) = AssimpGetElement(face.Indices[1], mesh);
                Vertex v2 = new Vertex(vv2, nv2, uv2) { c = color };
                (Vector3 vv3, Vec2d uv3, Vector3 nv3) = AssimpGetElement(face.Indices[2], mesh);
                Vertex v3 = new Vertex(vv3, nv3, uv3) { c = color };

                if (!vertsHash.Contains(vv1))
                {
                    vertsHash.Add(vv1);
                    modelData.allVerts.Add(vv1);
                }
                if (!vertsHash.Contains(vv2))
                {
                    vertsHash.Add(vv2);
                    modelData.allVerts.Add(vv2);
                }
                if (!vertsHash.Contains(vv3))
                {
                    vertsHash.Add(vv3);
                    modelData.allVerts.Add(vv3);
                }

                //int[] v = { int.Parse(vStr[0]), int.Parse(vStr[1]), int.Parse(vStr[2]) };

                //Vertex v1 = new Vertex(verts[v[0] - 1]) { pi = v[0] - 1, c = color };

                int v1h = v1.GetHashCode();
                if (!vertexHash.ContainsKey(v1h))
                {
                    modelData.uniqueVertices.Add(v1);
                    modelData.visibleVerticesData.AddRange(v1.GetData());
                    modelData.visibleVerticesDataOnlyPos.AddRange(v1.GetDataOnlyPos());
                    modelData.visibleVerticesDataOnlyPosAndNormal.AddRange(v1.GetDataOnlyPosAndNormal());
                    modelData.indices.Add((uint)modelData.uniqueVertices.Count - 1);
                    vertexHash.Add(v1h, (uint)modelData.uniqueVertices.Count - 1);
                    modelData.Bounds.Enclose(v1);
                }
                else
                {
                    modelData.indices.Add(vertexHash[v1h]);
                }
                int v2h = v2.GetHashCode();
                if (!vertexHash.ContainsKey(v2h))
                {
                    modelData.uniqueVertices.Add(v2);
                    modelData.visibleVerticesData.AddRange(v2.GetData());
                    modelData.visibleVerticesDataOnlyPos.AddRange(v2.GetDataOnlyPos());
                    modelData.visibleVerticesDataOnlyPosAndNormal.AddRange(v2.GetDataOnlyPosAndNormal());
                    modelData.indices.Add((uint)modelData.uniqueVertices.Count - 1);
                    vertexHash.Add(v2h, (uint)modelData.uniqueVertices.Count - 1);
                    modelData.Bounds.Enclose(v2);
                }
                else
                {
                    modelData.indices.Add(vertexHash[v2h]);
                }
                int v3h = v3.GetHashCode();
                if (!vertexHash.ContainsKey(v3h))
                {
                    modelData.uniqueVertices.Add(v3);
                    modelData.visibleVerticesData.AddRange(v3.GetData());
                    modelData.visibleVerticesDataOnlyPos.AddRange(v3.GetDataOnlyPos());
                    modelData.visibleVerticesDataOnlyPosAndNormal.AddRange(v3.GetDataOnlyPosAndNormal());
                    modelData.indices.Add((uint)modelData.uniqueVertices.Count - 1);
                    vertexHash.Add(v3h, (uint)modelData.uniqueVertices.Count - 1);
                    modelData.Bounds.Enclose(v3);
                }
                else
                {
                    modelData.indices.Add(vertexHash[v3h]);
                }
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

        private Vec2d AssimpVec2d(Vector3D v)
        {
            return new Vec2d(v.X, v.Y);
        }
    }
}
