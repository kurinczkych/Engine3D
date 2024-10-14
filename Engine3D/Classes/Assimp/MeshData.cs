using Assimp;
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
        public List<float> visibleVerticesData = new List<float>();
        public List<float> visibleVerticesDataWithAnim = new List<float>();
        public List<float> visibleVerticesDataOnlyPos = new List<float>();
        public List<float> visibleVerticesDataOnlyPosAndNormal = new List<float>();

        public List<uint> visibleIndices = new List<uint>();
        public uint maxVisibleIndex = 0;

        public List<List<uint>> groupedIndices = new List<List<uint>>();

        public AABB Bounds = new AABB();

        public MeshData(Assimp.Mesh mesh)
        {
            this.mesh = mesh;
            foreach (Vector3D v in mesh.Vertices)
                Bounds.Enclose(v);

            CalculateGroupedIndices();
            visibleVerticesData.AddRange(BaseMesh.GetMeshData(mesh));
            visibleVerticesDataWithAnim.AddRange(BaseMesh.GetMeshDataWithAnim(mesh));
            visibleVerticesDataOnlyPos.AddRange(BaseMesh.GetMeshDataOnlyPos(mesh));
            visibleVerticesDataOnlyPosAndNormal.AddRange(BaseMesh.GetMeshDataOnlyPosAndNormal(mesh));
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
