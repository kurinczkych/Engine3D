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
        public static Random rnd = new Random((int)DateTime.Now.Ticks);

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

        public static Vector3 GetRandomVectorInAABB(AABB aabb)
        {
            float x = (float)(rnd.NextDouble() * (aabb.Max.X - aabb.Min.X) + aabb.Min.X);
            float y = (float)(rnd.NextDouble() * (aabb.Max.Y - aabb.Min.Y) + aabb.Min.Y);
            float z = (float)(rnd.NextDouble() * (aabb.Max.Z - aabb.Min.Z) + aabb.Min.Z);

            return new Vector3(x, y, z);
        }

        public static Vector3 GetRandomNormVector()
        {
            Vector3 randomVector;

            do
            {
                randomVector = new Vector3(
                    GetRandomNumberBetween(-1f, 1f),
                    GetRandomNumberBetween(-1f, 1f),
                    GetRandomNumberBetween(-1f, 1f)
                );
            } while (randomVector.Length < 0.1f); // Ensure the vector isn't too close to zero-length

            return Vector3.Normalize(randomVector);
        }

        public static Quaternion GetRandomQuaternion()
        {
            float u1 = (float)rnd.NextDouble();
            float u2 = (float)rnd.NextDouble();
            float u3 = (float)rnd.NextDouble();

            float sqrt1MinusU1 = MathF.Sqrt(1 - u1);
            float sqrtU1 = MathF.Sqrt(u1);

            float x = sqrt1MinusU1 * MathF.Sin(2 * MathF.PI * u2);
            float y = sqrt1MinusU1 * MathF.Cos(2 * MathF.PI * u2);
            float z = sqrtU1 * MathF.Sin(2 * MathF.PI * u3);
            float w = sqrtU1 * MathF.Cos(2 * MathF.PI * u3);

            return new Quaternion(x, y, z, w).Normalized();
        }

        public static Vector3 GetRandomScale(AABB aabb)
        {
            float x = (float)(rnd.NextDouble() * (aabb.Max.X - aabb.Min.X));
            float y = (float)(rnd.NextDouble() * (aabb.Max.Y - aabb.Min.Y));
            float z = (float)(rnd.NextDouble() * (aabb.Max.Z - aabb.Min.Z));

            return new Vector3(x, y, z);
        }

        public static Color4 GetRandomColor(bool includeAlpha = false)
        {
            float r = (float)rnd.NextDouble();
            float g = (float)rnd.NextDouble();
            float b = (float)rnd.NextDouble();
            float a = includeAlpha ? (float)rnd.NextDouble() : 1.0f;  // If you don't want a random alpha, it defaults to 1 (opaque).

            return new Color4(r, g, b, a);
        }

        public static Color4 LerpColor(Color4 a, Color4 b, float t)
        {
            return new Color4(
                a.R + t * (b.R - a.R),
                a.G + t * (b.G - a.G),
                a.B + t * (b.B - a.B),
                a.A + t * (b.A - a.A)
            );  
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        public static Vector3 GetForwardVectorFromQuaternion(Quaternion rotation)
        {
            Vector3 forward = new Vector3(0, 0, -1);
            return Vector3.Normalize(rotation * forward);
        }

        public static float GetRandomNumberBetween(float min, float max)
        {
            return (float)(rnd.NextDouble() * (max - min) + min);
        }

        public static Color4 ColorFromRGBA(float r, float g, float b, float a = 255.0f)
        {
            return new Color4(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
        }

        public static Vector3 EulerFromQuaternion(Quaternion quat)
        {
            Vector3 eulerAnglesRadians = quat.ToEulerAngles();
            Vector3 eulerAnglesDegrees = new Vector3(
                MathHelper.RadiansToDegrees(eulerAnglesRadians.X),
                MathHelper.RadiansToDegrees(eulerAnglesRadians.Y),
                MathHelper.RadiansToDegrees(eulerAnglesRadians.Z)
            );

            Vector3 normalizedEulerAngles = new Vector3(
                (float)Math.Round((eulerAnglesDegrees.X + 360) % 360,2),
                (float)Math.Round((eulerAnglesDegrees.Y + 360) % 360,2),
                (float)Math.Round((eulerAnglesDegrees.Z + 360) % 360,2)
            );

            return normalizedEulerAngles;
        }

        public static Quaternion QuaternionFromEuler(Vector3 rot)
        {
            Quaternion rotation = Quaternion.FromEulerAngles(
                MathHelper.DegreesToRadians(rot.X),
                MathHelper.DegreesToRadians(rot.Y),
                MathHelper.DegreesToRadians(rot.Z)
            );

            return rotation;
        }
    }
}
