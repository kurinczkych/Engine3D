using Assimp;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class MeshData
    {


        public Assimp.Mesh mesh;
        [JsonIgnore]
        public List<float> visibleVerticesData = new List<float>();
        [JsonIgnore]
        public List<float> visibleVerticesDataWithAnim = new List<float>();
        [JsonIgnore]
        public List<float> visibleVerticesDataOnlyPos = new List<float>();
        [JsonIgnore]
        public List<float> visibleVerticesDataOnlyPosAndNormal = new List<float>();
        [JsonIgnore]
        public List<float> visibleVerticesDataOnlyPosAndColor = new List<float>();

        [JsonIgnore]
        public List<uint> visibleIndices = new List<uint>();
        [JsonIgnore]
        public uint maxVisibleIndex = 0;

        [JsonIgnore]
        public List<List<uint>> groupedIndices = new List<List<uint>>();
        [JsonIgnore]
        public List<int> pis = new List<int>();

        public AABB Bounds = new AABB();

        public MeshData(Assimp.Mesh mesh)
        {
            this.mesh = mesh;
            foreach (Vector3D v in mesh.Vertices)
                Bounds.Enclose(v);

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                pis.Add(i);  // Store the index of each vertex
            }

            CalculateGroupedIndices();
            visibleVerticesData.AddRange(BaseMesh.GetMeshData(mesh));
            visibleVerticesDataWithAnim.AddRange(BaseMesh.GetMeshDataWithAnim(mesh));
            visibleVerticesDataOnlyPos.AddRange(BaseMesh.GetMeshDataOnlyPos(mesh));
            visibleVerticesDataOnlyPosAndNormal.AddRange(BaseMesh.GetMeshDataOnlyPosAndNormal(mesh));
            visibleVerticesDataOnlyPosAndColor.AddRange(BaseMesh.GetMeshDataOnlyPosAndColor(mesh));
        }

        public void CalculateGroupedIndices()
        {
            groupedIndices = mesh.GetIndices()
                   .Select((x, i) => new { Index = i, Value = (uint)x })
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

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                var a = mesh.Vertices[i];
                a = AHelp.OpenTKToAssimp(Vector3.TransformPosition(AHelp.AssimpToOpenTK(mesh.Vertices[i]), trans));
                mesh.Vertices[i] = a;

                visibleVerticesData.AddRange(BaseMesh.GetMeshData(mesh));
                if (anim)
                    visibleVerticesDataWithAnim.AddRange(BaseMesh.GetMeshDataWithAnim(mesh));
                visibleVerticesDataOnlyPos.AddRange(BaseMesh.GetMeshDataOnlyPos(mesh));
                visibleVerticesDataOnlyPosAndNormal.AddRange(BaseMesh.GetMeshDataOnlyPosAndNormal(mesh));
            }
        }
    }
}
