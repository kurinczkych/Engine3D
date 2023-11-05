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
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System.Xml.Schema;
using OpenTK.Audio.OpenAL;
using System.Drawing;
using System.Linq.Expressions;
using MagicPhysX;
using ImGuiNET;
using OpenTK.Windowing.Common.Input;
using System.ComponentModel.DataAnnotations;

#pragma warning disable CS0649
#pragma warning disable CS8618

namespace Engine3D
{
    public class Engine : GameWindow
    {
        public class SavedStart
        {
            public List<Object> objects;
            public Dictionary<string, TextMesh> texts;
            Character character;
        }

        public class GameWindowProperty
        {
            public float topPanelSize = 50;
            public float bottomPanelSize = 25;
            public float bottomPanelPercent = 0.25f;
            public float leftPanelPercent = 0.15f;
            public float rightPanelPercent = 0.20f;
            public Vector2 gameWindowPos;
            public Vector2 gameWindowSize;

            public GameWindowProperty() { }
        }

        #region OPENGL
        private IndirectBuffer indirectBuffer;
        private VBO visibilityVbo;
        private VBO drawCommandsVbo;
        private VBO frustumVbo;

        private VAO outlineVao;
        private VBO outlineVbo;

        private VAO meshVao;
        private VBO meshVbo;

        private InstancedVAO instancedMeshVao;
        private VBO instancedMeshVbo;

        private VAO textVao;
        private VBO textVbo;

        private VAO noTexVao;
        private VBO noTexVbo;

        private VAO uiTexVao;
        private VBO uiTexVbo;

        private VAO wireVao;
        private VBO wireVbo;

        private VAO aabbVao;
        private VBO aabbVbo;

        private Shader cullingProgram;
        private Shader outlineShader;
        private Shader shaderProgram;
        private Shader instancedShaderProgram;
        private Shader posTexShader;
        private Shader noTextureShaderProgram;
        private Shader aabbShaderProgram;
        #endregion

        #region Program variables
        public static Random rnd = new Random((int)DateTime.Now.Ticks);
        private bool haveText = false;
        private bool firstRun = true;

        private Vector2 origWindowSize;
        private Vector2 windowSize;
        private GameWindowProperty gameWindowProperty;
        private Vector2 gameWindowMousePos;

        private SoundManager soundManager;
        public static TextureManager textureManager;

        private TextGenerator textGenerator;
        private Dictionary<string, TextMesh> texts;
        #endregion

        #region Editor moving
        private Vector2 lastPos;
        private bool firstMove = true;
        private float sensitivity = 130;
        #endregion

        #region UI variables
        private EditorData editorData = new EditorData();
        private EditorProperties editorProperties = new EditorProperties();
        private ImGuiController imGuiController;
        #endregion

        #region Engine variables
        private List<Object> objects;
        private Character character;

        private List<PointLight> pointLights;
        private List<ParticleSystem> particleSystems;
        private Physx physx;

        private bool useOcclusionCulling = false;
        private QueryPool queryPool;
        private Dictionary<int, Tuple<int, BVHNode>> pendingQueries;
        #endregion

        public Engine(int width, int height) : base(GameWindowSettings.Default, new NativeWindowSettings() { StencilBits = 8 })
        {
            Title = "3D Engine";

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            gameWindowProperty = new GameWindowProperty();
            gameWindowProperty.gameWindowSize = new Vector2();
            gameWindowProperty.gameWindowPos = new Vector2();

            windowSize = new Vector2(width, height);
            origWindowSize = new Vector2(width, height);
            CenterWindow(new Vector2i(width, height));

            shaderProgram = new Shader();
            posTexShader = new Shader();
            pointLights = new List<PointLight>();
            particleSystems = new List<ParticleSystem>();
            texts = new Dictionary<string, TextMesh>();
            textureManager = new TextureManager();

            objects = new List<Object>();
            queryPool = new QueryPool(1000);
            pendingQueries = new Dictionary<int, Tuple<int, BVHNode>>();
        }

        private void AddObject(Object obj)
        {
            if (obj.GetObjectType() == ObjectType.TextMesh)
                haveText = true;
            objects.Add(obj);
            objects.Sort();
        }

        private void AddText(Object obj, string tag)
        {
            if (obj.GetObjectType() != ObjectType.TextMesh && obj.GetMesh().GetType() != typeof(TextMesh))
                throw new Exception("Can only add Text, if its ObjectType.TextMesh");

            haveText = true;
            objects.Add(obj);
            texts.Add(tag, (TextMesh)obj.GetMesh());
            objects.Sort();
        }


        private bool DrawCorrectMesh(ref List<float> vertices, Type prevMeshType, Type currentMeshType, BaseMesh? prevMesh)
        {
            if (prevMeshType == null || currentMeshType == null)
                return false;

            if(prevMeshType == typeof(Mesh) && prevMeshType != currentMeshType)
            {
                meshVbo.Buffer(vertices);
            }
            if(prevMeshType == typeof(InstancedMesh) && prevMeshType != currentMeshType)
            {
                instancedMeshVbo.Buffer(vertices);
            }
            else if(prevMeshType == typeof(NoTextureMesh) && prevMeshType != currentMeshType)
            {
                noTexVbo.Buffer(vertices);
            }
            else if(prevMeshType == typeof(WireframeMesh) && prevMeshType != currentMeshType)
            {
                wireVbo.Buffer(vertices);
            }
            else if(prevMeshType == typeof(UITextureMesh) && prevMeshType != currentMeshType)
            {
                uiTexVbo.Buffer(vertices);
            }
            else if(prevMeshType == typeof(TextMesh) && prevMeshType != currentMeshType)
            {
                textVbo.Buffer(vertices);
            }
            else
                return false;

            if (prevMeshType == typeof(WireframeMesh))
                GL.DrawArrays(PrimitiveType.Lines, 0, vertices.Count);
            else if (prevMeshType == typeof(InstancedMesh))
            {
                if(prevMesh != null)
                    GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, vertices.Count, ((InstancedMesh)prevMesh).instancedData.Count());
            }
            else
                GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
            return true;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            imGuiController.Update(this, (float)args.Time);

            if (!editorProperties.isGameFullscreen)
            {
                GL.Viewport((int)gameWindowProperty.gameWindowPos.X, (int)gameWindowProperty.gameWindowPos.Y,
                            (int)gameWindowProperty.gameWindowSize.X, (int)gameWindowProperty.gameWindowSize.Y);
                GL.Enable(EnableCap.ScissorTest);
                GL.Scissor((int)gameWindowProperty.gameWindowPos.X, (int)gameWindowProperty.gameWindowPos.Y,
                           (int)gameWindowProperty.gameWindowSize.X, (int)gameWindowProperty.gameWindowSize.Y);
            }

            #region GameWindow

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Enable(EnableCap.DepthTest);

            // Triangle frustum visibility calculation
            if (editorProperties.gameRunning == GameState.Running || firstRun)
            {
                foreach (Object obj in objects)
                {
                    obj.GetMesh().CalculateFrustumVisibility();
                }
                firstRun = false;
            }

            if (editorProperties.gameRunning == GameState.Running)
            {
                if (useOcclusionCulling)
                {
                    //Occlusion
                    GL.ColorMask(false, false, false, false);  // Disable writing to the color buffer
                    GL.Clear(ClearBufferMask.DepthBufferBit);
                    GL.ClearDepth(1.0);

                    List<Object> triangleMeshObjects = objects.Where(x => x.GetObjectType() == ObjectType.TriangleMesh).ToList();
                    aabbShaderProgram.Use();
                    List<float> posVertices = new List<float>();
                    foreach (Object obj in triangleMeshObjects)
                    {
                        //posVertices.AddRange(((Mesh)obj.GetMesh()).DrawOnlyPos(aabbVao, aabbShaderProgram));
                    }
                    aabbVbo.Buffer(posVertices);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, posVertices.Count);


                    //GL.ColorMask(true, true, true, true);
                    //GL.ClearColor(Color4.Cyan);
                    //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    GL.Disable(EnableCap.CullFace);
                    if (pendingQueries.Count() == 0)
                    {
                        foreach (Object obj in triangleMeshObjects)
                        {
                            //OcclusionCulling.PerformOcclusionQueriesForBVH(obj.BVHStruct, aabbVbo, aabbVao, aabbShaderProgram, character.camera, ref queryPool, ref pendingQueries, true);
                        }
                    }
                    else
                    {
                        ;
                        foreach (Object obj in triangleMeshObjects)
                        {
                            //OcclusionCulling.PerformOcclusionQueriesForBVH(obj.BVHStruct, aabbVbo, aabbVao, aabbShaderProgram, character.camera, ref queryPool, ref pendingQueries, false);
                        }
                    }
                    GL.Enable(EnableCap.CullFace);


                    GL.ColorMask(true, true, true, true);
                }
            }

            //------------------------------------------------------------

            //character.camera.SetPosition(character.camera.GetPosition() +
            //    new Vector3(-(float)Math.Cos(MathHelper.DegreesToRadians(character.camera.GetYaw())) * 8, 10, -(float)Math.Sin(MathHelper.DegreesToRadians(character.camera.GetYaw()))) * 8);
            //character.camera.SetPosition(character.camera.GetPosition() +
            //    new Vector3(0, 100, 0));


            GL.ClearColor(Color4.Cyan);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            shaderProgram.Use();
            //GL.Enable(EnableCap.StencilTest);
            //GL.StencilMask(0xFF);
            //GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            //GL.StencilFunc(StencilFunction.Always, 1, 0xFF);

            PointLight.SendToGPU(ref pointLights, shaderProgram.id, editorProperties.gameRunning);

            Type? currentMeshType = null;
            //BaseMesh? currentMesh = null;
            List<float> vertices = new List<float>();
            foreach (Object o in objects)
            {
                ObjectType objectType = o.GetObjectType();
                if (objectType == ObjectType.Cube ||
                   objectType == ObjectType.Sphere ||
                   objectType == ObjectType.Capsule ||
                   objectType == ObjectType.TriangleMesh ||
                   objectType == ObjectType.TriangleMeshWithCollider)
                {
                    if (o.GetMesh().GetType() == typeof(Mesh))
                    {
                        Mesh mesh = (Mesh)o.GetMesh();
                        if (currentMeshType == null || currentMeshType != mesh.GetType())
                        {
                            shaderProgram.Use();
                        }

                        if (editorProperties.gameRunning == GameState.Running && useOcclusionCulling && objectType == ObjectType.TriangleMesh)
                        {
                            //List<triangle> notOccludedTris = new List<triangle>();
                            //OcclusionCulling.TraverseBVHNode(o.BVHStruct.Root, ref notOccludedTris, ref frustum);

                            //vertices.AddRange(mesh.DrawNotOccluded(notOccludedTris));
                            //currentMeshType = typeof(Mesh);

                            //if (vertices.Count > 0)
                            //{
                            //    meshVbo.Buffer(vertices);
                            //    GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                            //    vertices.Clear();
                            //}
                        }
                        else
                        {
                            vertices.AddRange(mesh.Draw(editorProperties.gameRunning));
                            currentMeshType = typeof(Mesh);

                            //cullingProgram.Use();

                            //visibilityVbo.Buffer(vertices);
                            //frustumVbo.Buffer(character.camera.frustum.GetData());
                            //GL.DispatchCompute(256, 1, 1);
                            //GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit); 

                            //GL.DrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero);

                            if(o.isSelected && mesh.verticesOnlyPos.Count > 0)
                            {
                                GL.DepthMask(false);
                                outlineShader.Use();
                                outlineVao.Bind();
                                mesh.SendUniformsOnlyPos(outlineShader);
                                outlineVbo.Buffer(mesh.verticesOnlyPos);
                                GL.DrawArrays(PrimitiveType.Triangles, 0, mesh.verticesOnlyPos.Count);
                                shaderProgram.Use();
                                GL.DepthMask(true);
                            }
                            meshVao.Bind();
                            meshVbo.Buffer(vertices);
                            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                            vertices.Clear();
                        }
                    }
                    else if (o.GetMesh().GetType() == typeof(InstancedMesh))
                    {
                        InstancedMesh mesh = (InstancedMesh)o.GetMesh();
                        if (currentMeshType == null || currentMeshType != mesh.GetType())
                        {
                            instancedShaderProgram.Use();
                        }

                        List<float> instancedVertices = new List<float>();
                        (vertices, instancedVertices) = mesh.Draw(editorProperties.gameRunning);
                        currentMeshType = typeof(InstancedMesh);

                        meshVbo.Buffer(vertices);
                        instancedMeshVbo.Buffer(instancedVertices);
                        GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, vertices.Count, mesh.instancedData.Count());
                        vertices.Clear();
                    }
                }
                else if (objectType == ObjectType.NoTexture)
                {
                    NoTextureMesh mesh = (NoTextureMesh)o.GetMesh();

                    if (DrawCorrectMesh(ref vertices, currentMeshType == null ? typeof(NoTextureMesh) : currentMeshType, typeof(NoTextureMesh), null))
                        vertices = new List<float>();

                    if (currentMeshType == null || currentMeshType != mesh.GetType())
                    {
                        noTextureShaderProgram.Use();
                    }

                    vertices.AddRange(mesh.Draw(editorProperties.gameRunning));
                    currentMeshType = typeof(NoTextureMesh);
                }
                else if (objectType == ObjectType.Wireframe)
                {
                    WireframeMesh mesh = (WireframeMesh)o.GetMesh();

                    if (DrawCorrectMesh(ref vertices, currentMeshType == null ? typeof(WireframeMesh) : currentMeshType, typeof(WireframeMesh), null))
                        vertices = new List<float>();

                    if (currentMeshType == null || currentMeshType != mesh.GetType())
                    {
                        noTextureShaderProgram.Use();
                    }

                    vertices.AddRange(mesh.Draw(editorProperties.gameRunning));
                    currentMeshType = typeof(WireframeMesh);
                }
                else if (objectType == ObjectType.UIMesh)
                {
                    UITextureMesh mesh = (UITextureMesh)o.GetMesh();

                    //if (DrawCorrectMesh(ref vertices, currentMesh, typeof(UITextureMesh)))
                    //    vertices = new List<float>();

                    if (currentMeshType == null || currentMeshType != mesh.GetType())
                    {
                        GL.Disable(EnableCap.DepthTest);
                        posTexShader.Use();
                    }

                    vertices.AddRange(mesh.Draw(editorProperties.gameRunning));
                    currentMeshType = typeof(UITextureMesh);

                    uiTexVbo.Buffer(vertices);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                    vertices.Clear();
                }
                else if (objectType == ObjectType.TextMesh)
                {
                    TextMesh mesh = (TextMesh)o.GetMesh();

                    if (currentMeshType == null || currentMeshType != mesh.GetType())
                    {
                        GL.Disable(EnableCap.DepthTest);
                        posTexShader.Use();
                    }

                    if (DrawCorrectMesh(ref vertices, currentMeshType == null ? typeof(TextMesh) : currentMeshType, typeof(TextMesh), null))
                        vertices = new List<float>();

                    vertices.AddRange(mesh.Draw(editorProperties.gameRunning));
                    currentMeshType = typeof(TextMesh);
                }
            }


            if (DrawCorrectMesh(ref vertices, currentMeshType == null ? typeof(int) : currentMeshType, typeof(int), null))
                vertices = new List<float>();

            //GL.BlendFunc(BlendingFactor.SrcColor, BlendingFactor.OneMinusSrcColor);
            foreach (ParticleSystem ps in particleSystems)
            {
                Object psO = ps.GetObject();
                psO.GetMesh().CalculateFrustumVisibility();

                InstancedMesh mesh = (InstancedMesh)psO.GetMesh();
                if (currentMeshType == null || currentMeshType != mesh.GetType())
                {
                    instancedShaderProgram.Use();
                }
                mesh.UpdateFrustumAndCamera(ref character.camera);

                List<float> instancedVertices = new List<float>();
                (vertices, instancedVertices) = mesh.Draw(editorProperties.gameRunning);
                currentMeshType = typeof(InstancedMesh);

                meshVbo.Buffer(vertices);
                instancedMeshVbo.Buffer(instancedVertices);
                GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, vertices.Count, mesh.instancedData.Count());
                vertices.Clear();
            }
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            if(editorProperties.gameRunning == GameState.Stopped)
            {
                //if(editorData.selectedItem != null && editorData.selectedItem is Object o && o.meshType == typeof(Mesh))
                //{
                //    int outlineLoc = GL.GetUniformLocation(outlineShader.id, "useScaleFactor");

                //    GL.ColorMask(false, false, false, false);
                //    GL.DepthMask(false);

                //    GL.StencilMask(0x00);
                //    GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);

                //    outlineShader.Use();
                //    GL.Uniform1(outlineLoc, 1);
                //    Mesh mesh = (Mesh)o.GetMesh();
                //    List<float> outlineVerts = mesh.DrawOnlyPos(editorProperties.gameRunning, outlineVao, outlineShader);
                //    outlineVbo.Buffer(outlineVerts);
                //    GL.DrawArrays(PrimitiveType.Triangles, 0, outlineVerts.Count);

                //    GL.ColorMask(true, true, true, true);
                //    GL.DepthMask(true);
                //}
            }

            //BVH bvh = objects.Where(x => x.GetObjectType() == ObjectType.TriangleMesh).First().BVHStruct;
            //List<WireframeMesh> bvhs = bvh.ExtractWireframesWithPos(bvh.Root, wireVao, wireVbo, noTextureShaderProgram.id, ref character.camera, character.Position);

            //noTextureShaderProgram.Use();
            //vertices.Clear();
            //WireframeMesh wfm = objects.Where(x => x.GetObjectType() == ObjectType.TriangleMesh).First().GridStructure.
            //    ExtractWireframeRange(wireVao, wireVbo, noTextureShaderProgram.id, ref character.camera);

            //wfm.UpdateFrustumAndCamera(ref frustum, ref character.camera);
            //vertices.AddRange(wfm.Draw());

            //wireVbo.Buffer(vertices);
            //GL.DrawArrays(PrimitiveType.Lines, 0, vertices.Count);

            //Drawing character wireframe
            //WireframeMesh characterWiremesh = character.mesh;
            //noTextureShaderProgram.Use();
            //characterWiremesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
            //vertices.AddRange(characterWiremesh.Draw());
            //wireVbo.Buffer(vertices);
            //GL.DrawArrays(PrimitiveType.Lines, 0, vertices.Count);
            //vertices = new List<float>();

            //texts["Position"].ChangeText("Position = (" + character.PStr + ")");
            //texts["Velocity"].ChangeText("Velocity = (" + character.VStr + ")");
            //texts["Looking"].ChangeText("Looking = (" + character.LStr + ")");
            //texts["Noclip"].ChangeText("Noclip = (" + character.noClip.ToString() + ")");

            #endregion

            if (!editorProperties.isGameFullscreen)
            {
                GL.Disable(EnableCap.ScissorTest);
                GL.Viewport(0, 0, (int)windowSize.X, (int)windowSize.Y);
            }

            textureManager.GetAssetTextureIfNeeded(ref editorData);
            if (!editorProperties.isGameFullscreen)
                imGuiController.EditorWindow(gameWindowProperty, ref editorData, KeyboardState);
            else
                imGuiController.FullscreenWindow(gameWindowProperty, ref editorData);

            if (editorProperties.windowResized)
                ResizedEditorWindow();
            if (Cursor != editorProperties.mouseType)
                Cursor = editorProperties.mouseType;
            imGuiController.Render();

            Context.SwapBuffers();

            base.OnRenderFrame(args);

            //if (limitFps)
            //{
            //    double elapsed = stopwatch.Elapsed.TotalSeconds;
            //    if (elapsed < TargetDeltaTime)
            //    {
            //        double sleepTime = TargetDeltaTime - elapsed;
            //        Thread.Sleep((int)(sleepTime * 1000));
            //    }
            //    stopwatch.Restart();
            //}
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            editorData.fps.Update((float)args.Time);

            if (editorProperties.gameRunning == GameState.Stopped &&
               editorProperties.justSetGameState)
            {
                editorProperties.manualCursor = false;

                editorProperties.justSetGameState = false;
            }

            if (editorProperties.gameRunning == GameState.Running && CursorState != CursorState.Grabbed && !editorProperties.manualCursor)
            {
                CursorState = CursorState.Grabbed;
            }
            else if (editorProperties.gameRunning == GameState.Stopped && CursorState != CursorState.Normal)
            {
                CursorState = CursorState.Normal;
            }

            if (KeyboardState.IsKeyReleased(Keys.F5))
            {
                editorProperties.gameRunning = editorProperties.gameRunning == GameState.Stopped ? GameState.Running : GameState.Stopped;
            }

            if (KeyboardState.IsKeyReleased(Keys.F2))
            {
                if (CursorState == CursorState.Normal)
                {
                    CursorState = CursorState.Grabbed;
                    editorProperties.manualCursor = false;
                }
                else if (CursorState == CursorState.Grabbed)
                {
                    CursorState = CursorState.Normal;
                    editorProperties.manualCursor = true;
                }
            }

            float deltaX = 0, deltaY = 0;
            if (firstMove)
            {
                lastPos = new Vector2(MouseState.X, MouseState.Y);
                firstMove = false;
            }
            else
            {
                deltaX = MouseState.X - lastPos.X;
                deltaY = MouseState.Y - lastPos.Y;
                if (deltaX != 0 || deltaY != 0)
                {
                    lastPos = new Vector2(MouseState.X, MouseState.Y);
                }
            }
            if (editorProperties.gameRunning == GameState.Running)
            {
                character.CalculateVelocity(KeyboardState, MouseState, args);
                character.UpdatePosition(KeyboardState, MouseState, args);

                foreach (ParticleSystem ps in particleSystems)
                {
                    ps.Update((float)args.Time);
                }

                soundManager.SetListener(character.camera.GetPosition());
            }
            else
            {
                bool moved = false;
                if (Math.Abs(MouseState.ScrollDelta.Y) > 0)
                {
                    character.Position += character.camera.front * MouseState.ScrollDelta.Y * 2;
                    character.camera.SetPosition(character.Position);

                    moved = true;
                }
                if(MouseState.IsButtonDown(MouseButton.Right) && !MouseState.IsButtonDown(MouseButton.Middle))
                {
                    if (deltaX != 0 || deltaY != 0)
                    {
                        lastPos = new Vector2(MouseState.X, MouseState.Y);

                        character.camera.SetYaw(character.camera.GetYaw() + deltaX * sensitivity * (float)args.Time);
                        character.camera.SetPitch(character.camera.GetPitch() - deltaY * sensitivity * (float)args.Time);
                        moved = true;
                    }
                }
                else if(MouseState.IsButtonDown(MouseButton.Middle))
                {
                    if (deltaX != 0 || deltaY != 0)
                    {
                        lastPos = new Vector2(MouseState.X, MouseState.Y);

                        character.Position += (character.camera.up * deltaY) - (character.camera.right * deltaX);
                        character.camera.SetPosition(character.Position);
                        moved = true;
                    }
                }

                if(moved)
                {
                    ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = BaseMesh.threadSize };
                    Parallel.ForEach(objects, parallelOptions, obj =>
                    {
                        obj.GetMesh().recalculate = true;
                    });
                }
            }


            character.AfterUpdate(MouseState, args, editorProperties.gameRunning);

            if (editorData.fps.totalTime > 0)
            {
                physx.Simulate((float)args.Time);
                foreach (Object o in objects)
                {
                    o.CollisionResponse();
                }
            }
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            CursorState = CursorState.Normal; 

            //if (limitFps)
            //    stopwatch = Stopwatch.StartNew();

            textGenerator = new TextGenerator();
            imGuiController = new ImGuiController(ClientSize.X, ClientSize.Y, ref editorProperties);
            textureManager.AddTexture("ui_play.png", flipY:false);
            textureManager.AddTexture("ui_play.png", flipY: false);
            textureManager.AddTexture("ui_stop.png", flipY:false);
            textureManager.AddTexture("ui_pause.png", flipY:false);
            textureManager.AddTexture("ui_screen.png", flipY:false);
            textureManager.AddTexture("ui_missing.png", flipY:false);
            editorData.objects = objects;
            editorData.particleSystems = particleSystems;
            editorData.pointLights = pointLights;
            editorData.assets = FileManager.GetAllAssets();
            textureManager.GetAssetTextures(editorData);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            // OPENGL init
            //indirectBuffer = new IndirectBuffer();
            visibilityVbo = new VisibilityVBO(DynamicCopy: true);
            frustumVbo = new VBO(DynamicCopy: true);
            //drawCommandsVbo = new DrawCommandVBO(DynamicCopy: true);

            outlineVbo = new VBO();
            outlineVao = new VAO(3);
            outlineVao.LinkToVAO(0, 3, outlineVbo);

            meshVbo = new VBO();
            meshVao = new VAO(Mesh.floatCount);
            meshVao.LinkToVAO(0, 4, meshVbo);
            meshVao.LinkToVAO(1, 3, meshVbo);
            meshVao.LinkToVAO(2, 2, meshVbo);
            meshVao.LinkToVAO(3, 4, meshVbo);
            meshVao.LinkToVAO(4, 3, meshVbo);

            //GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, drawCommandsVbo.id);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, visibilityVbo.id);
            //GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, meshVbo.id);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 2, frustumVbo.id);

            instancedMeshVbo = new VBO();
            instancedMeshVao = new InstancedVAO(InstancedMesh.floatCount, InstancedMesh.instancedFloatCount);
            instancedMeshVao.LinkToVAO(0, 4, meshVbo);
            instancedMeshVao.LinkToVAO(1, 3, meshVbo);
            instancedMeshVao.LinkToVAO(2, 2, meshVbo);
            instancedMeshVao.LinkToVAO(3, 4, meshVbo);
            instancedMeshVao.LinkToVAO(4, 3, meshVbo);
            instancedMeshVao.LinkToVAOInstanceData(5, 4, 1, instancedMeshVbo);
            instancedMeshVao.LinkToVAOInstanceData(6, 4, 1, instancedMeshVbo);
            instancedMeshVao.LinkToVAOInstanceData(7, 3, 1, instancedMeshVbo);
            instancedMeshVao.LinkToVAOInstanceData(8, 4, 1, instancedMeshVbo);

            textVbo = new VBO();
            textVao = new VAO(TextMesh.floatCount);
            textVao.LinkToVAO(0, 4, textVbo);
            textVao.LinkToVAO(1, 4, textVbo);
            textVao.LinkToVAO(2, 2, textVbo);

            noTexVbo = new VBO();
            noTexVao = new VAO(NoTextureMesh.floatCount);
            noTexVao.LinkToVAO(0, 4, noTexVbo);
            noTexVao.LinkToVAO(1, 4, noTexVbo);

            uiTexVbo = new VBO();
            uiTexVao = new VAO(UITextureMesh.floatCount);
            uiTexVao.LinkToVAO(0, 4, uiTexVbo);
            uiTexVao.LinkToVAO(1, 4, uiTexVbo);
            uiTexVao.LinkToVAO(2, 2, uiTexVbo);

            wireVbo = new VBO();
            wireVao = new VAO(WireframeMesh.floatCount);
            wireVao.LinkToVAO(0, 4, wireVbo);
            wireVao.LinkToVAO(1, 4, wireVbo);

            aabbVbo = new VBO();
            aabbVao = new VAO(3);
            aabbVao.LinkToVAO(0, 3, aabbVbo);

            // Create the shader program
            cullingProgram = new Shader(new List<string>() { "cullingshader.comp" });
            //shaderProgram = new Shader(new List<string>() { "DefaultForGeom.vert", "outline.geom", "Default.frag" });
            outlineShader = new Shader(new List<string>() { "outline.vert", "outline.frag" });
            shaderProgram = new Shader(new List<string>() { "Default.vert", "Default.frag" });
            instancedShaderProgram = new Shader(new List<string>() { "Instanced.vert", "Default.frag" });
            posTexShader = new Shader(new List<string>() { "postex.vert", "postex.frag" });
            noTextureShaderProgram = new Shader(new List<string>() { "noTexture.vert", "noTexture.frag" });
            aabbShaderProgram = new Shader(new List<string>() { "aabb.vert", "aabb.frag" });

            // Create Physx context
            physx = new Physx(true);

            // Create Sound Manager
            soundManager = new SoundManager();

            // Add test sound
            //soundManager.CreateSoundEmitter("nyanya.ogg", new Vector3(0,-28,0));
            //soundManager.PlayAll();

            //Camera
            Camera camera = new Camera(windowSize);
            camera.UpdateVectors();

            //TEMP---------------------------------------
            //camera.position.X = -6.97959471f;
            //camera.position.Z = -7.161373f;
            //camera.yaw = 45.73648f;
            //camera.pitch = -18.75002f;
            //-------------------------------------------

            noTextureShaderProgram.Use();
            //Vector3 characterPos = new Vector3(0, 20, 0);
            Vector3 characterPos = new Vector3(4.5f, 1.3f, 0);
            character = new Character(new WireframeMesh(wireVao, wireVbo, noTextureShaderProgram.id, ref camera, Color4.White), ref physx, characterPos, camera);
            character.camera.SetYaw(180f);
            character.camera.SetPitch(0f);

            //Point Lights
            //pointLights.Add(new PointLight(new Vector3(0, 5000, 0), Color4.White, meshVao.id, shaderProgram.id, ref frustum, ref camera, noTexVao, noTexVbo, noTextureShaderProgram.id, pointLights.Count));

            shaderProgram.Use();
            PointLight.SendToGPU(ref pointLights, shaderProgram.id, editorProperties.gameRunning);

            // Projection matrix and mesh loading

            //objects.Add(new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, "spiro.obj", "High.png", windowSize, ref frustum, ref camera, ref textureCount), ObjectType.TriangleMeshWithCollider, ref physx));
            //objects.Last().BuildBVH(shaderProgram, noTextureShaderProgram);

            Object o = new Object(ObjectType.TriangleMeshWithCollider, ref physx);
            o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "level2Rot.obj", "level.png", windowSize, ref camera, ref o));
            objects.Add(o);

            Object o2 = new Object(ObjectType.Cube, ref physx);
            o2.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "cube", Object.GetUnitCube(), "red_t.png", windowSize, ref camera, ref o2));
            objects.Add(o2);

            //objects.Last().BuildBVH(shaderProgram, noTextureShaderProgram);
            //objects.Last().BuildBSP();
            //objects.Last().BuildOctree();
            //objects.Last().BuildGrid(shaderProgram, noTextureShaderProgram);

            //objects.Add(new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, "core_transfer.obj", "High.png", windowSize, ref frustum, ref camera, ref textureCount), ObjectType.TriangleMeshWithCollider, ref physx));
            ////objects.Last().BuildBVH(shaderProgram, noTextureShaderProgram);
            //objects.Last().BuildBSP();
            //objects.Last().BuildGrid(shaderProgram, noTextureShaderProgram);

            //objects.Add(new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, Object.GetUnitCube(), "space.png", windowSize, ref frustum, ref camera, ref textureCount), ObjectType.Cube, ref physx));
            //objects.Last().SetSize(new Vector3(10, 2, 10));
            //objects.Last().AddCubeCollider(true);

            //objects.Add(new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, Object.GetUnitSphere(), "red.png", -1, windowSize, ref frustum, ref character.camera, ref textureCount), ObjectType.Sphere, ref physx));
            //objects.Last().SetPosition(new Vector3(0, 20, 0));
            //objects.Last().SetSize(2);
            //objects.Last().AddSphereCollider(false);

            //objects.Add(new Object(new InstancedMesh(instancedMeshVao, instancedMeshVbo, instancedShaderProgram.id, Object.GetUnitCube(), windowSize, ref camera, ref textureCount), ObjectType.TriangleMesh, ref physx));

            //for (int i = 0; i < 10000; i++)
            //{
            //    InstancedMeshData instData = new InstancedMeshData();
            //    instData.Position = Helper.GetRandomVectorInAABB(new AABB(new Vector3(-100, -100, -100), new Vector3(100, 100, 100)));
            //    instData.Rotation = Helper.GetRandomQuaternion();
            //    //instData.Scale = Helper.GetRandomScale(new AABB(new Vector3(1, 1, 1), new Vector3(5, 5, 5)));
            //    instData.Scale = new Vector3(3, 3, 3);
            //    instData.Color = Helper.GetRandomColor();

            //    ((InstancedMesh)objects.Last().GetMesh()).instancedData.Add(instData);
            //}

            //ParticleSystem ps = new ParticleSystem(new Object(new InstancedMesh(instancedMeshVao, instancedMeshVbo, instancedShaderProgram.id, Object.GetUnitFace(), "smoke.png", windowSize, ref camera, ref textureCount), ObjectType.TriangleMesh, ref physx));
            //ps.GetObject().SetBillboarding(true);
            //ps.emitTimeSec = 0.2f;
            //ps.startSpeed = 3;
            //ps.endSpeed = 3;
            //ps.lifetime = 2;
            //ps.randomDir = true;
            //ps.startScale = new Vector3(10, 10, 10);
            //ps.endScale = new Vector3(10, 10, 10);
            //ps.startColor = Helper.ColorFromRGBA(255, 0, 0);
            //ps.endColor = Helper.ColorFromRGBA(255, 0, 0, 0);
            //particleSystems.Add(ps);

            //noTextureShaderProgram.Use();
            //List<WireframeMesh> aabbs = objects.Last().BVHStruct.ExtractWireframes(objects.Last().BVHStruct.Root, wireVao, wireVbo, noTextureShaderProgram.id, ref frustum, ref character.camera);
            //foreach (WireframeMesh mesh in aabbs)
            //{
            //    Object aabbO = new Object(mesh, ObjectType.Wireframe, ref physx);
            //    aabbsToChange.Add(aabbO);
            //    AddObject(aabbO);
            //}

            //posTexShader.Use();

            //Object textObj1 = new Object(ObjectType.TextMesh, ref physx);
            //TextMesh m1 = new TextMesh(textVao, textVbo, posTexShader.id, "font.png", windowSize, ref textGenerator, ref textObj1);
            //textObj1.AddMesh(m1);
            //((TextMesh)textObj1.GetMesh()).ChangeText("Position = (" + character.PStr + ")");
            //((TextMesh)textObj1.GetMesh()).Position = new Vector2(10, windowSize.Y - 35);
            //((TextMesh)textObj1.GetMesh()).Scale = new Vector2(1.5f, 1.5f);
            //AddText(textObj1, "Position");

            //Object textObj2 = new Object(ObjectType.TextMesh, ref physx);
            //TextMesh m2 = new TextMesh(textVao, textVbo, posTexShader.id, "font.png", windowSize, ref textGenerator, ref textObj2);
            //textObj2.AddMesh(m2);
            //((TextMesh)textObj2.GetMesh()).ChangeText("Velocity = (" + character.VStr + ")");
            //((TextMesh)textObj2.GetMesh()).Position = new Vector2(10, windowSize.Y - 65);
            //((TextMesh)textObj2.GetMesh()).Scale = new Vector2(1.5f, 1.5f);
            //AddText(textObj2, "Velocity");

            //Object textObj3 = new Object(ObjectType.TextMesh, ref physx);
            //TextMesh m3 = new TextMesh(textVao, textVbo, posTexShader.id, "font.png", windowSize, ref textGenerator, ref textObj3);
            //textObj3.AddMesh(m3);
            //((TextMesh)textObj3.GetMesh()).ChangeText("Looking = (" + character.LStr + ")");
            //((TextMesh)textObj3.GetMesh()).Position = new Vector2(10, windowSize.Y - 95);
            //((TextMesh)textObj3.GetMesh()).Scale = new Vector2(1.5f, 1.5f);
            //AddText(textObj3, "Looking");

            //Object textObj4 = new Object(ObjectType.TextMesh, ref physx);
            //TextMesh m4 = new TextMesh(textVao, textVbo, posTexShader.id, "font.png", windowSize, ref textGenerator, ref textObj4);
            //textObj4.AddMesh(m4);
            //((TextMesh)textObj4.GetMesh()).ChangeText("Noclip = (" + character.noClip.ToString() + ")");
            //((TextMesh)textObj4.GetMesh()).Position = new Vector2(10, windowSize.Y - 125);
            //((TextMesh)textObj4.GetMesh()).Scale = new Vector2(1.5f, 1.5f);
            //AddText(textObj4, "Noclip");

            //uiTexMeshes.Add(new UITextureMesh(uiTexVao, uiTexVbo, posTexShader.id, "bmp_24.bmp", new Vector2(10, 10), new Vector2(100, 100), windowSize, ref textureCount));

            // We have text on screen
            if (haveText)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }

            objects.Sort();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            imGuiController.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            imGuiController.MouseScroll(e.Offset);
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            queryPool.DeleteQueries();

            GL.DeleteVertexArray(meshVao.id);
            GL.DeleteVertexArray(textVao.id);
            GL.DeleteVertexArray(noTexVao.id);
            GL.DeleteVertexArray(uiTexVao.id);
            GL.DeleteVertexArray(wireVao.id);

            foreach(Object obj in objects)
            {
                BaseMesh mesh = obj.GetMesh();
                GL.DeleteBuffer(mesh.vbo);               
            }

            shaderProgram.Unload();
            noTextureShaderProgram.Unload();
            posTexShader.Unload();

            FileManager.DisposeStreams();
            textureManager.DeleteTextures();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            Resized(e);
        }

        private void Resized(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            windowSize.X = e.Width;
            windowSize.Y = e.Height;

            gameWindowProperty.gameWindowSize = new Vector2(windowSize.X * (1.0f - (gameWindowProperty.leftPanelPercent + gameWindowProperty.rightPanelPercent)),
                                                            windowSize.Y * (1 - gameWindowProperty.bottomPanelPercent) - gameWindowProperty.topPanelSize - gameWindowProperty.bottomPanelSize);
            gameWindowProperty.gameWindowPos = new Vector2(windowSize.X * gameWindowProperty.leftPanelPercent, windowSize.Y * gameWindowProperty.bottomPanelPercent + gameWindowProperty.bottomPanelSize);

            if (imGuiController != null)
                imGuiController.WindowResized(ClientSize.X, ClientSize.Y);
        }

        private void ResizedEditorWindow()
        {
            gameWindowProperty.gameWindowSize = new Vector2(windowSize.X * (1.0f - (gameWindowProperty.leftPanelPercent + gameWindowProperty.rightPanelPercent)),
                                                            windowSize.Y * (1 - gameWindowProperty.bottomPanelPercent) - gameWindowProperty.topPanelSize - gameWindowProperty.bottomPanelSize);
            gameWindowProperty.gameWindowPos = new Vector2(windowSize.X * gameWindowProperty.leftPanelPercent, windowSize.Y * gameWindowProperty.bottomPanelPercent + gameWindowProperty.bottomPanelSize);
        }
    }
}
