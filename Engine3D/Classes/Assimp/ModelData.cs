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
    public class ModelData
    {
        public List<MeshData> meshes = new List<MeshData>();
        public Skeleton skeleton;
        [JsonConverter(typeof(Matrix4ListConverter))]
        public List<Matrix4> boneMatrices = new List<Matrix4>();
        public int boneCount = 0;

        public ModelData() { }
    }
}
