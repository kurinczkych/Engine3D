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

#pragma warning disable CS0649
#pragma warning disable CS8618

namespace Engine3D
{

    public class Engine : GameWindow
    {

        #region OPENGL
        private VAO meshVao;
        private VBO meshVbo;

        private InstancedVAO instancedMeshVao;
        private VBO instancedMeshVbo;

        private VAO testVao;
        private VBO testVbo;

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

        private Shader shaderProgram;
        private Shader instancedShaderProgram;
        private Shader posTexShader;
        private Shader noTextureShaderProgram;
        private Shader aabbShaderProgram;
        private int textureCount = 0;
        #endregion

        #region Program variables
        public static Random rnd = new Random((int)DateTime.Now.Ticks);
        private bool haveText = false;

        private Vector2 origWindowSize;
        private Vector2 windowSize;
        private Box2i previousWindowBounds;
        private float gameWindowPercent = 0.7f;
        private Vector2 gameWindowPos;
        private Vector2 gameWindowSize;

        private SoundManager soundManager;
        
        private TextGenerator textGenerator;
        private Dictionary<string, TextMesh> texts;
        #endregion

        #region Engine variables
        private List<Object> objects;
        private Character character;
        private Camera camera;

        private Frustum frustum;
        private List<PointLight> pointLights;
        private List<ParticleSystem> particleSystems;
        private Physx physx;

        private bool useOcclusionCulling = false;
        private QueryPool queryPool;
        private Dictionary<int, Tuple<int, BVHNode>> pendingQueries;
        #endregion

        #region FPS and Frame limiting
        private double totalTime;
        private const int SAMPLE_SIZE = 30;
        private Queue<double> sampleTimes = new Queue<double>(SAMPLE_SIZE);

        private int maxFps = 0;
        private int minFps = int.MaxValue;
        private Stopwatch maxminStopwatch;

        private bool limitFps = false;
        private const double TargetDeltaTime = 1.0 / 60.0; // for 60 FPS
        private Stopwatch stopwatch;
        #endregion

        public Engine(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            windowSize = new Vector2(width, height);
            origWindowSize = new Vector2(width, height);
            CenterWindow(new Vector2i(width, height));

            gameWindowSize = new Vector2(width * gameWindowPercent, height * gameWindowPercent);
            gameWindowPos = new Vector2((windowSize.X - gameWindowSize.X) / 2, (windowSize.Y - gameWindowSize.Y) / 2);

            frustum = new Frustum();
            shaderProgram = new Shader();
            posTexShader = new Shader();
            pointLights = new List<PointLight>();
            particleSystems = new List<ParticleSystem>();
            texts = new Dictionary<string, TextMesh>();

            maxminStopwatch = new Stopwatch();
            maxminStopwatch.Start();

            objects = new List<Object>();
            queryPool = new QueryPool(1000);
            pendingQueries = new Dictionary<int, Tuple<int, BVHNode>>();
        }

        private double DrawFps(double deltaTime)
        {
            if (sampleTimes.Count >= SAMPLE_SIZE)
            {
                // Remove the oldest time (from the front of the queue)
                totalTime -= sampleTimes.Dequeue();
            }

            // Add the newest time (to the back of the queue)
            sampleTimes.Enqueue(deltaTime);
            totalTime += deltaTime;

            double averageDeltaTime = totalTime / sampleTimes.Count;
            double fps = 1.0 / averageDeltaTime;


            if (!maxminStopwatch.IsRunning)
            {
                if (fps > maxFps)
                    maxFps = (int)fps;
                if (fps < minFps)
                    minFps = (int)fps;
            }
            else
            {
                if (maxminStopwatch.ElapsedMilliseconds > 1000)
                    maxminStopwatch.Stop();
            }

            Title = "3D Engine    |    FPS: " + Math.Round(fps, 2).ToString() + 
                             "    |    MaxFPS: " + maxFps.ToString() + "    |    MinFPS: " + minFps.ToString();

            return fps;
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
            else if(prevMeshType == typeof(TestMesh) && prevMeshType != currentMeshType)
            {
                testVbo.Buffer(vertices);
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
            double fps = DrawFps(args.Time);

            //GL.Viewport((int)gameWindowPos.X, (int)gameWindowPos.Y, (int)gameWindowSize.X, (int)gameWindowSize.Y);
            //GL.Enable(EnableCap.ScissorTest);
            //GL.Scissor((int)gameWindowPos.X, (int)gameWindowPos.Y, (int)gameWindowSize.X, (int)gameWindowSize.Y);

            #region GameWindow

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Enable(EnableCap.DepthTest);

            // Triangle frustum visibility calculation
            foreach (Object obj in objects)
            {
                obj.GetMesh().CalculateFrustumVisibility(character.camera, obj.BVHStruct);
            }

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
                    posVertices.AddRange(((Mesh)obj.GetMesh()).DrawOnlyPos(aabbVao, aabbShaderProgram));
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
                        OcclusionCulling.PerformOcclusionQueriesForBVH(obj.BVHStruct, aabbVbo, aabbVao, aabbShaderProgram, character.camera, ref queryPool, ref pendingQueries, true);
                    }
                }
                else
                {
                    ;
                    foreach (Object obj in triangleMeshObjects)
                    {
                        OcclusionCulling.PerformOcclusionQueriesForBVH(obj.BVHStruct, aabbVbo, aabbVao, aabbShaderProgram, character.camera, ref queryPool, ref pendingQueries, false);
                    }
                }
                GL.Enable(EnableCap.CullFace);
                
                
                GL.ColorMask(true, true, true, true);
            }

            //------------------------------------------------------------

            //character.camera.SetPosition(character.camera.GetPosition() +
            //    new Vector3(-(float)Math.Cos(MathHelper.DegreesToRadians(character.camera.GetYaw())) * 8, 10, -(float)Math.Sin(MathHelper.DegreesToRadians(character.camera.GetYaw()))) * 8);
            //character.camera.SetPosition(character.camera.GetPosition() +
            //    new Vector3(0, 100, 0));


            GL.ClearColor(Color4.Cyan);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            shaderProgram.Use();
            PointLight.SendToGPU(ref pointLights, shaderProgram.id);

            Type? currentMeshType = null;
            //BaseMesh? currentMesh = null;
            List<float> vertices = new List<float>();
            foreach (Object o in objects)
            {
                ObjectType objectType = o.GetObjectType();
                if (objectType == ObjectType.Cube ||
                   objectType == ObjectType.Sphere ||
                   objectType == ObjectType.Capsule ||
                   objectType == ObjectType.TriangleMesh)
                {
                    if (o.GetMesh().GetType() == typeof(Mesh))
                    {
                        Mesh mesh = (Mesh)o.GetMesh();
                        if (currentMeshType == null || currentMeshType != mesh.GetType())
                        {
                            shaderProgram.Use();
                        }
                        mesh.UpdateFrustumAndCamera(ref character.camera);

                        if (useOcclusionCulling && objectType == ObjectType.TriangleMesh)
                        {
                            List<triangle> notOccludedTris = new List<triangle>();
                            OcclusionCulling.TraverseBVHNode(o.BVHStruct.Root, ref notOccludedTris, ref frustum);

                            vertices.AddRange(mesh.DrawNotOccluded(notOccludedTris));
                            currentMeshType = typeof(Mesh);

                            if (vertices.Count > 0)
                            {
                                meshVbo.Buffer(vertices);
                                GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                                vertices.Clear();
                            }
                        }
                        else
                        {
                            vertices.AddRange(mesh.Draw());
                            currentMeshType = typeof(Mesh);

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
                        mesh.UpdateFrustumAndCamera(ref character.camera);

                        List<float> instancedVertices = new List<float>();
                        (vertices, instancedVertices) = mesh.Draw();
                        currentMeshType = typeof(InstancedMesh);

                        meshVbo.Buffer(vertices);
                        instancedMeshVbo.Buffer(instancedVertices);
                        GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, vertices.Count, mesh.instancedData.Count());
                        vertices.Clear();
                    }
                }
                else if (objectType == ObjectType.TestMesh)
                {
                    TestMesh mesh = (TestMesh)o.GetMesh();

                    //if (DrawCorrectMesh(ref vertices, currentMesh, typeof(TestMesh)))
                    //    vertices = new List<float>();

                    if (currentMeshType == null || currentMeshType != mesh.GetType())
                    {
                        shaderProgram.Use();
                    }

                    mesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
                    vertices.AddRange(mesh.Draw());
                    currentMeshType = typeof(TestMesh);

                    testVbo.Buffer(vertices);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                    vertices.Clear();
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

                    mesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
                    vertices.AddRange(mesh.Draw());
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

                    mesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
                    vertices.AddRange(mesh.Draw());
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

                    vertices.AddRange(mesh.Draw());
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

                    vertices.AddRange(mesh.Draw());
                    currentMeshType = typeof(TextMesh);
                }
            }


            if (DrawCorrectMesh(ref vertices, currentMeshType == null ? typeof(int) : currentMeshType, typeof(int), null))
                vertices = new List<float>();

            //GL.BlendFunc(BlendingFactor.SrcColor, BlendingFactor.OneMinusSrcColor);
            foreach (ParticleSystem ps in particleSystems)
            {
                Object psO = ps.GetObject();
                psO.GetMesh().CalculateFrustumVisibility(character.camera, null);

                InstancedMesh mesh = (InstancedMesh)psO.GetMesh();
                if (currentMeshType == null || currentMeshType != mesh.GetType())
                {
                    instancedShaderProgram.Use();
                }
                mesh.UpdateFrustumAndCamera(ref character.camera);

                List<float> instancedVertices = new List<float>();
                (vertices, instancedVertices) = mesh.Draw();
                currentMeshType = typeof(InstancedMesh);

                meshVbo.Buffer(vertices);
                instancedMeshVbo.Buffer(instancedVertices);
                GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, vertices.Count, mesh.instancedData.Count());
                vertices.Clear();
            }
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

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

            texts["Position"].ChangeText("Position = (" + character.PStr + ")");
            texts["Velocity"].ChangeText("Velocity = (" + character.VStr + ")");
            texts["Looking"].ChangeText("Looking = (" + character.LStr + ")");
            texts["Noclip"].ChangeText("Noclip = (" + character.noClip.ToString() + ")");

            #endregion

            //GL.Disable(EnableCap.ScissorTest);
            //GL.Viewport(0, 0, (int)windowSize.X, (int)windowSize.Y);

            Context.SwapBuffers();

            base.OnRenderFrame(args);

            if (limitFps)
            {
                double elapsed = stopwatch.Elapsed.TotalSeconds;
                if (elapsed < TargetDeltaTime)
                {
                    double sleepTime = TargetDeltaTime - elapsed;
                    Thread.Sleep((int)(sleepTime * 1000));
                }
                stopwatch.Restart();
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (KeyboardState.IsKeyReleased(Keys.Escape))
            {
                CursorState = CursorState == CursorState.Grabbed ? CursorState.Normal : CursorState.Grabbed;
            }
            if (KeyboardState.IsKeyReleased(Keys.F2))
                WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;

            character.CalculateVelocity(KeyboardState, MouseState, args);
            character.UpdatePosition(KeyboardState, MouseState, args);
            character.AfterUpdate(MouseState, args);

            soundManager.SetListener(character.camera.GetPosition());

            foreach(ParticleSystem ps in particleSystems)
            {
                ps.Update((float)args.Time);
            }

            
            //if (temp != Math.Round(totalTime) || temp == -1)
            //{
            //    Object obj = new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, Object.GetUnitSphere(), "red.png", windowSize, ref frustum, ref character.camera, ref textureCount), ObjectType.Sphere, ref physx);
            //    obj.SetPosition(new Vector3(rnd.Next(-20, 20), 50, rnd.Next(-20, 20)));
            //    obj.SetSize(2);
            //    obj.AddSphereCollider(false);
            //    temp += 1;
            //    AddObject(obj);
            //}

            if (totalTime > 0)
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
            CursorState = CursorState.Grabbed;

            if (limitFps)
                stopwatch = Stopwatch.StartNew();

            textGenerator = new TextGenerator();

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            // OPENGL init
            meshVbo = new VBO();
            meshVao = new VAO(Mesh.floatCount);
            meshVao.LinkToVAO(0, 4, meshVbo);
            meshVao.LinkToVAO(1, 3, meshVbo);
            meshVao.LinkToVAO(2, 2, meshVbo);
            meshVao.LinkToVAO(3, 4, meshVbo);
            meshVao.LinkToVAO(4, 3, meshVbo);

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

            testVbo = new VBO();
            testVao = new VAO(Mesh.floatCount);
            testVao.LinkToVAO(0, 4, testVbo);
            testVao.LinkToVAO(1, 3, testVbo);
            testVao.LinkToVAO(2, 2, testVbo);
            testVao.LinkToVAO(3, 4, testVbo);
            testVao.LinkToVAO(4, 3, testVbo);

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
            shaderProgram = new Shader("Default.vert", "Default.frag");
            instancedShaderProgram = new Shader("Instanced.vert", "Default.frag");
            posTexShader = new Shader("postex.vert", "postex.frag");
            noTextureShaderProgram = new Shader("noTexture.vert", "noTexture.frag");
            aabbShaderProgram = new Shader("aabb.vert", "aabb.frag");

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
            character = new Character(new WireframeMesh(wireVao, wireVbo, noTextureShaderProgram.id, ref frustum, ref camera, Color4.White), ref physx, characterPos, camera);
            character.camera.SetYaw(180f);
            character.camera.SetPitch(0f);

            //Point Lights
            //pointLights.Add(new PointLight(new Vector3(0, 5000, 0), Color4.White, meshVao.id, shaderProgram.id, ref frustum, ref camera, noTexVao, noTexVbo, noTextureShaderProgram.id, pointLights.Count));

            shaderProgram.Use();
            PointLight.SendToGPU(ref pointLights, shaderProgram.id);

            // Projection matrix and mesh loading

            //objects.Add(new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, "spiro.obj", "High.png", windowSize, ref frustum, ref camera, ref textureCount), ObjectType.TriangleMeshWithCollider, ref physx));
            //objects.Last().BuildBVH(shaderProgram, noTextureShaderProgram);

            //objects.Add(new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, "level2Rot.obj", "level.png", windowSize, ref frustum, ref camera, ref textureCount), ObjectType.TriangleMeshWithCollider, ref physx));
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

            ParticleSystem ps = new ParticleSystem(new Object(new InstancedMesh(instancedMeshVao, instancedMeshVbo, instancedShaderProgram.id, Object.GetUnitFace(), "smoke.png", windowSize, ref camera, ref textureCount), ObjectType.TriangleMesh, ref physx));
            ps.GetObject().SetBillboarding(true);
            ps.emitTimeSec = 0.2f;
            ps.startSpeed = 3;
            ps.endSpeed = 3;
            ps.lifetime = 2;
            ps.randomDir = true;
            ps.startScale = new Vector3(10, 10, 10);
            ps.endScale = new Vector3(10, 10, 10);
            ps.startColor = Helper.ColorFromRGBA(255, 0, 0);
            ps.endColor = Helper.ColorFromRGBA(255, 0, 0, 0);
            particleSystems.Add(ps);

            //noTextureShaderProgram.Use();
            //List<WireframeMesh> aabbs = objects.Last().BVHStruct.ExtractWireframes(objects.Last().BVHStruct.Root, wireVao, wireVbo, noTextureShaderProgram.id, ref frustum, ref character.camera);
            //foreach (WireframeMesh mesh in aabbs)
            //{
            //    Object aabbO = new Object(mesh, ObjectType.Wireframe, ref physx);
            //    aabbsToChange.Add(aabbO);
            //    AddObject(aabbO);
            //}

            //posTexShader.Use();

            Object textObj1 = new Object(new TextMesh(textVao, textVbo, posTexShader.id, "font.png", windowSize, ref textGenerator, ref textureCount), ObjectType.TextMesh, ref physx);
            ((TextMesh)textObj1.GetMesh()).ChangeText("Position = (" + character.PStr + ")");
            ((TextMesh)textObj1.GetMesh()).Position = new Vector2(10, windowSize.Y - 35);
            ((TextMesh)textObj1.GetMesh()).Scale = new Vector2(1.5f, 1.5f);
            AddText(textObj1, "Position");

            Object textObj2 = new Object(new TextMesh(textVao, textVbo, posTexShader.id, "font.png", windowSize, ref textGenerator, ref textureCount), ObjectType.TextMesh, ref physx);
            ((TextMesh)textObj2.GetMesh()).ChangeText("Velocity = (" + character.VStr + ")");
            ((TextMesh)textObj2.GetMesh()).Position = new Vector2(10, windowSize.Y - 65);
            ((TextMesh)textObj2.GetMesh()).Scale = new Vector2(1.5f, 1.5f);
            AddText(textObj2, "Velocity");

            Object textObj3 = new Object(new TextMesh(textVao, textVbo, posTexShader.id, "font.png", windowSize, ref textGenerator, ref textureCount), ObjectType.TextMesh, ref physx);
            ((TextMesh)textObj3.GetMesh()).ChangeText("Looking = (" + character.LStr + ")");
            ((TextMesh)textObj3.GetMesh()).Position = new Vector2(10, windowSize.Y - 95);
            ((TextMesh)textObj3.GetMesh()).Scale = new Vector2(1.5f, 1.5f);
            AddText(textObj3, "Looking");

            Object textObj4 = new Object(new TextMesh(textVao, textVbo, posTexShader.id, "font.png", windowSize, ref textGenerator, ref textureCount), ObjectType.TextMesh, ref physx);
            ((TextMesh)textObj4.GetMesh()).ChangeText("Noclip = (" + character.noClip.ToString() + ")");
            ((TextMesh)textObj4.GetMesh()).Position = new Vector2(10, windowSize.Y - 125);
            ((TextMesh)textObj4.GetMesh()).Scale = new Vector2(1.5f, 1.5f);
            AddText(textObj4, "Noclip");

            //uiTexMeshes.Add(new UITextureMesh(uiTexVao, uiTexVbo, posTexShader.id, "bmp_24.bmp", new Vector2(10, 10), new Vector2(100, 100), windowSize, ref textureCount));

            // We have text on screen
            if (haveText)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }

            objects.Sort();
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            queryPool.DeleteQueries();

            GL.DeleteVertexArray(meshVao.id);
            GL.DeleteVertexArray(testVao.id);
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
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            windowSize.X = e.Width;
            windowSize.Y = e.Height;
            gameWindowSize = new Vector2(windowSize.X * gameWindowPercent, windowSize.Y * gameWindowPercent);
            gameWindowPos = new Vector2((windowSize.X - gameWindowSize.X) / 2, (windowSize.Y - gameWindowSize.Y) / 2);
        }
    }
}
