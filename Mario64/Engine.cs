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
using static System.Net.WebRequestMethods;

#pragma warning disable CS8600
#pragma warning disable CA1416
#pragma warning disable CS8604

namespace Mario64
{
    public class Plane
    {
        public Plane() { }

        public Plane(Vector3 normal, float dist)
        {
            this.normal = normal.Normalized();
            distance = dist;
        }

        // unit vector
        public Vector3 normal = new Vector3(0.0f, 1.0f, 0.0f);

        // distance from origin to the nearest point in the plane
        public float distance = 0.0f;

        public float DistanceToPoint(Vector3 point)
        {
            return Vector3.Dot(normal, point) + distance;
        }

        public float DistanceToPoint(Vec3d point)
        {
            return Vector3.Dot(normal, new Vector3(point.X, point.Y, point.Z)) + distance;
        }
    };

    public class Frustum
    {
        public Frustum() 
        {
            Left = new Plane();
            Right = new Plane(); 
            Bottom = new Plane();
            Top = new Plane();
            Near = new Plane();
            Far = new Plane();
        }

        public bool IsInside(Vector3 point)
        {
            if (Left.DistanceToPoint(point) < 0) return false;
            if (Right.DistanceToPoint(point) < 0) return false;
            if (Bottom.DistanceToPoint(point) < 0) return false;
            if (Top.DistanceToPoint(point) < 0) return false;
            if (Near.DistanceToPoint(point) < 0) return false;
            if (Far.DistanceToPoint(point) < 0) return false;
            return true;
        }

        public bool IsInside(Vec3d point)
        {
            if (Left.DistanceToPoint(point) < 0) return false;
            if (Right.DistanceToPoint(point) < 0) return false;
            if (Bottom.DistanceToPoint(point) < 0) return false;
            if (Top.DistanceToPoint(point) < 0) return false;
            if (Near.DistanceToPoint(point) < 0) return false;
            if (Far.DistanceToPoint(point) < 0) return false;
            return true;
        }

        public bool IsTriangleInside(triangle tri)
        {
            var a = IsInside(tri.p[0]);
            var b = IsInside(tri.p[1]);
            var c = IsInside(tri.p[2]);
            return a || b || c;
        }

        public List<triangle> GetTriangles()
        { 
            List<triangle> triangles = new List<triangle>();

            Vec3d[] corners = new Vec3d[8];
            corners[0] = new Vec3d(ntl.X, ntl.Y, ntl.Z);
            corners[1] = new Vec3d(ntr.X, ntr.Y, ntr.Z);
            corners[2] = new Vec3d(nbl.X, nbl.Y, nbl.Z);
            corners[3] = new Vec3d(nbr.X, nbr.Y, nbr.Z);
            corners[4] = new Vec3d(ftl.X, ftl.Y, ftl.Z);
            corners[5] = new Vec3d(ftr.X, ftr.Y, ftr.Z);
            corners[6] = new Vec3d(fbl.X, fbl.Y, fbl.Z);
            corners[7] = new Vec3d(fbr.X, fbr.Y, fbr.Z);

            triangles.Add(new triangle( new Vec3d[] { corners[0], corners[1], corners[2] } ));
            triangles.Add(new triangle( new Vec3d[] { corners[1], corners[3], corners[2] } ));
            triangles.Add(new triangle( new Vec3d[] { corners[4], corners[5], corners[6] } ));
            triangles.Add(new triangle( new Vec3d[] { corners[5], corners[7], corners[6] } ));
            triangles.Add(new triangle( new Vec3d[] { corners[0], corners[4], corners[2] } ));
            triangles.Add(new triangle( new Vec3d[] { corners[4], corners[6], corners[2] } ));
            triangles.Add(new triangle( new Vec3d[] { corners[1], corners[5], corners[3] } ));
            triangles.Add(new triangle( new Vec3d[] { corners[5], corners[7], corners[3] } ));
            triangles.Add(new triangle( new Vec3d[] { corners[0], corners[1], corners[4] } ));
            triangles.Add(new triangle( new Vec3d[] { corners[1], corners[5], corners[4] } ));
            triangles.Add(new triangle( new Vec3d[] { corners[2], corners[3], corners[6] } ));
            triangles.Add(new triangle( new Vec3d[] { corners[3], corners[7], corners[6] } ));

            return triangles;
        }

        public Plane Left;
        public Plane Right;
        public Plane Bottom;
        public Plane Top;
        public Plane Near;
        public Plane Far;

        public Vector3 nearCenter;
        public Vector3 farCenter;

        public Vector3 ntl;
        public Vector3 ntr;
        public Vector3 nbl;
        public Vector3 nbr;
        public Vector3 ftl;
        public Vector3 ftr;
        public Vector3 fbl;
        public Vector3 fbr;
    }

    public class Engine : GameWindow
    {
        

        private int vertexSize;
        struct Vertex
        {
            public Vector4 Position;
            public Vector3 Normal;
            public Vector2 Texture;
            public Vector3 Camera;
        }


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
        private Frustum frustum;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;

        public Engine(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            screenWidth = width;
            screenHeight = height;
            this.CenterWindow(new Vector2i(screenWidth, screenHeight));
            vertices = new List<Vertex>();
            frustum = new Frustum();
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

            int viewMatrixLocation = GL.GetUniformLocation(shaderProgram, "viewMatrix");
            int modelMatrixLocation = GL.GetUniformLocation(shaderProgram, "modelMatrix");

            viewMatrix = camera.GetViewMatrix();
            //frustum = camera.GetFrustum();

            trisToRaster = new List<triangle>();
            foreach (triangle tri in meshCube.tris)
            {
                if (frustum.IsTriangleInside(tri))
                    trisToRaster.Add(tri);
            }

            foreach (triangle tri in frustum.GetTriangles())
            {
                tri.Color(Color4.Red);
                trisToRaster.Add(tri);
            }

            modelMatrix = Matrix4.Identity;
            //theta += 1f * (float)args.Time;
            //modelMatrix = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(45f));
            //modelMatrix *= Matrix4.CreateTranslation(new Vector3(0f, -1.5f, -3f));

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
            Stream stream = GetResourceStreamByNameEnd("High.png");
            if (stream != null)
            {
                using (stream)
                {
                    Bitmap bitmap = new Bitmap(stream);
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
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
            frustum = camera.GetFrustum();
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
