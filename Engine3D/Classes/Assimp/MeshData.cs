using Assimp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OpenTK.Graphics.OpenGL.GL;

namespace Engine3D
{
    public class MeshData
    {
        public List<Vec2d> uvs = new List<Vec2d>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector3> allVerts = new List<Vector3>();

        public List<Vertex> uniqueVertices = new List<Vertex>();
        public List<float> visibleVerticesData = new List<float>();
        public List<float> visibleVerticesDataWithAnim = new List<float>();
        public List<float> visibleVerticesDataOnlyPos = new List<float>();
        public List<float> visibleVerticesDataOnlyPosAndNormal = new List<float>();

        public List<uint> indices = new List<uint>();
        public List<uint> visibleIndices = new List<uint>();
        public uint maxVisibleIndex = 0;

        public bool hasIndices = false;
        public AABB Bounds = new AABB();

        public List<List<uint>> groupedIndices = new List<List<uint>>();

        public Dictionary<string, Bone> boneMatrices = new Dictionary<string, Bone>();

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
            visibleVerticesDataOnlyPos.Clear();
            visibleVerticesDataOnlyPosAndNormal.Clear();
            bool anim = false;
            if (visibleVerticesDataWithAnim.Count > 0)
            {
                visibleVerticesDataWithAnim.Clear();
                anim = true;
            }

            for (int i = 0; i < uniqueVertices.Count; i++)
            {
                var a = uniqueVertices[i];
                a.p = Vector3.TransformPosition(uniqueVertices[i].p, trans);
                uniqueVertices[i] = a;

                visibleVerticesData.AddRange(uniqueVertices[i].GetData());
                if(anim)
                    visibleVerticesDataWithAnim.AddRange(uniqueVertices[i].GetDataWithAnim());
                visibleVerticesDataOnlyPos.AddRange(uniqueVertices[i].GetDataOnlyPos());
                visibleVerticesDataOnlyPosAndNormal.AddRange(uniqueVertices[i].GetDataOnlyPosAndNormal());
            }
        }
    }
}
