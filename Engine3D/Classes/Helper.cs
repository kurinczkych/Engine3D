using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using MagicPhysX;
using static Engine3D.TextGenerator;
using System.IO;
using Cyotek.Drawing.BitmapFont;
using System.Security.Cryptography;

namespace Engine3D
{

    public static class Helper
    {
        public static Color4 CalcualteColorBasedOnDistance(float index, float maxIndex)
        {
            float c = InterpolateComponent(index, 0f, maxIndex, 1f, 0f);

            return new Color4(c, c, c, 1.0f);
        }

        public static float InterpolateComponent(float xComp, float inMinComp, float inMaxComp, float outMinComp, float outMaxComp)
        {
            return outMinComp + ((xComp - inMinComp) / (inMaxComp - inMinComp)) * (outMaxComp - outMinComp);
        }

        public static Vector3 Interpolate(Vector3 x, Vector3 inMin, Vector3 inMax, Vector3 outMin, Vector3 outMax)
        {
            return new Vector3(
                InterpolateComponent(x.X, inMin.X, inMax.X, outMin.X, outMax.X),
                InterpolateComponent(x.Y, inMin.Y, inMax.Y, outMin.Y, outMax.Y),
                InterpolateComponent(x.Z, inMin.Z, inMax.Z, outMin.Z, outMax.Z)
            );
        }

        public static Vector3 Vector3Min(Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        }

        public static Vector3 Vector3Max(Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        }

    }
}
