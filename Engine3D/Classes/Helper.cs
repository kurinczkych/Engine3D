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

    }
    public struct AABB
    {
        public Vector3 Min, Max;
        // ... Additional methods and properties ...
    }

    public class BVHNode
    {
        public AABB Bounds;
        public BVHNode Left, Right;
        public List<triangle> Triangles;
        public bool IsVisible = false;

        public bool IsLeaf => Left == null && Right == null;
    }

    public class BVH
    {
        public BVHNode Root;

        public BVH(List<triangle> triangles)
        {
            Root = Build(triangles);
        }

        private BVHNode Build(List<triangle> triangles)
        {
            BVHNode node = new BVHNode();
            node.Bounds = ComputeBounds(triangles);

            if (triangles.Count <= 3)  // Threshold can be adjusted
            {
                node.Triangles = triangles;
                return node;
            }

            List<triangle> leftTriangles, rightTriangles;
            SplitTriangles(triangles, out leftTriangles, out rightTriangles);

            if (leftTriangles.Count == 0 || rightTriangles.Count == 0)  // Failed to split
            {
                node.Triangles = triangles;
                return node;
            }

            node.Left = Build(leftTriangles);
            node.Right = Build(rightTriangles);

            return node;
        }

        private AABB ComputeBounds(List<triangle> triangles)
        {
            Vector3 min = new Vector3
            {
                X = float.MaxValue,
                Y = float.MaxValue,
                Z = float.MaxValue
            };

            Vector3 max = new Vector3
            {
                X = float.MinValue,
                Y = float.MinValue,
                Z = float.MinValue
            };

            foreach (var tri in triangles)
            {
                foreach (var point in tri.p)
                {
                    min.X = Math.Min(min.X, point.X);
                    min.Y = Math.Min(min.Y, point.Y);
                    min.Z = Math.Min(min.Z, point.Z);

                    max.X = Math.Max(max.X, point.X);
                    max.Y = Math.Max(max.Y, point.Y);
                    max.Z = Math.Max(max.Z, point.Z);
                }
            }

            return new AABB { Min = min, Max = max };
        }

        private void SplitTriangles(List<triangle> triangles, out List<triangle> left, out List<triangle> right)
        {
            // For simplicity, split by the longest axis of the AABB
            AABB bounds = ComputeBounds(triangles);
            Vector3 extent = new Vector3
            {
                X = bounds.Max.X - bounds.Min.X,
                Y = bounds.Max.Y - bounds.Min.Y,
                Z = bounds.Max.Z - bounds.Min.Z
            };

            float split;
            Func<Vector3, float> getValue;

            if (extent.X > extent.Y && extent.X > extent.Z)
            {
                split = (bounds.Max.X + bounds.Min.X) * 0.5f;
                getValue = v => v.X;
            }
            else if (extent.Y > extent.Z)
            {
                split = (bounds.Max.Y + bounds.Min.Y) * 0.5f;
                getValue = v => v.Y;
            }
            else
            {
                split = (bounds.Max.Z + bounds.Min.Z) * 0.5f;
                getValue = v => v.Z;
            }

            left = new List<triangle>();
            right = new List<triangle>();

            foreach (var tri in triangles)
            {
                Vector3 centroid = new Vector3
                {
                    X = (tri.p[0].X + tri.p[1].X + tri.p[2].X) / 3.0f,
                    Y = (tri.p[0].Y + tri.p[1].Y + tri.p[2].Y) / 3.0f,
                    Z = (tri.p[0].Z + tri.p[1].Z + tri.p[2].Z) / 3.0f
                };

                if (getValue(centroid) < split)
                    left.Add(tri);
                else
                    right.Add(tri);
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

                // ... and so on for bottom, top, left, and right faces
            };

            shader.Use();
            aabbVao.Bind();

            int modelLoc = GL.GetUniformLocation(shader.id, "modelMatrix");
            int viewLoc = GL.GetUniformLocation(shader.id, "viewMatrix");
            int projLoc = GL.GetUniformLocation(shader.id, "projectionMatrix");

            Matrix4 modelMatrix = Matrix4.Identity;
            Matrix4 projectionMatrix = camera.GetProjectionMatrix();
            Matrix4 viewMatrix = camera.GetViewMatrix();

            GL.UniformMatrix4(modelLoc, true, ref modelMatrix);
            GL.UniformMatrix4(viewLoc, true, ref viewMatrix);
            GL.UniformMatrix4(projLoc, true, ref projectionMatrix);

            aabbVbo.Buffer(vertices);

            // Draw the AABB using the indices
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count / 3);

            aabbVao.Unbind();
            aabbVbo.Unbind();
        }

        public static void PerformOcclusionQueriesForBVH(BVHNode node, VBO aabbVbo, VAO aabbVao, Shader shader, Camera camera)
        {
            if (node == null) return;

            int query;
            GL.GenQueries(1, out query);

            // 1. Initiate occlusion query
            GL.BeginQuery(QueryTarget.SamplesPassed, query);

            // 2. Render the AABB of the current BVH node
            RenderAABB(node.Bounds, aabbVbo, aabbVao, shader, camera);

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
                node.IsVisible = true;

                // Optionally, if you want to cull aggressively, you can stop here
                // and not process the children of this node since its bounding box is visible.
                // However, if you want more granular visibility checks, continue traversing the BVH.
                PerformOcclusionQueriesForBVH(node.Left, aabbVbo, aabbVao, shader, camera);
                PerformOcclusionQueriesForBVH(node.Right, aabbVbo, aabbVao, shader, camera);
            }
            else
            {
                node.IsVisible = false;
            }
        }

        public static void TraverseBVHNode(BVHNode node)
        {
            if (node == null)
                return;

            // If it's a leaf node (i.e., no children but has triangles)
            if (node.Left == null && node.Right == null && node.Triangles != null)
            {
                if (node.IsVisible)
                {
                    ;
                    
                }
            }
            else
            {
                TraverseBVHNode(node.Left);
                TraverseBVHNode(node.Right);
            }
        }
    }
}
