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
    public class AABB
    {
        public Vector3 Min;
        public Vector3 Max;

        public Vector3 Center
        {
            get
            {
                return new Vector3(
                (Min.X + Max.X) * 0.5f,
                (Min.Y + Max.Y) * 0.5f,
                (Min.Z + Max.Z) * 0.5f);
            }
        }

        public AABB()
        {
            Min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }

        public void Enclose(triangle tri)
        {
            for (int i = 0; i < 3; i++)
            {
                Min = Helper.Vector3Min(Min, tri.p[i]);
                Max = Helper.Vector3Max(Max, tri.p[i]);
            }
        }
    }
    public class BVHNode
    {
        public AABB bounds;
        public BVHNode left;
        public BVHNode right;
        public List<triangle> triangles;

        public bool isVisible = false;
    }

    public class BVH
    {
        public BVHNode Root;

        public BVH(List<triangle> triangles)
        {
            Root = BuildBVH(triangles);
        }

        private BVHNode BuildBVH(List<triangle> triangles)
        {
            BVHNode node = new BVHNode();
            node.bounds = ComputeBounds(triangles);
            node.triangles = new List<triangle>();

            if (triangles.Count <= 3)  // leaf node
            {
                node.triangles.AddRange(triangles);
                return node;
            }

            // Split triangles into two groups
            var axis = node.bounds.Max - node.bounds.Min;
            int splitAxis = axis.X > axis.Y ? (axis.X > axis.Z ? 0 : 2) : (axis.Y > axis.Z ? 1 : 2);

            triangles.Sort((a, b) => a.p[0][splitAxis].CompareTo(b.p[0][splitAxis]));
            var half = triangles.Count / 2;

            node.left = BuildBVH(triangles.GetRange(0, half));
            node.right = BuildBVH(triangles.GetRange(half, triangles.Count - half));

            return node;
        }

        private AABB ComputeBounds(List<triangle> triangles)
        {
            AABB box = new AABB();

            foreach (var tri in triangles)
            {
                box.Enclose(tri);
            }

            return box;
        }

        public void WriteBVHToFile(string file)
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                WriteNode(writer, Root, 0);  // Start at depth 0
            }
        }


        private void WriteNode(StreamWriter writer, BVHNode node, int depth)
        {
            if (node == null) return;

            string indent = new string(' ', depth * 2);  // Indentation for visualization
            writer.WriteLine($"{indent}Node Bounds: {node.bounds.Min} to {node.bounds.Max}");

            writer.WriteLine($"{indent}  Triangle: {node.triangles.Count.ToString()}");  // Assuming your triangle has a simple representation
            writer.WriteLine($"{indent}  IsVisible: {node.isVisible}");  // Assuming your triangle has a simple representation
            if (node.triangles != null && node.triangles.Count > 0)  // It's a leaf node
            {
                writer.WriteLine($"{indent}  LEAFLEAFLEAFLEAFLEAFLEAF");  // Assuming your triangle has a simple representation
            }
            else
            {
                writer.WriteLine($"{indent}Left Child:");
                WriteNode(writer, node.left, depth + 1);

                writer.WriteLine($"{indent}Right Child:");
                WriteNode(writer, node.right, depth + 1);
            }
        }
    }

    public static class GLHelper
    {
        private static void RenderAABB(AABB bounds, VBO aabbVbo, VAO aabbVao, Shader shader, Camera camera)
        {
            List<float> vertices = new List<float>()
            {
                // Front face
                bounds.Min.X, bounds.Min.Y, bounds.Min.Z,
                bounds.Max.X, bounds.Min.Y, bounds.Min.Z,
                bounds.Max.X, bounds.Max.Y, bounds.Min.Z,

                bounds.Max.X, bounds.Max.Y, bounds.Min.Z,
                bounds.Min.X, bounds.Max.Y, bounds.Min.Z,
                bounds.Min.X, bounds.Min.Y, bounds.Min.Z,

                // Back face
                bounds.Min.X, bounds.Min.Y, bounds.Max.Z,
                bounds.Max.X, bounds.Min.Y, bounds.Max.Z,
                bounds.Max.X, bounds.Max.Y, bounds.Max.Z,

                bounds.Max.X, bounds.Max.Y, bounds.Max.Z,
                bounds.Min.X, bounds.Max.Y, bounds.Max.Z,
                bounds.Min.X, bounds.Min.Y, bounds.Max.Z,

                // Bottom face
                bounds.Min.X, bounds.Min.Y, bounds.Min.Z,
                bounds.Max.X, bounds.Min.Y, bounds.Min.Z,
                bounds.Max.X, bounds.Min.Y, bounds.Max.Z,

                bounds.Max.X, bounds.Min.Y, bounds.Max.Z,
                bounds.Min.X, bounds.Min.Y, bounds.Max.Z,
                bounds.Min.X, bounds.Min.Y, bounds.Min.Z,

                // Top face
                bounds.Min.X, bounds.Max.Y, bounds.Min.Z,
                bounds.Max.X, bounds.Max.Y, bounds.Min.Z,
                bounds.Max.X, bounds.Max.Y, bounds.Max.Z,

                bounds.Max.X, bounds.Max.Y, bounds.Max.Z,
                bounds.Min.X, bounds.Max.Y, bounds.Max.Z,
                bounds.Min.X, bounds.Max.Y, bounds.Min.Z,

                // Left face
                bounds.Min.X, bounds.Min.Y, bounds.Min.Z,
                bounds.Min.X, bounds.Max.Y, bounds.Min.Z,
                bounds.Min.X, bounds.Max.Y, bounds.Max.Z,

                bounds.Min.X, bounds.Max.Y, bounds.Max.Z,
                bounds.Min.X, bounds.Min.Y, bounds.Max.Z,
                bounds.Min.X, bounds.Min.Y, bounds.Min.Z,

                // Right face
                bounds.Max.X, bounds.Min.Y, bounds.Min.Z,
                bounds.Max.X, bounds.Max.Y, bounds.Min.Z,
                bounds.Max.X, bounds.Max.Y, bounds.Max.Z,

                bounds.Max.X, bounds.Max.Y, bounds.Max.Z,
                bounds.Max.X, bounds.Min.Y, bounds.Max.Z,
                bounds.Max.X, bounds.Min.Y, bounds.Min.Z,
            }; 
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

            //GL.DepthMask(false);
            // 2. Render the AABB of the current BVH node
            RenderAABB(node.bounds, aabbVbo, aabbVao, shader, camera);
            //GL.DepthMask(true);

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

                PerformOcclusionQueriesForBVHRecursive(firstChild, aabbVbo, aabbVao, shader, camera);
                PerformOcclusionQueriesForBVHRecursive(secondChild, aabbVbo, aabbVao, shader, camera);


                //if (firstChild == null)
                //    PerformOcclusionQueriesForBVH(secondChild, aabbVbo, aabbVao, shader, camera);
                //else if (secondChild == null)
                //    PerformOcclusionQueriesForBVH(firstChild, aabbVbo, aabbVao, shader, camera);
                //else
                //{
                //    // Assuming your AABB has a method to compute its center
                //    float distanceToLeft = (node.left.bounds.Center - camera.position).Length;
                //    float distanceToRight = (node.right.bounds.Center - camera.position).Length;

                //    if (distanceToRight < distanceToLeft)
                //    {
                //        firstChild = node.right;
                //        secondChild = node.left;
                //    }

                //    PerformOcclusionQueriesForBVH(firstChild, aabbVbo, aabbVao, shader, camera);
                //    PerformOcclusionQueriesForBVH(secondChild, aabbVbo, aabbVao, shader, camera);
                //}
            }
            else
            {
                node.isVisible = false;
            }
        }

        public static void TraverseBVHNode(BVHNode node, ref List<triangle> notOccludedTris)
        {
            if (node == null)
                return;



            //If it's a leaf node (i.e., no children but has triangles)
            if (node.left == null && node.right == null && node.triangles != null)
            {
                if (node.isVisible)
                {
                    notOccludedTris.AddRange(node.triangles);
                }
            }
            else
            {
                TraverseBVHNode(node.left, ref notOccludedTris);
                TraverseBVHNode(node.right, ref notOccludedTris);
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
