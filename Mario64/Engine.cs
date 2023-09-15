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
using System.Security.Cryptography;

#pragma warning disable CS8600
#pragma warning disable CA1416
#pragma warning disable CS8604

namespace Mario64
{
    public struct Vertex
    {
        public Vector4 Position;
        public Vector3 Normal;
        public Vector2 Texture;
        public Vector3 Camera;
    }

    public class Engine : GameWindow
    {
       

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
        private List<Vertex> trisToDraw = new List<Vertex>();

        // Program variables
        private Random rnd = new Random((int)DateTime.Now.Ticks);
        private int screenWidth;
        private int screenHeight;
        private int frameCount;
        private double totalTime;

        // Engine variables
        private List<Mesh> meshes;
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
            trisToDraw = new List<Vertex>();
            meshes = new List<Mesh>();
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


        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.ClearColor(Color4.Cyan);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            DrawFps(args.Time);

            GL.DrawArrays(PrimitiveType.Triangles, 0, trisToDraw.Count);

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
            frustum = camera.GetFrustum();

            GL.UniformMatrix4(viewMatrixLocation, true, ref viewMatrix);

            trisToDraw = new List<Vertex>();
            foreach (Mesh mesh in meshes)
            {
                trisToDraw.AddRange(mesh.UpdateVertexArray(ref frustum, ref camera));
            }
            int vertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex));

            GL.BufferData(BufferTarget.ArrayBuffer, trisToDraw.Count * vertexSize, trisToDraw.ToArray(), BufferUsageHint.DynamicDraw);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.Enable(EnableCap.DepthTest);
            //GL.Disable(EnableCap.CullFace);
            //GL.Enable(EnableCap.Blend);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // OPENGL init
            vao = GL.GenVertexArray();

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
            camera.UpdateVectors();

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

            // delete the shaders
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            CursorState = CursorState.Grabbed;

            // Projection matrix and mesh loading
            //meshCube.OnlyCube();
            //meshCube.OnlyTriangle();
            //meshCube.ProcessObj("spiro.obj");
            meshes.Add(new Mesh(vao, shaderProgram, "spiro.obj", "High.png"));
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

        
    }
}
