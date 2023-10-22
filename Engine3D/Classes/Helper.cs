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
        public static Stream GetResourceStreamByNameEnd(string nameEnd)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (resourceName.EndsWith(nameEnd, StringComparison.OrdinalIgnoreCase))
                {
                    Stream? s = assembly.GetManifestResourceStream(resourceName);
                    if (s == null)
                        return Stream.Null;

                    return s;
                }
            }
            return Stream.Null; // or throw an exception if the resource is not found
        }

        public static void LoadTexture(string embeddedResourceName, bool flipY, TextureMinFilter tminf, TextureMagFilter tmagf)
        {
            // Load the image (using System.Drawing or another library)
            Stream stream = Helper.GetResourceStreamByNameEnd(embeddedResourceName);
            if (stream != Stream.Null)
            {
                using (stream)
                {
                    Bitmap bitmap = new Bitmap(stream);
                    if (flipY)
                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)tminf);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)tmagf);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                    bitmap.UnlockBits(data);

                    // Texture settings
                }
            }
            else
            {
                throw new Exception("No texture was found");
            }
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
    

    public static class GLHelper
    {
        private static void RenderAABB(AABB bounds, VBO aabbVbo, VAO aabbVao, Shader shader, Camera camera)
        {

            List<float> vertices = bounds.GetTriangleVertices();

            aabbVao.Bind();

            aabbVbo.Buffer(vertices);

            // Draw the AABB using the indices
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count / 3);
        }

        public static void PerformOcclusionQueriesForBVH(BVHNode node, VBO aabbVbo, VAO aabbVao, Shader shader, Camera camera)
        {
            shader.Use();

            int modelLoc = GL.GetUniformLocation(shader.id, "modelMatrix");
            int viewLoc = GL.GetUniformLocation(shader.id, "viewMatrix");
            int projLoc = GL.GetUniformLocation(shader.id, "projectionMatrix");

            Matrix4 modelMatrix = Matrix4.Identity;
            Matrix4 projectionMatrix = camera.GetProjectionMatrix();
            Matrix4 viewMatrix = camera.GetViewMatrix();

            GL.UniformMatrix4(modelLoc, true, ref modelMatrix);
            GL.UniformMatrix4(viewLoc, true, ref viewMatrix);
            GL.UniformMatrix4(projLoc, true, ref projectionMatrix);

            PerformOcclusionQueriesForBVHRecursive(node, aabbVbo, aabbVao, shader, camera);

            aabbVbo.Unbind();
            aabbVao.Unbind();

        }


        public static void PerformOcclusionQueriesForBVHRecursive(BVHNode node, VBO aabbVbo, VAO aabbVao, Shader shader, Camera camera)
        {
            if (node == null) return;

            int query;
            GL.GenQueries(1, out query);

            // 1. Initiate occlusion query
            GL.BeginQuery(QueryTarget.SamplesPassed, query);

            GL.DepthMask(false);
            // 2. Render the AABB of the current BVH node
            RenderAABB(node.bounds, aabbVbo, aabbVao, shader, camera);
            GL.DepthMask(true);

            // 3. End occlusion query
            GL.EndQuery(QueryTarget.SamplesPassed);

            // 4. Fetch the results
            int samplesPassed;
            GL.GetQueryObject(query, GetQueryObjectParam.QueryResult, out samplesPassed);

            // 5. Cleanup the query object
            GL.DeleteQueries(1, ref query);


            // 6. If there are samples passed, then mark the node as visible
            if (samplesPassed > 0)
            {
                node.isVisible = true;

                BVHNode firstChild = node.left;
                BVHNode secondChild = node.right;

                //PerformOcclusionQueriesForBVHRecursive(firstChild, aabbVbo, aabbVao, shader, camera);
                //PerformOcclusionQueriesForBVHRecursive(secondChild, aabbVbo, aabbVao, shader, camera);


                if (firstChild == null)
                    PerformOcclusionQueriesForBVH(secondChild, aabbVbo, aabbVao, shader, camera);
                else if (secondChild == null)
                    PerformOcclusionQueriesForBVH(firstChild, aabbVbo, aabbVao, shader, camera);
                else
                {
                    // Assuming your AABB has a method to compute its center
                    float distanceToLeft = (node.left.bounds.Center - camera.position).Length;
                    float distanceToRight = (node.right.bounds.Center - camera.position).Length;

                    if (distanceToRight < distanceToLeft)
                    {
                        firstChild = node.right;
                        secondChild = node.left;
                    }

                    PerformOcclusionQueriesForBVH(firstChild, aabbVbo, aabbVao, shader, camera);
                    PerformOcclusionQueriesForBVH(secondChild, aabbVbo, aabbVao, shader, camera);
                }
            }
            else
            {
                node.isVisible = false;
            }
        }

        public static double GTRandom(int index)
        {
            byte[] hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                // Compute hash from the index
                hash = sha256.ComputeHash(BitConverter.GetBytes(index));
            }

            // Convert the hash bytes to an int seed (we'll just use the first 4 bytes)
            int seed = BitConverter.ToInt32(hash, 0);

            // Use the seed to get a random number
            Random random = new Random(seed);
            return random.NextDouble();
        }

        public static void TraverseBVHNode(BVHNode node, ref List<triangle> notOccludedTris, ref int i)
        {
            if (node == null)
                return;



            //If it's a leaf node (i.e., no children but has triangles)
            if (node.left == null && node.right == null && node.triangles != null)
            {

                //Color4 c = new Color4((float)GTRandom(i), (float)GTRandom(i+1), (float)GTRandom(i+2), 1.0f);
                //i += 3;
                //List<triangle> colorTris = new List<triangle>(node.triangles);
                //colorTris.ForEach(x => x.SetColor(c));
                //notOccludedTris.AddRange(colorTris);

                if (node.isVisible)
                {
                    Color4 c = new Color4((float)GTRandom(i), (float)GTRandom(i + 1), (float)GTRandom(i + 2), 1.0f);
                    i += 3;
                    List<triangle> colorTris = new List<triangle>(node.triangles);
                    colorTris.ForEach(x => x.SetColor(c));
                    notOccludedTris.AddRange(colorTris);
                }
            }
            else
            {
                TraverseBVHNode(node.left, ref notOccludedTris, ref i);
                TraverseBVHNode(node.right, ref notOccludedTris, ref i);
            }
        }

        public static void TraverseBVHNode(BVHNode node)
        {
            if (node == null)
                return;



            // If it's a leaf node (i.e., no children but has triangles)
            if (node.left == null && node.right == null && node.triangles != null)
            {
                if (node.isVisible)
                {
                    ;
                }
            }
            else
            {
                TraverseBVHNode(node.left);
                TraverseBVHNode(node.right);
            }
        }
    }
}
