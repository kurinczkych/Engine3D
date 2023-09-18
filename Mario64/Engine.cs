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
using System.ComponentModel.Design;
using static System.Net.WebRequestMethods;
using System.Security.Cryptography;

#pragma warning disable CS0649

namespace Mario64
{

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
        private int textVao;
        private int noTextureVao;
        private Shader shaderProgram;
        private Shader textShaderProgram;
        private Shader noTextureShaderProgram;
        private int textureCount = 0;

        // Program variables
        private Random rnd = new Random((int)DateTime.Now.Ticks);
        private int screenWidth;
        private int screenHeight;
        private int frameCount;
        private double totalTime;

        // Engine variables
        private List<Mesh> meshes;
        private List<TextMesh> textMeshes;

        private Camera camera = new Camera();
        private Frustum frustum;
        private List<PointLight> pointLights;
        private TextGenerator textGenerator;

        public Engine(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            screenWidth = width;
            screenHeight = height;
            this.CenterWindow(new Vector2i(screenWidth, screenHeight));
            meshes = new List<Mesh>();
            textMeshes = new List<TextMesh>();
            frustum = new Frustum();
            shaderProgram = new Shader();
            textShaderProgram = new Shader();
            pointLights = new List<PointLight>();
        }

        private double DrawFps(double deltaTime)
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

            return fps;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.ClearColor(Color4.Cyan);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            double fps = DrawFps(args.Time);


            frustum = camera.GetFrustum();

            GL.Enable(EnableCap.DepthTest);
            shaderProgram.Use();
            PointLight.SendToGPU(ref pointLights, shaderProgram.id);

            foreach (Mesh mesh in meshes)
            {
                mesh.UpdateFrustumAndCamera(ref frustum, ref camera);
                mesh.Draw();
            }

            //foreach (NoTextureMesh mesh in PointLight.GetMeshes(ref pointLights, vao, noTextureShaderProgram.id, ref frustum, ref camera))

            noTextureShaderProgram.Use();
            foreach (PointLight pl in pointLights)
            {
                pl.mesh.UpdateFrustumAndCamera(ref frustum, ref camera);
                pl.mesh.Draw();
            }

            // Text rendering
            GL.Disable(EnableCap.DepthTest);


            textMeshes[0] = textGenerator.Generate(textVao, textShaderProgram.id,
                ((int)fps).ToString() + " fps",
                new Vector2(10, screenHeight - 35),
                Color4.White,
                new Vector2(1.5f, 1.5f),
                new Vector2(screenWidth, screenHeight),
                ref textureCount);

            textShaderProgram.Use();
            foreach (TextMesh textMesh in textMeshes)
            {
                textMesh.Draw();
            }

            GL.DisableVertexAttribArray(0);

            Context.SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            camera.Update(KeyboardState, MouseState, args);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            CursorState = CursorState.Grabbed;

            textGenerator = new TextGenerator();

            GL.Enable(EnableCap.DepthTest);
            //GL.Disable(EnableCap.CullFace);


            // OPENGL init
            vao = GL.GenVertexArray();
            textVao = GL.GenVertexArray();
            noTextureVao = GL.GenVertexArray();

            // create the shader program
            shaderProgram = new Shader("Default.vert", "Default.frag");
            textShaderProgram = new Shader("textVert.vert", "textFrag.frag");
            noTextureShaderProgram = new Shader("noTexture.vert", "noTexture.frag");

            //Camera
            camera = new Camera(new Vector2(screenWidth, screenHeight));
            camera.UpdateVectors();
            frustum = camera.GetFrustum();

            //Point Lights
            noTextureShaderProgram.Use();
            pointLights.Add(new PointLight(new Vector3(0, 5, 0), Color4.White, vao, shaderProgram.id, ref frustum, ref camera, noTextureVao, noTextureShaderProgram.id, pointLights.Count));


            shaderProgram.Use();
            PointLight.SendToGPU(ref pointLights, shaderProgram.id);

            // Projection matrix and mesh loading
            //meshCube.OnlyCube();
            //meshCube.OnlyTriangle();
            //meshCube.ProcessObj("spiro.obj");
            meshes.Add(new Mesh(vao, shaderProgram.id, "spiro.obj", "High.png", new Vector2(screenWidth, screenHeight), ref frustum, ref camera, ref textureCount));
            //meshes.Add(new Mesh(vao, shaderProgram.id, "sphere.obj"));
            //meshes.Last().TranslateRotateScale(new Vector3(7, -2.0f, 0), new Vector3(0, 0, 0), Vector3.One);

            textShaderProgram.Use();

            textMeshes.Add(textGenerator.Generate(textVao, textShaderProgram.id,
                "test",
                new Vector2(10, 10),
                Color4.White,
                new Vector2(10, 10),
                new Vector2(screenWidth, screenHeight),
                ref textureCount));


            if (textMeshes.Count > 0)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteVertexArray(vao);
            foreach(Mesh mesh in meshes)
                GL.DeleteBuffer(mesh.vbo);
            foreach(TextMesh mesh in textMeshes)
                GL.DeleteBuffer(mesh.vbo);
            shaderProgram.Unload();
            textShaderProgram.Unload();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            screenWidth = e.Width;
            screenHeight = e.Height;
        }
    }
}
