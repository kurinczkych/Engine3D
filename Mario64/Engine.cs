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

namespace Mario64
{
    public class Engine : GameWindow
    {
        private struct triangle
        {
            public triangle()
            {
                p = new Vector3d[3];
                color = Color4.White;
            }

            public triangle(Vector3d[] p)
            {
                this.p = p;
                color = Color4.White;
            }

            public triangle GetCopy()
            {
                triangle tri = new triangle();
                tri.p[0] = p[0];
                tri.p[1] = p[1];
                tri.p[2] = p[2];
                tri.color = color;

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

            public Vector3d[] p;
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

        // OPENGL
        private int vao;
        private int shaderProgram;
        private int vbo;
        private List<Vertex> vertices;

        // Program variables
        private int screenWidth;
        private int screenHeight;
        private int frameCount;
        private double totalTime;

        // Engine variables
        private mesh meshCube;
        private Matrix4d matProj;
        private Matrix4d matRotZ, matRotX;
        private double theta = 0f;

        private Vector3d camera;

        public Engine(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            screenWidth = width;
            screenHeight = height;
            this.CenterWindow(new Vector2i(screenWidth, screenHeight));
            vertices = new List<Vertex>();
        }
        private void Swap(ref double x, ref double y)
        {
            double t = x;
            x = y;
            y = t;
        }

        private void Swap(ref int x, ref int y)
        {
            int t = x;
            x = y;
            y = t;
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

        float zz = -10f;

        private void DrawPixel(double x, double y, Color4 color, bool scissorTest = true)
        {
            if (scissorTest)
                GL.Enable(EnableCap.ScissorTest);

            GL.Scissor((int)x, (int)y, 1, 1);
            GL.ClearColor(color);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            if(scissorTest)
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

        private void DrawTriangle(triangle tri)
        {
            int x1 = (int)tri.p[0].X;
            int y1 = (int)tri.p[0].Y;
            int x2 = (int)tri.p[1].X;
            int y2 = (int)tri.p[1].Y;
            int x3 = (int)tri.p[2].X;
            int y3 = (int)tri.p[2].Y;

            DrawLine(x1, y1, x2, y2, tri.color);
            DrawLine(x2, y2, x3, y3, tri.color);
            DrawLine(x3, y3, x1, y1, tri.color);
        }

        //private void FillTriangle(int x1, int y1, int x2, int y2, int x3, int y3)
        private void FillTriangle(triangle tri)
        {
            int x1 = (int)tri.p[0].X;
            int y1 = (int)tri.p[0].Y;
            int x2 = (int)tri.p[1].X;
            int y2 = (int)tri.p[1].Y;
            int x3 = (int)tri.p[2].X;
            int y3 = (int)tri.p[2].Y;
            //auto SWAP = [](int & x, int & y) { int t = x; x = y; y = t; };
            //auto drawline = [&](int sx, int ex, int ny) { for (int i = sx; i <= ex; i++) Draw(i, ny, c, col); };

            int t1x, t2x, y, minx, maxx, t1xp, t2xp;
            bool changed1 = false;
            bool changed2 = false;
            int signx1, signx2, dx1, dy1, dx2, dy2;
            int e1, e2;
            // Sort vertices
            if (y1 > y2) { Swap(ref y1, ref y2); Swap(ref x1, ref x2); }
            if (y1 > y3) { Swap(ref y1, ref y3); Swap(ref x1, ref x3); }
            if (y2 > y3) { Swap(ref y2, ref y3); Swap(ref x2, ref x3); }

            t1x = t2x = x1; y = y1;   // Starting points
            dx1 = (int)(x2 - x1); if (dx1 < 0) { dx1 = -dx1; signx1 = -1; }
            else signx1 = 1;
            dy1 = (int)(y2 - y1);

            dx2 = (int)(x3 - x1); if (dx2 < 0) { dx2 = -dx2; signx2 = -1; }
            else signx2 = 1;
            dy2 = (int)(y3 - y1);

            if (dy1 > dx1)
            {   // swap values
                Swap(ref dx1, ref dy1);
                changed1 = true;
            }
            if (dy2 > dx2)
            {   // swap values
                Swap(ref dy2, ref dx2);
                changed2 = true;
            }

            e2 = (int)(dx2 >> 1);
            // Flat top, just process the second half
            if (y1 == y2) goto next;
            e1 = (int)(dx1 >> 1);

            for (int i = 0; i < dx1;)
            {
                t1xp = 0; t2xp = 0;
                if (t1x < t2x) { minx = t1x; maxx = t2x; }
                else { minx = t2x; maxx = t1x; }
                // process first line until y value is about to change
                while (i < dx1)
                {
                    i++;
                    e1 += dy1;
                    while (e1 >= dx1)
                    {
                        e1 -= dx1;
                        if (changed1) t1xp = signx1;//t1x += signx1;
                        else goto next1;
                    }
                    if (changed1) break;
                    else t1x += signx1;
                }
                // Move line
                next1:
                // process second line until y value is about to change
                while (true)
                {
                    e2 += dy2;
                    while (e2 >= dx2)
                    {
                        e2 -= dx2;
                        if (changed2) t2xp = signx2;//t2x += signx2;
                        else goto next2;
                    }
                    if (changed2) break;
                    else t2x += signx2;
                }

                next2:
                if (minx > t1x) minx = t1x; if (minx > t2x) minx = t2x;
                if (maxx < t1x) maxx = t1x; if (maxx < t2x) maxx = t2x;

                // Draw line from min to max points found on the y
                for (int i2 = minx; i2 <= maxx; i2++)
                    DrawPixel(i2, y, tri.color);

                // Now increase y
                if (!changed1) t1x += signx1;
                t1x += t1xp;
                if (!changed2) t2x += signx2;
                t2x += t2xp;
                y += 1;
                if (y == y2) break;

            }

            next:
            // Second half
            dx1 = (int)(x3 - x2); if (dx1 < 0) { dx1 = -dx1; signx1 = -1; }
            else signx1 = 1;
            dy1 = (int)(y3 - y2);
            t1x = x2;

            if (dy1 > dx1)
            {   // swap values
                Swap(ref dy1, ref dx1);
                changed1 = true;
            }
            else changed1 = false;

            e1 = (int)(dx1 >> 1);

            for (int i = 0; i <= dx1; i++)
            {
                t1xp = 0; t2xp = 0;
                if (t1x < t2x) { minx = t1x; maxx = t2x; }
                else { minx = t2x; maxx = t1x; }
                // process first line until y value is about to change
                while (i < dx1)
                {
                    e1 += dy1;
                    while (e1 >= dx1)
                    {
                        e1 -= dx1;
                        if (changed1) { t1xp = signx1; break; }//t1x += signx1;
                        else goto next3;
                    }
                    if (changed1) break;
                    else t1x += signx1;
                    if (i < dx1) i++;
                }
                next3:
                // process second line until y value is about to change
                while (t2x != x3)
                {
                    e2 += dy2;
                    while (e2 >= dx2)
                    {
                        e2 -= dx2;
                        if (changed2) t2xp = signx2;
                        else goto next4;
                    }
                    if (changed2) break;
                    else t2x += signx2;
                }
                next4:

                if (minx > t1x) minx = t1x; if (minx > t2x) minx = t2x;
                if (maxx < t1x) maxx = t1x; if (maxx < t2x) maxx = t2x;

                for (int i2 = minx; i2 <= maxx; i2++)
                    DrawPixel(i2, y, tri.color);

                if (!changed1) t1x += signx1;
                t1x += t1xp;
                if (!changed2) t2x += signx2;
                t2x += t2xp;
                y += 1;
                if (y > y3) return;
            }
        }

        private List<triangle> CalculateTriangles()
        {
            // Rotation Z
            matRotZ.Row0[0] = Math.Cos(theta);
            matRotZ.Row0[1] = Math.Sin(theta);
            matRotZ.Row1[0] = -Math.Sin(theta);
            matRotZ.Row1[1] = Math.Cos(theta);
            matRotZ.Row2[2] = 1;
            matRotZ.Row3[3] = 1;

            // Rotation X
            matRotX.Row0[0] = 1;
            matRotX.Row1[1] = Math.Cos(theta * 0.5f);
            matRotX.Row1[2] = Math.Sin(theta * 0.5f);
            matRotX.Row2[1] = -Math.Sin(theta * 0.5f);
            matRotX.Row2[2] = Math.Cos(theta * 0.5f);
            matRotX.Row3[3] = 1;

            List<triangle> trisToRaster = new List<triangle>();

            //DrawTriangle(100, 100, 2)
            foreach (triangle tri in meshCube.tris)
            {
                triangle triRotatedZ = new triangle();
                // Rotate in Z-Axis
                triRotatedZ.p[0] = MultiplyMatrixVector(tri.p[0], matRotZ);
                triRotatedZ.p[1] = MultiplyMatrixVector(tri.p[1], matRotZ);
                triRotatedZ.p[2] = MultiplyMatrixVector(tri.p[2], matRotZ);

                triangle triRotatedZX = new triangle();
                // Rotate in X-Axis
                triRotatedZX.p[0] = MultiplyMatrixVector(triRotatedZ.p[0], matRotX);
                triRotatedZX.p[1] = MultiplyMatrixVector(triRotatedZ.p[1], matRotX);
                triRotatedZX.p[2] = MultiplyMatrixVector(triRotatedZ.p[2], matRotX);

                triangle triTranslated = new triangle();
                // Offset to screen
                triTranslated = triRotatedZX.GetCopy();
                triTranslated.p[0].Z = triRotatedZX.p[0].Z + 8.0f;
                triTranslated.p[1].Z = triRotatedZX.p[1].Z + 8.0f;
                triTranslated.p[2].Z = triRotatedZX.p[2].Z + 8.0f;

                // Normal calculation
                Vector3d normal, line1, line2;
                line1.X = triTranslated.p[1].X - triTranslated.p[0].X;
                line1.Y = triTranslated.p[1].Y - triTranslated.p[0].Y;
                line1.Z = triTranslated.p[1].Z - triTranslated.p[0].Z;

                line2.X = triTranslated.p[2].X - triTranslated.p[0].X;
                line2.Y = triTranslated.p[2].Y - triTranslated.p[0].Y;
                line2.Z = triTranslated.p[2].Z - triTranslated.p[0].Z;

                normal = Vector3d.Cross(line1, line2);
                normal.Normalize();

                // If not visible, continue
                if (Vector3d.Dot(normal, (triTranslated.p[0] - camera)) >= 0)
                    continue;

                Vector3d lightDir = new Vector3d(0.0f, 0.0f, -1.0f);
                lightDir.Normalize();

                double dp = Vector3d.Dot(normal, lightDir);

                triangle triProjected = new triangle();
                triProjected.color = new Color4((float)dp, (float)dp, (float)dp, 1.0f);

                // Project triangles from 3D to 2D
                triProjected.p[0] = MultiplyMatrixVector(triTranslated.p[0], matProj);
                triProjected.p[1] = MultiplyMatrixVector(triTranslated.p[1], matProj);
                triProjected.p[2] = MultiplyMatrixVector(triTranslated.p[2], matProj);

                // Scale into view
                triProjected.p[0] += new Vector3d(1.0f, 1.0f, 1.0f);
                triProjected.p[1] += new Vector3d(1.0f, 1.0f, 1.0f);
                triProjected.p[2] += new Vector3d(1.0f, 1.0f, 1.0f);

                triProjected.p[0].X *= 0.5f * (double)screenWidth;
                triProjected.p[0].Y *= 0.5f * (double)screenHeight;
                triProjected.p[1].X *= 0.5f * (double)screenWidth;
                triProjected.p[1].Y *= 0.5f * (double)screenHeight;
                triProjected.p[2].X *= 0.5f * (double)screenWidth;
                triProjected.p[2].Y *= 0.5f * (double)screenHeight;

                trisToRaster.Add(triProjected);
            }

            // Sort triangles back to front
            trisToRaster.Sort((a, b) => a.CompareTo(b));

            return trisToRaster;
        }

        public double ConvertRange( double originalStart, double originalEnd, double newStart, double newEnd, double value) 
        {
            double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (double)(newStart + ((value - originalStart) * scale)); 
        }

        Vertex ConvertToNDC(Vector3d screenPos, Color4 color)
        {
            float x = (float)ConvertRange(0, screenWidth, -1, 1, screenPos[0]);
            float y = (float)ConvertRange(0, screenHeight, -1, 1, screenPos[1]);

            return new Vertex() { Position = new Vector3(x, y, 1.0f), Color = color };
        }

        void ConvertListToNDC(List<triangle> tris)
        {
            vertices = new List<Vertex>();
            foreach (var tri in tris)
            {
                vertices.Add(ConvertToNDC(tri.p[0], tri.color));
                vertices.Add(ConvertToNDC(tri.p[1], tri.color));
                vertices.Add(ConvertToNDC(tri.p[2], tri.color));
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

            Context.SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            theta += 1.0f * args.Time;

            List<triangle> tris = CalculateTriangles();
            ConvertListToNDC(tris);

            
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
            meshCube.ProcessObj("plane.obj");

            //Projection matrix
            double near = 0.1f;
            double far = 1000.0f;
            double fov = 90.0f;
            double aspectRatio = (double)screenHeight / (double)screenWidth;
            double fovRad = 1.0f / Math.Tan(fov * 0.5f / 180.0f * Math.PI);

            matProj = new Matrix4d();

            matProj.Row0[0] = aspectRatio * fovRad;
            matProj.Row1[1] = fovRad;
            matProj.Row2[2] = far / (far - near);
            matProj.Row3[2] = (-far * near) / (far - near);
            matProj.Row2[3] = 1.0f;
            matProj.Row3[3] = 0.0f;

        }

        private Vector3d MultiplyMatrixVector(Vector3d i, Matrix4d m)
        {
            Vector3d o = new Vector3d();

            o.X = i.X * m.Row0[0] + i.Y * m.Row1[0] + i.Z * m.Row2[0] + m.Row3[0];
            o.Y = i.X * m.Row0[1] + i.Y * m.Row1[1] + i.Z * m.Row2[1] + m.Row3[1];
            o.Z = i.X * m.Row0[2] + i.Y * m.Row1[2] + i.Z * m.Row2[2] + m.Row3[2];
            double w = i.X * m.Row0[3] + i.Y * m.Row1[3] + i.Z * m.Row2[3] + m.Row3[3];

            if (w != 0.0f)
            {
                o.X /= w; o.Y /= w; o.Z /= w;
            }

            return o;
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
