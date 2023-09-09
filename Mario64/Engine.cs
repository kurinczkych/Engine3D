using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.GL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static System.Net.Mime.MediaTypeNames;

namespace Mario64
{
    public class Engine : GameWindow
    {
        private struct triangle
        {
            public triangle()
            {
                p = new Vector3d[3];
                W = new double[] { 1.0f, 1.0f, 1.0f };
                color = Color4.White;
            }

            public triangle(Vector3d[] p)
            {
                this.p = p;
                W = new double[]{ 1.0f, 1.0f, 1.0f};
                color = Color4.White;
            }

            public triangle GetCopy()
            {
                triangle tri = new triangle();
                tri.p[0] = p[0];
                tri.p[1] = p[1];
                tri.p[2] = p[2];
                tri.W[0] = W[0];
                tri.W[1] = W[1];
                tri.W[2] = W[2];
                tri.color = color;

                return tri;
            }

            public static Vector3d GetVec3d(Vector4d v)
            {
                return new Vector3d(v);
            }

            public Vector4d GetVec4d(int index)
            {
                return new Vector4d(p[index], 1.0f);
            }

            public int CompareTo(triangle tri)
            {
                double z1 = (p[0].Z + p[1].Z + p[2].Z) / 3.0f;
                double z2 = (tri.p[0].Z + tri.p[1].Z + tri.p[2].Z) / 3.0f;

                if (z1 < z2)
                    return 1;
                else if (z1 > z2)
                    return -1;
                else
                    return 0;
            }

            public Vector3d[] p;
            public double[] W;
            public Color4 color;
        }

        private struct mesh
        {
            public mesh()
            {
                tris = new List<triangle>();
            }

            public void ProcessObj(string filename)
            {
                tris = new List<triangle>();

                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(filename));

                string result;
                List<Vector3d> verts = new List<Vector3d>();

                #pragma warning disable CS8600
                #pragma warning disable CS8604
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (true)
                    {
                        result = reader.ReadLine();
                        if (result != null && result.Length > 0)
                        {
                            if (result[0] == 'v')
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                Vector3d v = new Vector3d(double.Parse(vStr[0]), double.Parse(vStr[1]), double.Parse(vStr[2]));
                                verts.Add(v);
                            }
                            else if (result[0] == 'f')
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                int[] f = { int.Parse(vStr[0]), int.Parse(vStr[1]), int.Parse(vStr[2]) };

                                tris.Add(new triangle(new Vector3d[] { verts[f[0] - 1], verts[f[1] - 1], verts[f[2] - 1] }));
                            }
                        }

                        if (result == null)
                            break;
                    }
                }
                #pragma warning restore CS8600
                #pragma warning restore CS8604
            }

            public List<triangle> tris;
        }

        private int vertexSize;
        struct Vertex
        {
            public Vector3 Position;
            public Color4 Color;
        }


        #region Matrix stuff
        Matrix4d Matrix_MakeIdentity()
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = 1.0f;
            matrix.Row1[1] = 1.0f;
            matrix.Row2[2] = 1.0f;
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4d Matrix_MakeRotationX(double fAngleRad)
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = 1.0f;
            matrix.Row1[1] = Math.Cos(fAngleRad);
            matrix.Row1[2] = Math.Sin(fAngleRad);
            matrix.Row2[1] = -Math.Sin(fAngleRad);
            matrix.Row2[2] = Math.Cos(fAngleRad);
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4d Matrix_MakeRotationY(double fAngleRad)
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = Math.Cos(fAngleRad);
            matrix.Row0[2] = Math.Sin(fAngleRad);
            matrix.Row2[0] = -Math.Sin(fAngleRad);
            matrix.Row1[1] = 1.0f;
            matrix.Row2[2] = Math.Cos(fAngleRad);
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4d Matrix_MakeRotationZ(double fAngleRad)
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = Math.Cos(fAngleRad);
            matrix.Row0[1] = Math.Sin(fAngleRad);
            matrix.Row1[0] = -Math.Sin(fAngleRad);
            matrix.Row1[1] = Math.Cos(fAngleRad);
            matrix.Row2[2] = 1.0f;
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4d Matrix_MakeTranslation(double x, double y, double z)
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = 1.0f;
            matrix.Row1[1] = 1.0f;
            matrix.Row2[2] = 1.0f;
            matrix.Row3[3] = 1.0f;
            matrix.Row3[0] = x;
            matrix.Row3[1] = y;
            matrix.Row3[2] = z;
            return matrix;
        }

        Matrix4d Matrix_MakeProjection(double fFovDegrees, double fAspectRatio, double fNear, double fFar)
        {
            double fFovRad = 1.0f / Math.Tan(fFovDegrees * 0.5f / 180.0f * 3.14159f);
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = fAspectRatio * fFovRad;
            matrix.Row1[1] = fFovRad;
            matrix.Row2[2] = fFar / (fFar - fNear);
            matrix.Row3[2] = (-fFar * fNear) / (fFar - fNear);
            matrix.Row2[3] = 1.0f;
            matrix.Row3[3] = 0.0f;
            return matrix;
        }

        Matrix4d Matrix_PointAt(Vector3d pos, Vector3d target, Vector3d up)
        {
            // Calculate new forward direction
            Vector3d newForward = target - pos;
            newForward = Vector3d.Normalize(newForward);

            // Calculate new Up direction
            Vector3d a = newForward * Vector3d.Dot(up, newForward);
            Vector3d newUp = up - a;
            newUp = Vector3d.Normalize(newUp);

            // New Right direction is easy, its just cross product
            Vector3d newRight = Vector3d.Cross(newUp, newForward);

            // Construct Dimensioning and Translation Matrix	
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = newRight.X; matrix.Row0[1] = newRight.Y; matrix.Row0[2] = newRight.Z; matrix.Row0[3] = 0.0f;
            matrix.Row1[0] = newUp.X; matrix.Row1[1] = newUp.Y; matrix.Row1[2] = newUp.Z; matrix.Row1[3] = 0.0f;
            matrix.Row2[0] = newForward.X; matrix.Row2[1] = newForward.Y; matrix.Row2[2] = newForward.Z; matrix.Row2[3] = 0.0f;
            matrix.Row3[0] = pos.X; matrix.Row3[1] = pos.Y; matrix.Row3[2] = pos.Z; matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4d Matrix_QuickInverse(Matrix4d m)
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = m.Row0[0]; matrix.Row0[1] = m.Row1[0]; matrix.Row0[2] = m.Row2[0]; matrix.Row0[3] = 0.0f;
            matrix.Row1[0] = m.Row0[1]; matrix.Row1[1] = m.Row1[1]; matrix.Row1[2] = m.Row2[1]; matrix.Row1[3] = 0.0f;
            matrix.Row2[0] = m.Row0[2]; matrix.Row2[1] = m.Row1[2]; matrix.Row2[2] = m.Row2[2]; matrix.Row2[3] = 0.0f;
            matrix.Row3[0] = -(m.Row3[0] * matrix.Row0[0] + m.Row3[1] * matrix.Row1[0] + m.Row3[2] * matrix.Row2[0]);
            matrix.Row3[1] = -(m.Row3[0] * matrix.Row0[1] + m.Row3[1] * matrix.Row1[1] + m.Row3[2] * matrix.Row2[1]);
            matrix.Row3[2] = -(m.Row3[0] * matrix.Row0[2] + m.Row3[1] * matrix.Row1[2] + m.Row3[2] * matrix.Row2[2]);
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Tuple<Vector3d, double> Matrix_MultiplyVector(Matrix4d m, Vector3d i, double w)
        {
            Vector3d v;
            v.X = i.X * m.Row0[0] + i.Y * m.Row1[0] + i.Z * m.Row2[0] + w * m.Row3[0];
            v.Y = i.X * m.Row0[1] + i.Y * m.Row1[1] + i.Z * m.Row2[1] + w * m.Row3[1];
            v.Z = i.X * m.Row0[2] + i.Y * m.Row1[2] + i.Z * m.Row2[2] + w * m.Row3[2];
            double _w = i.X * m.Row0[3] + i.Y * m.Row1[3] + i.Z * m.Row2[3] + w * m.Row3[3];
            return new Tuple<Vector3d, double>(v, _w);
        }
        #endregion

        #region Clipping stuff
        Vector3d Vector_IntersectPlane(Vector3d plane_p, Vector3d plane_n, Vector3d lineStart, Vector3d lineEnd)
        {
            plane_n = Vector3d.Normalize(plane_n);
            double plane_d = -Vector3d.Dot(plane_n, plane_p);
            double ad = Vector3d.Dot(lineStart, plane_n);
            double bd = Vector3d.Dot(lineEnd, plane_n);
            double t = (-plane_d - ad) / (bd - ad);
            Vector3d lineStartToEnd = lineEnd - lineStart;
            Vector3d lineToIntersect = lineStartToEnd * t;
            return lineStart + lineToIntersect;
        }

        int Triangle_ClipAgainstPlane(Vector3d plane_p, Vector3d plane_n, triangle in_tri, ref triangle out_tri1, ref triangle out_tri2)
        {
            // Make sure plane normal is indeed normal
            plane_n = Vector3d.Normalize(plane_n);

            Func<Vector3d, double> dist = (Vector3d p) =>
            {
                Vector3d n = Vector3d.Normalize(p);
                return (plane_n.X * n.X + plane_n.Y * n.Y + plane_n.Z * n.Z - Vector3d.Dot(plane_n, plane_p));
            };

            // Create two temporary storage arrays to classify points either side of plane
            // If distance sign is positive, point lies on "inside" of plane
            Vector3d[] inside_points = new Vector3d[3]; int nInsidePointCount = 0;
            Vector3d[] outside_points = new Vector3d[3]; int nOutsidePointCount = 0;

            // Get signed distance of each point in triangle to plane
            double d0 = dist(in_tri.p[0]);
            double d1 = dist(in_tri.p[1]);
            double d2 = dist(in_tri.p[2]);

            if (d0 >= 0) { inside_points[nInsidePointCount++] = in_tri.p[0]; }
            else { outside_points[nOutsidePointCount++] = in_tri.p[0]; }
            if (d1 >= 0) { inside_points[nInsidePointCount++] = in_tri.p[1]; }
            else { outside_points[nOutsidePointCount++] = in_tri.p[1]; }
            if (d2 >= 0) { inside_points[nInsidePointCount++] = in_tri.p[2]; }
            else { outside_points[nOutsidePointCount++] = in_tri.p[2]; }

            // Now classify triangle points, and break the input triangle into 
            // smaller output triangles if required. There are four possible
            // outcomes...

            if (nInsidePointCount == 0)
            {
                // All points lie on the outside of plane, so clip whole triangle
                // It ceases to exist

                return 0; // No returned triangles are valid
            }

            if (nInsidePointCount == 3)
            {
                // All points lie on the inside of plane, so do nothing
                // and allow the triangle to simply pass through
                out_tri1 = in_tri.GetCopy();

                return 1; // Just the one returned original triangle is valid
            }

            if (nInsidePointCount == 1 && nOutsidePointCount == 2)
            {
                // Triangle should be clipped. As two points lie outside
                // the plane, the triangle simply becomes a smaller triangle

                // Copy appearance info to new triangle
                out_tri1.color = in_tri.color;

                // The inside point is valid, so keep that...
                out_tri1.p[0] = inside_points[0];

                // but the two new points are at the locations where the 
                // original sides of the triangle (lines) intersect with the plane
                out_tri1.p[1] = Vector_IntersectPlane(plane_p, plane_n, inside_points[0], outside_points[0]);
                out_tri1.p[2] = Vector_IntersectPlane(plane_p, plane_n, inside_points[0], outside_points[1]);

                return 1; // Return the newly formed single triangle
            }

            if (nInsidePointCount == 2 && nOutsidePointCount == 1)
            {
                // Triangle should be clipped. As two points lie inside the plane,
                // the clipped triangle becomes a "quad". Fortunately, we can
                // represent a quad with two new triangles

                // Copy appearance info to new triangles
                out_tri1.color = in_tri.color;

                out_tri2.color = in_tri.color;

                // The first triangle consists of the two inside points and a new
                // point determined by the location where one side of the triangle
                // intersects with the plane
                out_tri1.p[0] = inside_points[0];
                out_tri1.p[1] = inside_points[1];
                out_tri1.p[2] = Vector_IntersectPlane(plane_p, plane_n, inside_points[0], outside_points[0]);

                // The second triangle is composed of one of he inside points, a
                // new point determined by the intersection of the other side of the 
                // triangle and the plane, and the newly created point above
                out_tri2.p[0] = inside_points[1];
                out_tri2.p[1] = out_tri1.p[2];
                out_tri2.p[2] = Vector_IntersectPlane(plane_p, plane_n, inside_points[1], outside_points[0]);

                return 2; // Return two newly formed triangles which form a quad
            }

            return 0;
        }
        #endregion

        #region Wireframe drawing
        private void DrawPixel(double x, double y, Color4 color, bool scissorTest = true)
        {
            if (scissorTest)
                GL.Enable(EnableCap.ScissorTest);

            GL.Scissor((int)x, (int)y, 1, 1);
            GL.ClearColor(color);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            if (scissorTest)
                GL.Disable(EnableCap.ScissorTest);
        }
        private void DrawLine(double x1, double y1, double x2, double y2, Color4 color)
        {
            double x, y, dx, dy, dx1, dy1, px, py, xe, ye, i;
            dx = x2 - x1; dy = y2 - y1;
            dx1 = Math.Abs(dx); dy1 = Math.Abs(dy);
            px = 2 * dy1 - dx1; py = 2 * dx1 - dy1;
            if (dy1 <= dx1)
            {
                if (dx >= 0)
                { x = x1; y = y1; xe = x2; }
                else
                { x = x2; y = y2; xe = x1; }

                //DrawPixel(x, y, c, col);
                DrawPixel(x, y, color);

                for (i = 0; x < xe; i++)
                {
                    x = x + 1;
                    if (px < 0)
                        px = px + 2 * dy1;
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0)) y = y + 1; else y = y - 1;
                        px = px + 2 * (dy1 - dx1);
                    }
                    DrawPixel(x, y, color);
                }
            }
            else
            {
                if (dy >= 0)
                { x = x1; y = y1; ye = y2; }
                else
                { x = x2; y = y2; ye = y1; }

                DrawPixel(x, y, color);

                for (i = 0; y < ye; i++)
                {
                    y = y + 1;
                    if (py <= 0)
                        py = py + 2 * dx1;
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0)) x = x + 1; else x = x - 1;
                        py = py + 2 * (dx1 - dy1);
                    }
                    DrawPixel(x, y, color);
                }
            }
        }
        private void DrawTriangle(triangle tri, Color4 color)
        {
            int x1 = (int)tri.p[0].X;
            int y1 = (int)tri.p[0].Y;
            int x2 = (int)tri.p[1].X;
            int y2 = (int)tri.p[1].Y;
            int x3 = (int)tri.p[2].X;
            int y3 = (int)tri.p[2].Y;

            DrawLine(x1, y1, x2, y2, color);
            DrawLine(x2, y2, x3, y3, color);
            DrawLine(x3, y3, x1, y1, color);
        }
        #endregion

        // OPENGL
        private int vao;
        private int shaderProgram;
        private int vbo;
        private List<Vertex> vertices;
        private List<triangle> trisToRaster = new List<triangle>();

        // Program variables
        private Random rnd = new Random((int)DateTime.Now.Ticks);
        private int screenWidth;
        private int screenHeight;
        private int frameCount;
        private double totalTime;

        // Engine variables
        private mesh meshCube;
        private Matrix4d matProj;
        private Matrix4d matRotZ, matRotX, matTrans, matWorld;
        private double theta = 0f;

        private Vector3d camera;
        private Vector3d lookDir;
        private double yaw;

        public Engine(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            screenWidth = width;
            screenHeight = height;
            this.CenterWindow(new Vector2i(screenWidth, screenHeight));
            vertices = new List<Vertex>();
        }

        private void DrawFps(double deltaTime)
        {
            frameCount += 1;
            totalTime += deltaTime;

            double fps = (double)frameCount / totalTime;
            Title = "Mario 64    |    FPS: " + Math.Round(fps, 4).ToString();

            if (frameCount > 1000)
            {
                frameCount = 0;
                totalTime = 0;
            }
        }

        private void CalculateTriangles()
        {
            matRotZ = Matrix_MakeRotationZ(theta * 0.5f);
            matRotX = Matrix_MakeRotationX(theta);

            matTrans = Matrix_MakeTranslation(0.0f, 0.0f, 8.0f);

            matWorld = new Matrix4d();
            matWorld = Matrix_MakeIdentity();
            matWorld = Matrix4d.Mult(matRotZ, matRotX);
            matWorld = Matrix4d.Mult(matWorld, matTrans);

            Vector3d up = new Vector3d(0.0f, 1.0f, 0.0f);
            Vector3d target = new Vector3d(0.0f, 0.0f, 1.0f);
            Matrix4d matCameraRot = Matrix_MakeRotationY(yaw);

            var cameraP = Matrix_MultiplyVector(matCameraRot, target, 1.0f); 
            lookDir = cameraP.Item1;
            target = camera + lookDir;

            List<triangle> trisToClip = new List<triangle>();

            Matrix4d matCamera = Matrix_PointAt(camera, target, up);
            Matrix4d matView = Matrix_QuickInverse(matCamera);

            foreach (triangle tri in meshCube.tris)
            {
                triangle triTransormed = new triangle();
                // Offset to screen
                var p = Matrix_MultiplyVector(matWorld, tri.p[0], tri.W[0]); triTransormed.p[0] = p.Item1; triTransormed.W[0] = p.Item2;
                var p1 = Matrix_MultiplyVector(matWorld, tri.p[1], tri.W[1]); triTransormed.p[1] = p1.Item1; triTransormed.W[1] = p1.Item2;
                var p2 = Matrix_MultiplyVector(matWorld, tri.p[2], tri.W[2]); triTransormed.p[2] = p2.Item1; triTransormed.W[2] = p2.Item2;

                // Normal calculation
                Vector3d normal, line1, line2;
                line1 = triTransormed.p[1] - triTransormed.p[0];
                line2 = triTransormed.p[2] - triTransormed.p[0];

                normal = Vector3d.Cross(line1, line2);
                normal.Normalize();

                Vector3d cameraRay = triTransormed.p[0] - camera;
                // If not visible, continue
                if (Vector3d.Dot(normal, cameraRay) >= 0)
                    continue;

                Vector3d lightDir = new Vector3d(0.0f, 0.0f, -1.0f);
                lightDir.Normalize();
                double dp = Math.Max(0.1f, Vector3d.Dot(normal, lightDir));
                triTransormed.color = new Color4((float)dp, (float)dp, (float)dp, 1.0f);

                // Convert world space to view space
                triangle triViewed = new triangle();
                triViewed.color = triTransormed.color;
                p = Matrix_MultiplyVector(matView, triTransormed.p[0], triTransormed.W[0]); triViewed.p[0] = p.Item1; triViewed.W[0] = p.Item2;
                p1 = Matrix_MultiplyVector(matView, triTransormed.p[1], triTransormed.W[1]); triViewed.p[1] = p1.Item1; triViewed.W[1] = p1.Item2;
                p2 = Matrix_MultiplyVector(matView, triTransormed.p[2], triTransormed.W[2]); triViewed.p[2] = p2.Item1; triViewed.W[2] = p2.Item2;

                int nClippedTriangles = 0;
                triangle clipped1 = new triangle();
                triangle clipped2 = new triangle();
                nClippedTriangles = Triangle_ClipAgainstPlane(new Vector3d(0.0f, 0.0f, 0.1f), new Vector3d(0.0f, 0.0f, 1.0f), triViewed, ref clipped1, ref clipped2);
                triangle[] clipped = new triangle[2] { clipped1, clipped2 };

                for (int n = 0; n < nClippedTriangles; n++)
                {
                    triangle triProjected = new triangle();

                    // Project triangles from 3D to 2D
                    p = Matrix_MultiplyVector(matProj, clipped[n].p[0], clipped[n].W[0]); triProjected.p[0] = p.Item1; triProjected.W[0] = p.Item2;
                    p1 = Matrix_MultiplyVector(matProj, clipped[n].p[1], clipped[n].W[1]); triProjected.p[1] = p1.Item1; triProjected.W[1] = p1.Item2;
                    p2 = Matrix_MultiplyVector(matProj, clipped[n].p[2], clipped[n].W[2]); triProjected.p[2] = p2.Item1; triProjected.W[2] = p2.Item2;
                    triProjected.color = clipped[n].color;

                    triProjected.p[0] = triProjected.p[0] / triProjected.W[0];
                    triProjected.p[1] = triProjected.p[1] / triProjected.W[1];
                    triProjected.p[2] = triProjected.p[2] / triProjected.W[2];

                    // Scale into view
                    Vector3d offsetView = new Vector3d(1.0f, 1.0f, 0.0f);
                    triProjected.p[0] += offsetView;
                    triProjected.p[1] += offsetView;
                    triProjected.p[2] += offsetView;

                    triProjected.p[0] *= new Vector3d(0.5f * (double)screenWidth, 0.5f * (double)screenHeight, 1.0f);
                    triProjected.p[1] *= new Vector3d(0.5f * (double)screenWidth, 0.5f * (double)screenHeight, 1.0f);
                    triProjected.p[2] *= new Vector3d(0.5f * (double)screenWidth, 0.5f * (double)screenHeight, 1.0f);

                    trisToClip.Add(triProjected);
                }
            }

                // Sort triangles back to front
            trisToClip.Sort((a, b) => a.CompareTo(b));

            trisToRaster = new List<triangle>();
            foreach (triangle triToClip in trisToClip)
            {
                // Clip triangles against all four screen edges, this could yield
                // a bunch of triangles, so create a queue that we traverse to 
                //  ensure we only test new triangles generated against planes
                triangle[] clippedNew = new triangle[2] { new triangle(), new triangle() };
                List<triangle> listTriangles = new List<triangle>() { triToClip };

                int nNewTriangles = 1;

                for (int p_ = 0; p_ < 4; p_++)
                {
                    int nTrisToAdd = 0;
                    while (nNewTriangles > 0)
                    {
                        // Take triangle from front of queue
                        triangle test = listTriangles.First();
                        listTriangles.RemoveAt(0);
                        nNewTriangles--;

                        // Clip it against a plane. We only need to test each 
                        // subsequent plane, against subsequent new triangles
                        // as all triangles after a plane clip are guaranteed
                        // to lie on the inside of the plane. I like how this
                        // comment is almost completely and utterly justified

                        if (p_ == 0)
                            nTrisToAdd = Triangle_ClipAgainstPlane(new Vector3d(0.0f, 0.0f, 0.0f), new Vector3d(0.0f, 1.0f, 0.0f), test, ref clippedNew[0], ref clippedNew[1]);

                        if (p_ == 1)
                            nTrisToAdd = Triangle_ClipAgainstPlane(new Vector3d(0.0f, (float)screenHeight - 1, 0.0f), new Vector3d(0.0f, -1.0f, 0.0f), test, ref clippedNew[0], ref clippedNew[1]);

                        if (p_ == 2)
                            nTrisToAdd = Triangle_ClipAgainstPlane(new Vector3d(0.0f, 0.0f, 0.0f), new Vector3d(1.0f, 0.0f, 0.0f), test, ref clippedNew[0], ref clippedNew[1]);

                        if (p_ == 3)
                            nTrisToAdd = Triangle_ClipAgainstPlane(new Vector3d((float)screenWidth - 1, 0.0f, 0.0f), new Vector3d(-1.0f, 0.0f, 0.0f), test, ref clippedNew[0], ref clippedNew[1]);

                        // Clipping may yield a variable number of triangles, so
                        // add these new ones to the back of the queue for subsequent
                        // clipping against next planes

                        for (int w = 0; w < nTrisToAdd; w++)
                            listTriangles.Add(clippedNew[w]);
                    }
                    nNewTriangles = listTriangles.Count();
                }

                foreach (triangle listTriangle in listTriangles)
                    trisToRaster.Add(listTriangle);
            }
        }


        public double ConvertRange( double originalStart, double originalEnd, double newStart, double newEnd, double value) 
        {
            double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (double)(newStart + ((value - originalStart) * scale)); 
        }

        Vertex ConvertToNDC(Vector3d screenPos, Color4 color)
        {
            //float x = (float)ConvertRange(0, screenWidth, -1, 1, screenPos[0]);
            //float y = (float)ConvertRange(0, screenHeight, -1, 1, screenPos[1]);

            float x = (2.0f * (float)screenPos.X / screenWidth) - 1.0f;
            float y = (2.0f * (float)screenPos.Y / screenHeight) - 1.0f;

            return new Vertex() { Position = new Vector3(x, y, 1.0f), Color = color };
        }

        void ConvertListToNDC(List<triangle> tris)
        {
            vertices = new List<Vertex>();
            foreach (var tri in tris)
            {
                Color4 c = new Color4((float)rnd.Next(0, 100) / 100f, (float)rnd.Next(0, 100) / 100f, (float)rnd.Next(0, 100) / 100f, 1.0f);
                //vertices.Add(ConvertToNDC(tri.p[0], tri.color));
                //vertices.Add(ConvertToNDC(tri.p[1], tri.color));
                //vertices.Add(ConvertToNDC(tri.p[2], tri.color));
                vertices.Add(ConvertToNDC(tri.p[0], c));
                vertices.Add(ConvertToNDC(tri.p[1], c));
                vertices.Add(ConvertToNDC(tri.p[2], c));
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.ClearColor(Color4.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            DrawFps(args.Time);

            GL.UseProgram(shaderProgram); // bind vao
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, vertexSize, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            GL.DisableVertexAttribArray(0);

            foreach (triangle tri in trisToRaster)
            {
                DrawTriangle(tri, Color4.Red);
            }

            Context.SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);


            if (IsKeyDown(Keys.Space))
            {
                camera.Y += 8.0f * args.Time;
            }
            if (IsKeyDown(Keys.LeftShift))
            {
                camera.Y -= 8.0f * args.Time;
            }

            Vector3d forward = lookDir * (4.0f * args.Time);
            if (IsKeyDown(Keys.W))
            {
                camera += forward;
            }
            if (IsKeyDown(Keys.S))
            {
                camera -= forward;
            }
            if (IsKeyDown(Keys.A))
            {
                camera.X -= 8.0f * args.Time;
            }
            if (IsKeyDown(Keys.D))
            {
                camera.X += 8.0f * args.Time;
            }

            if (IsKeyDown(Keys.Left))
            {
                yaw += 2.0f * args.Time;
            }
            if (IsKeyDown(Keys.Right))
            {
                yaw -= 2.0f * args.Time;
            }

            CalculateTriangles();
            ConvertListToNDC(trisToRaster);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * vertexSize, vertices.ToArray(), BufferUsageHint.StaticDraw);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            vertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex));

            // OPENGL init
            vao = GL.GenVertexArray();

            // generate a buffer
            GL.GenBuffers(1, out vbo);

            // bind the vao
            GL.BindVertexArray(vao);
            // point slot (0) of the VAO to the currently bound VBO (vbo)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, 0);
            // enable the slot
            //GL.EnableVertexArrayAttrib(vao, 0);
            GL.EnableVertexAttribArray(0);


            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, vertexSize, 3 * sizeof(float));
            //GL.EnableVertexArrayAttrib(vao, 1);
            GL.EnableVertexAttribArray(1);


            // create the shader program
            shaderProgram = GL.CreateProgram();

            // create the vertex shader
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            // add the source code from "Default.vert" in the Shaders file
            GL.ShaderSource(vertexShader, LoadShaderSource("Default.vert"));
            // Compile the Shader
            GL.CompileShader(vertexShader);

            // Same as vertex shader
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, LoadShaderSource("Default.frag"));
            GL.CompileShader(fragmentShader);

            // Attach the shaders to the shader program
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);

            // Link the program to OpenGL
            GL.LinkProgram(shaderProgram);

            // delete the shaders
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // Projection matrix and mesh loading
            meshCube.ProcessObj("cube.obj");

            //Projection matrix
            double near = 0.1f;
            double far = 1000.0f;
            double fov = 90.0f;
            double aspectRatio = (double)screenHeight / (double)screenWidth;
            double fovRad = 1.0f / Math.Tan(fov * 0.5f / 180.0f * Math.PI);

            matProj = Matrix_MakeProjection(fov, aspectRatio, near, far);
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo);
            GL.DeleteProgram(shaderProgram);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            screenWidth = e.Width;
            screenHeight = e.Height;
        }

        public string LoadShaderSource(string filePath)
        {
            string shaderSource = "";

            try
            {
                using (StreamReader reader = new StreamReader("../../../Shaders/" + filePath))
                {
                    shaderSource = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load shader source file: " + e.Message);
            }

            return shaderSource;
        }
    }
}
