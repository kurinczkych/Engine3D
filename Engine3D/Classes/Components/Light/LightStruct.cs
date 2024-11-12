using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LightStruct
    {
        public Vector4 direction;
        public Vector4 position;
        public Vector4 color;

        public float constant;
        public float linear;
        public float quadratic;
        public float padding1;

        public Vector4 ambient;
        public Vector4 diffuse;
        public Vector4 specular;

        public float specularPow;
        public float padding2;
        public float padding3;
        public float padding4;

        public int lightType;
        public int padding5;
        public int padding6;
        public int padding7;

        public Matrix4 lightSpaceSmallMatrix;
        public Matrix4 lightSpaceMediumMatrix;
        public Matrix4 lightSpaceLargeMatrix;

        public float cascadeFarPlaneSmall;
        public float cascadeFarPlaneMedium;
        public float cascadeFarPlaneLarge;
        public float padding8;

        public Matrix4 lightSpaceTopMatrix;
        public Matrix4 lightSpaceBottomMatrix;
        public Matrix4 lightSpaceLeftMatrix;
        public Matrix4 lightSpaceRightMatrix;
        public Matrix4 lightSpaceFrontMatrix;
        public Matrix4 lightSpaceBackMatrix;
    }
}
