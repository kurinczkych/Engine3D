using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Shadow
    {
        public int size;

        [JsonIgnore]
        public int fbo = -1;
        [JsonIgnore]
        public Texture shadowMap;

        public Projection projection = Projection.ShadowSmall;
        public Matrix4 projectionMatrix = Matrix4.Identity;

        public Shadow(int size)
        {
            this.size = size;
        }
    }
}
