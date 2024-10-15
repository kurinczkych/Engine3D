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
        [JsonIgnore]
        public List<Assimp.Material> materials = new List<Assimp.Material>();

        public ModelData() { }
    }
}
