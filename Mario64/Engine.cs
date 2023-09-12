using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
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
using System.Drawing;
using System.Drawing.Imaging;
using System.ComponentModel.Design;

#pragma warning disable CS8600
#pragma warning disable CS8604

namespace Mario64
{
    public class Vec3d
    {
        public Vec3d()
        {
            W = 1.0f;
            color = Color4.White;
        }

        public Vec3d(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
            W = 1.0f;
            color = Color4.White;
        }

        public float X;
        public float Y;
        public float Z;
        public float W;

        public Vec3d GetCopy()
        {
            Vec3d v = new Vec3d(X, Y, Z);
            v.W = W;
            v.color = color;
            return v;
        }

        public static float Dot(Vec3d v1, Vec3d v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public static Vec3d Cross(Vec3d v1, Vec3d v2)
        {
            Vec3d v = new Vec3d();
            v.X = v1.Y * v2.Z - v1.Z * v2.Y;
            v.Y = v1.Z * v2.X - v1.X * v2.Z;
            v.Z = v1.X * v2.Y - v1.Y * v2.X;
            v.W = v1.W;
            v.color = v1.color;
            return v;
        }

        public static Vec3d Normalize(Vec3d v)
        {
            float l = v.Length;
            Vec3d v2 = new Vec3d(v.X / l, v.Y / l, v.Z / l);
            v2.W = v.W;
            v2.color = v.color;
            return v;
        }

        public void Normalize()
        {
            float l = Length;
            X /= l;
            Y /= l;
            Z /= l;
        }

        #region Operator overloads
        public static Vec3d operator -(Vec3d v1, Vec3d v2)
        {
            Vec3d v3 = new Vec3d(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
            v3.W = v1.W;
            v3.color = v1.color;
            return v3;
        }
        public static Vec3d operator +(Vec3d v1, Vec3d v2)
        {
            Vec3d v3 = new Vec3d(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
            v3.W = v1.W;
            v3.color = v1.color;
            return v3;
        }
        public static Vec3d operator /(Vec3d v1, float d)
        {
            if (d == 0)
                return v1;
            Vec3d v3 = new Vec3d(v1.X / d, v1.Y / d, v1.Z / d);
            v3.W = v1.W;
            v3.color = v1.color;
            return v3;
        }
        public static Vec3d operator *(Vec3d v1, float d)
        {
            Vec3d v3 = new Vec3d(v1.X * d, v1.Y * d, v1.Z * d);
            v3.W = v1.W;
            v3.color = v1.color;
            return v3;
        }
        #endregion

        public Color4 color;
        public float Length
        {
            get
            {
                return (float)Math.Sqrt(Vec3d.Dot(this, this));
            }
        }
    }

    public class Engine : GameWindow
    {
        class Vec2d
        {
            public Vec2d() { }

            public Vec2d(float u, float v)
            {
                this.u = u;
                this.v = v;
                this.w = 1.0f;
            }

            public float u = 0.0f;
            public float v = 0.0f;
            public float w;


            public Vec2d GetCopy()
            {
                Vec2d v2 = new Vec2d(u, v);
                v2.w = w;
                return v2;
            }
        }


        private class triangle
        {
            public triangle()
            {
                p = new Vec3d[3];
                t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
            }

            public triangle(Vec3d[] p)
            {
                this.p = new Vec3d[p.Length];
                for (int p_ = 0; p_ < p.Length; p_++)
                {
                    this.p[p_] = p[p_].GetCopy();
                }

                t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
            }

            public triangle(Vec3d[] p, Vec2d[] t)
            {
                this.p = new Vec3d[p.Length];
                this.t = new Vec2d[t.Length];
                for (int p_ = 0; p_ < p.Length; p_++)
                {
                    this.p[p_] = p[p_].GetCopy();
                }
                for (int t_ = 0; t_ < t.Length; t_++)
                {
                    this.t[t_] = t[t_].GetCopy();
                }
            }

            public Vec3d GetMiddle()
            {
                return (p[0] + p[1] + p[2]) / 3;
            }

            public void Color(Color4 c)
            {
                foreach (Vec3d p_ in p)
                    p_.color = c;
            }

            public void Color(triangle t)
            {
                for (int p_ = 0; p_ < p.Length; p_++)
                {
                    p[p_].color = t.p[p_].color;
                }
            }

            public string GetPointsStr()
            {
                return p[0].ToString() + p[1].ToString() + p[2].ToString();
            }

            public Vec3d ComputeNormal()
            {
                Vec3d normal, line1, line2;
                line1 = p[1] - p[0];
                line2 = p[2] - p[0];

                normal = Vec3d.Cross(line1, line2);
                normal.Normalize();
                return normal;
            }

            public triangle GetCopy()
            {
                triangle tri = new triangle();
                tri.p[0] = p[0].GetCopy();
                tri.p[1] = p[1].GetCopy();
                tri.p[2] = p[2].GetCopy();
                tri.t[0] = t[0].GetCopy();
                tri.t[1] = t[1].GetCopy();
                tri.t[2] = t[2].GetCopy();

                return tri;
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

            public Vec3d[] p;
            public Vec2d[] t;
        }

        private struct mesh
        {
            public mesh()
            {
                tris = new List<triangle>();
            }

            public void OnlyCube()
            {
                tris = new List<triangle>
                {
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(1.0f, 1.0f, 0.0f), new Vec3d(1.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 0.0f), new Vec3d(1.0f, 1.0f, 0.0f), new Vec3d(1.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 0.0f), new Vec3d(1.0f, 1.0f, 1.0f), new Vec3d(1.0f, 0.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(1.0f, 1.0f, 1.0f), new Vec3d(0.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(0.0f, 1.0f, 1.0f), new Vec3d(0.0f, 0.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 1.0f), new Vec3d(0.0f, 1.0f, 1.0f), new Vec3d(0.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 1.0f), new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(0.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(0.0f, 1.0f, 1.0f), new Vec3d(1.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(1.0f, 1.0f, 1.0f), new Vec3d(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(0.0f, 0.0f, 1.0f), new Vec3d(0.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(1.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) })
                };
            }
                
            public void OnlyTriangle()
            {
                tris = new List<triangle>
                {
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) })                
                };
            }

            public void ProcessObj(string filename)
            {
                tris = new List<triangle>();

                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(filename));

                string result;
                List<Vec3d> verts = new List<Vec3d>();
                List<Vec2d> uvs = new List<Vec2d>();
                
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
                                if (result[1] == 't')
                                {
                                    string[] vStr = result.Substring(3).Split(" ");
                                    var a = float.Parse(vStr[0]);
                                    var b = float.Parse(vStr[1]);
                                    Vec2d v = new Vec2d(a, b);
                                    uvs.Add(v);
                                }
                                else if (result[1] == 'n')
                                {
                                    //string[] vStr = result.Substring(3).Split(" ");
                                    //var a = float.Parse(vStr[0]);
                                    //var b = float.Parse(vStr[1]);
                                    //Vec2d v = new Vec2d(a, b);
                                    //uvs.Add(v);
                                }
                                else
                                {
                                    string[] vStr = result.Substring(2).Split(" ");
                                    var a = float.Parse(vStr[0]);
                                    var b = float.Parse(vStr[1]);
                                    var c = float.Parse(vStr[2]);
                                    Vec3d v = new Vec3d(a, b, c);
                                    verts.Add(v);
                                }
                            }
                            else if (result[0] == 'f')
                            {
                                if(result.Contains("//"))
                                {

                                }
                                else if (result.Contains("/"))
                                {
                                    string[] vStr = result.Substring(2).Split(" ");
                                    if (vStr.Length > 3)
                                        throw new Exception();

                                    // 1/1, 2/2, 3/3
                                    int[] v = new int[3];
                                    int[] uv = new int[3];
                                    for(int i = 0; i < 3; i++)
                                    {
                                        string[] fStr = vStr[i].Split("/");
                                        v[i] = int.Parse(fStr[0]);
                                        uv[i] = int.Parse(fStr[1]);
                                    }

                                    tris.Add(new triangle(new Vec3d[] { verts[v[0] - 1], verts[v[1] - 1], verts[v[2] - 1] },
                                                          new Vec2d[] { uvs[uv[0] - 1], uvs[uv[1] - 1], uvs[uv[2] - 1] }));
                                }
                                else
                                {
                                    string[] vStr = result.Substring(2).Split(" ");
                                    int[] f = { int.Parse(vStr[0]), int.Parse(vStr[1]), int.Parse(vStr[2]) };

                                    tris.Add(new triangle(new Vec3d[] { verts[f[0] - 1], verts[f[1] - 1], verts[f[2] - 1] }));
                                }
                            }
                        }

                        if (result == null)
                            break;
                    }
                }
            }

            public List<triangle> tris;
        }

        private int vertexSize;
        struct Vertex
        {
            public Vector4 Position;
            public Vector3 Normal;
            public Vector2 Texture;
            public Vector3 Camera;
        }


        #region Matrix stuff
        Matrix4 Matrix_MakeIdentity()
        {
            Matrix4 matrix = new Matrix4();
            matrix.Row0[0] = 1.0f;
            matrix.Row1[1] = 1.0f;
            matrix.Row2[2] = 1.0f;
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4 Matrix_MakeRotationX(float fAngleRad)
        {
            Matrix4 matrix = new Matrix4();
            matrix.Row0[0] = 1.0f;
            matrix.Row1[1] = (float)Math.Cos(fAngleRad);
            matrix.Row1[2] = (float)Math.Sin(fAngleRad);
            matrix.Row2[1] = -(float)Math.Sin(fAngleRad);
            matrix.Row2[2] = (float)Math.Cos(fAngleRad);
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4 Matrix_MakeRotationY(float fAngleRad)
        {
            Matrix4 matrix = new Matrix4();
            matrix.Row0[0] = (float)Math.Cos(fAngleRad);
            matrix.Row0[2] = (float)Math.Sin(fAngleRad);
            matrix.Row2[0] = -(float)Math.Sin(fAngleRad);
            matrix.Row1[1] = 1.0f;
            matrix.Row2[2] = (float)Math.Cos(fAngleRad);
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4 Matrix_MakeRotationZ(float fAngleRad)
        {
            Matrix4 matrix = new Matrix4();
            matrix.Row0[0] = (float)Math.Cos(fAngleRad);
            matrix.Row0[1] = (float)Math.Sin(fAngleRad);
            matrix.Row1[0] = -(float)Math.Sin(fAngleRad);
            matrix.Row1[1] = (float)Math.Cos(fAngleRad);
            matrix.Row2[2] = 1.0f;
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4 Matrix_MakeTranslation(float x, float y, float z)
        {
            Matrix4 matrix = new Matrix4();
            matrix.Row0[0] = 1.0f;
            matrix.Row1[1] = 1.0f;
            matrix.Row2[2] = 1.0f;
            matrix.Row3[3] = 1.0f;
            matrix.Row3[0] = x;
            matrix.Row3[1] = y;
            matrix.Row3[2] = z;
            return matrix;
        }

        Matrix4 Matrix_MakeProjection(float fFovDegrees, float fAspectRatio, float fNear, float fFar)
        {
            float fFovRad = 1.0f / (float)Math.Tan(fFovDegrees * 0.5f / 180.0f * Math.PI);
            Matrix4 matrix = new Matrix4();
            matrix.Row0[0] = fAspectRatio * fFovRad;
            matrix.Row1[1] = fFovRad;
            matrix.Row2[2] = fFar / (fFar - fNear);
            matrix.Row3[2] = (-fFar * fNear) / (fFar - fNear);
            matrix.Row2[3] = 1.0f;
            matrix.Row3[3] = 0.0f;
            return matrix;
        }

        Matrix4 Matrix_PointAt(Vec3d pos, Vec3d target, Vec3d up)
        {
            // Calculate new forward direction
            Vec3d newForward = target - pos;
            newForward = Vec3d.Normalize(newForward);

            // Calculate new Up direction
            Vec3d a = newForward * Vec3d.Dot(up, newForward);
            Vec3d newUp = up - a;
            newUp = Vec3d.Normalize(newUp);

            // New Right direction is easy, its just cross product
            Vec3d newRight = Vec3d.Cross(newUp, newForward);

            // Construct Dimensioning and Translation Matrix	
            Matrix4 matrix = new Matrix4();
            matrix.Row0[0] = newRight.X; matrix.Row0[1] = newRight.Y; matrix.Row0[2] = newRight.Z; matrix.Row0[3] = 0.0f;
            matrix.Row1[0] = newUp.X; matrix.Row1[1] = newUp.Y; matrix.Row1[2] = newUp.Z; matrix.Row1[3] = 0.0f;
            matrix.Row2[0] = newForward.X; matrix.Row2[1] = newForward.Y; matrix.Row2[2] = newForward.Z; matrix.Row2[3] = 0.0f;
            matrix.Row3[0] = pos.X; matrix.Row3[1] = pos.Y; matrix.Row3[2] = pos.Z; matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4 Matrix_QuickInverse(Matrix4 m)
        {
            Matrix4 matrix = new Matrix4();
            matrix.Row0[0] = m.Row0[0]; matrix.Row0[1] = m.Row1[0]; matrix.Row0[2] = m.Row2[0]; matrix.Row0[3] = 0.0f;
            matrix.Row1[0] = m.Row0[1]; matrix.Row1[1] = m.Row1[1]; matrix.Row1[2] = m.Row2[1]; matrix.Row1[3] = 0.0f;
            matrix.Row2[0] = m.Row0[2]; matrix.Row2[1] = m.Row1[2]; matrix.Row2[2] = m.Row2[2]; matrix.Row2[3] = 0.0f;
            matrix.Row3[0] = -(m.Row3[0] * matrix.Row0[0] + m.Row3[1] * matrix.Row1[0] + m.Row3[2] * matrix.Row2[0]);
            matrix.Row3[1] = -(m.Row3[0] * matrix.Row0[1] + m.Row3[1] * matrix.Row1[1] + m.Row3[2] * matrix.Row2[1]);
            matrix.Row3[2] = -(m.Row3[0] * matrix.Row0[2] + m.Row3[1] * matrix.Row1[2] + m.Row3[2] * matrix.Row2[2]);
            matrix.Row3[3] = 1.0f;
            return matrix;
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
        private float theta = 0f;

        private Camera camera = new Camera();
        private float yaw;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;

        public Engine(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

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

        Vertex ConvertToNDC(Vec3d screenPos, Vec2d tex, Vec3d normal)
        {
            return new Vertex()
            {
                Position = new Vector4(screenPos.X, screenPos.Y, screenPos.Z, screenPos.W),
                Normal = new Vector3(normal.X, normal.Y, normal.Z),
                Texture = new Vector2(tex.u, tex.v),
                Camera = new Vector3(0f,0f,0f)
            };
        }

        void ConvertListToNDC(List<triangle> tris)
        {
            vertices = new List<Vertex>();
            foreach (var tri in tris)
            {
                //Vec3d m = tri.GetMiddle();
                //Vec3d cameraToTri = camera.position_ - m;
                //if (Math.Abs(cameraToTri.Length) > 50)
                //    continue;

                Vec3d normal = tri.ComputeNormal();
                vertices.Add(ConvertToNDC(tri.p[0], tri.t[0], normal));
                vertices.Add(ConvertToNDC(tri.p[1], tri.t[1], normal));
                vertices.Add(ConvertToNDC(tri.p[2], tri.t[2], normal));
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.ClearColor(Color4.Cyan);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            DrawFps(args.Time);

            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            GL.DisableVertexAttribArray(0);

            Context.SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            camera.Update(KeyboardState, MouseState, args);

            if (IsKeyDown(Keys.Left))
            {
                yaw += 2.0f * (float)args.Time;
            }
            if (IsKeyDown(Keys.Right))
            {
                yaw -= 2.0f * (float)args.Time;
            }

            trisToRaster = new List<triangle>();
            foreach (triangle tri in meshCube.tris)
            {
                trisToRaster.Add(tri);
            }

            int viewMatrixLocation = GL.GetUniformLocation(shaderProgram, "viewMatrix");
            int modelMatrixLocation = GL.GetUniformLocation(shaderProgram, "modelMatrix");

            //Vec3d up = new Vec3d(0.0f, 1.0f, 0.0f);
            //Vec3d target = new Vec3d(0.0f, 0.0f, 1.0f);
            //Matrix4 matCameraRot = Matrix_MakeRotationY(yaw);
            //var cameraP = Matrix_MultiplyVector(matCameraRot, target);
            //lookDir = cameraP.GetCopy();
            //target = camera + lookDir;
            //Matrix4 matCamera = Matrix_PointAt(camera, target, up);
            //viewMatrix = Matrix_QuickInverse(matCamera);
            //viewMatrix = Matrix4.Identity;
            viewMatrix = camera.GetViewMatrix();

            modelMatrix = Matrix4.Identity;
            //theta += 1f * (float)args.Time;
            modelMatrix = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(45f));
            modelMatrix *= Matrix4.CreateTranslation(new Vector3(0f, -1.5f, -3f));

            GL.UniformMatrix4(modelMatrixLocation, true, ref modelMatrix);
            GL.UniformMatrix4(viewMatrixLocation, true, ref viewMatrix);

            ConvertListToNDC(trisToRaster);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexSize, 0);
            //GL.EnableVertexAttribArray(0);
            GL.EnableVertexArrayAttrib(vao, 0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, 4 * sizeof(float));
            //GL.EnableVertexAttribArray(1);
            GL.EnableVertexArrayAttrib(vao, 1);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertexSize, 7 * sizeof(float));
            //GL.EnableVertexAttribArray(2);
            GL.EnableVertexArrayAttrib(vao, 2);

            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, vertexSize, 9 * sizeof(float));
            //GL.EnableVertexAttribArray(3);
            GL.EnableVertexArrayAttrib(vao, 3);

            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * vertexSize, vertices.ToArray(), BufferUsageHint.DynamicDraw);

            GL.Enable(EnableCap.DepthTest);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            vertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex));

            // OPENGL init
            vao = GL.GenVertexArray();

            // generate a buffer
            GL.GenBuffers(1, out vbo);

            // Texture -----------------------------------------------
            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            // Load the image (using System.Drawing or another library)
            Stream stream = GetResourceStreamByNameEnd("high.png");
            if (stream != null)
            {
                using (stream)
                {
                    Bitmap bitmap = new Bitmap(stream);
                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                    bitmap.UnlockBits(data);

                    // Texture settings
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                }
            }
            else
            {
                ;
            }
            // Texture --------------------------------------------

            // bind the vao
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindVertexArray(vao);
            // point slot (0) of the VAO to the currently bound VBO (vbo)
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexSize, 0);
            //GL.EnableVertexAttribArray(0);
            GL.EnableVertexArrayAttrib(vao, 0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, 4 * sizeof(float));
            //GL.EnableVertexAttribArray(1);
            GL.EnableVertexArrayAttrib(vao, 1);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertexSize, 7 * sizeof(float));
            //GL.EnableVertexAttribArray(2);
            GL.EnableVertexArrayAttrib(vao, 2);

            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, vertexSize, 9 * sizeof(float));
            //GL.EnableVertexAttribArray(3);
            GL.EnableVertexArrayAttrib(vao, 3);

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
            GL.UseProgram(shaderProgram); // bind vao

            int windowSizeLocation = GL.GetUniformLocation(shaderProgram, "windowSize");

            camera = new Camera(new Vector2(screenWidth, screenHeight));

            // Matrixes
            int modelMatrixLocation = GL.GetUniformLocation(shaderProgram, "modelMatrix");
            int viewMatrixLocation = GL.GetUniformLocation(shaderProgram, "viewMatrix");
            int projectionMatrixLocation = GL.GetUniformLocation(shaderProgram, "projectionMatrix");


            modelMatrix = Matrix4.CreateRotationY(theta);
            modelMatrix *= Matrix4.CreateTranslation(new Vector3(0f, 0f, -3f));
            viewMatrix = camera.GetViewMatrix();
            projectionMatrix = camera.GetProjectionMatrix();

            GL.UniformMatrix4(modelMatrixLocation, true, ref modelMatrix);
            GL.UniformMatrix4(viewMatrixLocation, true, ref viewMatrix);
            GL.UniformMatrix4(projectionMatrixLocation, true, ref projectionMatrix);
            GL.Uniform2(windowSizeLocation, new Vector2(screenWidth, screenHeight));

            int textureLocation = GL.GetUniformLocation(shaderProgram, "textureSampler");
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.Uniform1(textureLocation, 0);  // 0 corresponds to TextureUnit.Texture0

            // delete the shaders
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            CursorState = CursorState.Grabbed;

            // Projection matrix and mesh loading
            //meshCube.OnlyCube();
            //meshCube.OnlyTriangle();
            meshCube.ProcessObj("spiro.obj");
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

        private Stream GetResourceStreamByNameEnd(string nameEnd)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (resourceName.EndsWith(nameEnd, StringComparison.OrdinalIgnoreCase))
                {
                    return assembly.GetManifestResourceStream(resourceName);
                }
            }
            return null; // or throw an exception if the resource is not found
        }
    }
}
