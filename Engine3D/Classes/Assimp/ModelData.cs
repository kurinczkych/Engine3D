using Assimp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class ModelData
    {
        public List<MeshData> meshes = new List<MeshData>();
        public Skeleton skeleton;
        public List<Matrix4> animationMatrices = new List<Matrix4>();

        public ModelData() { }
    }
}
