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

        public Projection projection = Projection.ShadowSmall;
        public Matrix4 projectionMatrix = Matrix4.Identity;

        public ShadowType shadowType;

        public Shadow(int size, ShadowType shadowType)
        {
            this.size = size;
            this.shadowType = shadowType;
        }
    }
}
