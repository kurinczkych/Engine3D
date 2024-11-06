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
        public Vector2 size;

        [JsonIgnore]
        public int fbo = -1;
        [JsonIgnore]
        public Texture shadowMap;

        public Projection projection = Projection.ShadowSmall;
        public Matrix4 projectionMatrixOrtho = Matrix4.Identity;

        public Shadow(Vector2 size)
        {
            this.size = size;
        }
    }
}
