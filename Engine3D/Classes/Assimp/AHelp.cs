using Assimp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public static class AHelp
    {
        public static Vector3 AssimpToOpenTK(Vector3D v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
        public static Vector3D OpenTKToAssimp(Vector3 v)
        {
            return new Vector3D(v.X, v.Y, v.Z);
        }
    }
    
}
