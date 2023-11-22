using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

#pragma warning disable CS0649
#pragma warning disable CS8618

namespace Engine3D
{
    public class GameWindowProperty
    {
        public float topPanelSize = 50;
        public float bottomPanelSize = 25;

        public float bottomPanelPercent = 0.25f;
        public float leftPanelPercent = 0.15f;
        public float rightPanelPercent = 0.20f;

        public float origBottomPanelPercent;
        public float origLeftPanelPercent;
        public float origRightPanelPercent;

        public Vector2 gameWindowPos;
        public Vector2 gameWindowSize;

        public GameWindowProperty()
        {
            origBottomPanelPercent = bottomPanelPercent;
            origLeftPanelPercent = leftPanelPercent;
            origRightPanelPercent = rightPanelPercent;
        }
    }

    public partial class Engine : GameWindow
    {
        public class SavedStart
        {
            public List<Object> objects;
            public Dictionary<string, TextMesh> texts;
            Character character;
        }

        #region OPENGL
        public static GLState GLState = new GLState();

        private IndirectBuffer indirectBuffer;
        private VBO visibilityVbo;
        private VBO drawCommandsVbo;
        private VBO frustumVbo;

        private VAO onlyPosVao;
        private VBO onlyPosVbo;
        private IBO onlyPosIbo;

        private VAO onlyPosAndNormalVao;
        private VBO onlyPosAndNormalVbo;
        private IBO onlyPosAndNormalIbo;

        private InstancedVAO instancedOnlyPosAndNormalVao;
        private VBO instancedOnlyPosAndNormalVbo;

        private VAO meshVao;
        private VBO meshVbo;
        private IBO meshIbo;

        private InstancedVAO instancedMeshVao;
        private VBO instancedMeshVbo;

        private VAO textVao;
        private VBO textVbo;

        private VAO uiTexVao;
        private VBO uiTexVbo;

        private VAO wireVao;
        private VBO wireVbo;

        private VAO aabbVao;
        private VBO aabbVbo;

        private Shader cullingProgram;
        private Shader outlineShader;
        private Shader outlineInstancedShader;
        private Shader pickingShader;
        private Shader pickingInstancedShader;
        private Shader shaderProgram;
        private Shader instancedShaderProgram;
        private Shader posTexShader;
        private Shader onlyPosShaderProgram;
        private Shader aabbShaderProgram;
        #endregion

        #region Program variables
        public static Random rnd = new Random((int)DateTime.Now.Ticks);
        private bool haveText = false;
        private bool firstRun = true;

        private Vector2 origWindowSize;
        private Vector2 windowSize;
        private Vector2 gameWindowMousePos;

        private SoundManager soundManager;
        private AssetManager assetManager;
        private Stopwatch fileDetectorStopWatch;
        public static TextureManager textureManager;
        public static ConsoleManager consoleManager = new ConsoleManager();

        private TextGenerator textGenerator;
        private Dictionary<string, TextMesh> texts;

        private PickingTexture pickingTexture;
        #endregion

        #region Editor moving
        private float deltaX;
        private float deltaY;
        private Vector2 lastPos;
        private bool firstMove = true;
        private float sensitivity = 130;

        private Axis? objectMovingAxis;
        private Plane? objectMovingPlane;
        private Vector3 objectMovingOrig;

        #endregion

        #region UI variables
        private EditorData editorData;
        private ImGuiController imGuiController;
        #endregion

        #region Engine variables
        public static int objectID = 1;
        private List<Object> objects;
        private List<Object> _meshObjects;
        private List<Object> _instObjects;

        private Character character;
        private List<float> vertices = new List<float>();
        private List<uint> indices = new List<uint>();
        private List<float> verticesUnique = new List<float>();

        private List<PointLight> pointLights;
        private List<ParticleSystem> particleSystems;
        private Physx physx;

        private bool useOcclusionCulling = false;
        private QueryPool queryPool;
        private Dictionary<int, Tuple<int, BVHNode>> pendingQueries;
        #endregion

        public Engine(int width, int height) : base(GameWindowSettings.Default, new NativeWindowSettings() { StencilBits = 8, DepthBits = 32 })
        {
            Title = "3D Engine";

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            editorData = new EditorData();
            editorData.gameWindow = new GameWindowProperty();
            editorData.gameWindow.gameWindowSize = new Vector2();
            editorData.gameWindow.gameWindowPos = new Vector2();

            windowSize = new Vector2(width, height);
            origWindowSize = new Vector2(width, height);
            CenterWindow(new Vector2i(width, height));

            shaderProgram = new Shader();
            posTexShader = new Shader();
            pointLights = new List<PointLight>();
            particleSystems = new List<ParticleSystem>();
            texts = new Dictionary<string, TextMesh>();
            textureManager = new TextureManager();
            fileDetectorStopWatch = new Stopwatch();

            assetManager = new AssetManager(ref textureManager);
            editorData.assetManager = assetManager;

            objects = new List<Object>();
            _meshObjects = new List<Object>();
            _instObjects = new List<Object>();
            queryPool = new QueryPool(1000);
            pendingQueries = new Dictionary<int, Tuple<int, BVHNode>>();
        }

        public void AddObject(ObjectType type)
        {
            if (type == ObjectType.Cube)
            {
                Object o = new Object(type, ref physx);
                o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "cube", Object.GetUnitCube(), windowSize, ref character.camera, ref o));
                editorData.objects.Add(o);
                _meshObjects.Add(o);
            }
            else if (type == ObjectType.Sphere)
            {
                Object o = new Object(type, ref physx);
                o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "sphere", Object.GetUnitSphere(), windowSize, ref character.camera, ref o));
                editorData.objects.Add(o);
                _meshObjects.Add(o);
            }
            else if (type == ObjectType.Capsule)
            {
                Object o = new Object(type, ref physx);
                o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "capsule", Object.GetUnitCapsule(), windowSize, ref character.camera, ref o));
                editorData.objects.Add(o);
                _meshObjects.Add(o);
            }
            else if (type == ObjectType.Plane)
            {
                Object o = new Object(type, ref physx);
                o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "plane", Object.GetUnitFace(), windowSize, ref character.camera, ref o));
                editorData.objects.Add(o);
                _meshObjects.Add(o);
            }
            else if (type == ObjectType.TriangleMesh)
            {
                Object o = new Object(type, ref physx);
                o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "mesh", new MeshData(), windowSize, ref character.camera, ref o));
                editorData.objects.Add(o);
                _meshObjects.Add(o);
            }
        }

        public void AddMeshObject(string meshName)
        {
            Object o = new Object(ObjectType.TriangleMesh, ref physx);
            o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, Path.GetFileName(meshName), windowSize, ref character.camera, ref o));
            editorData.objects.Add(o);
            _meshObjects.Add(o);
        }

        public void RemoveObject(Object o)
        {
            if (o.hasMesh)
            {
                if (o.meshType == typeof(Mesh))
                    _meshObjects.Remove(o);
                else if (o.meshType == typeof(InstancedMesh))
                    _instObjects.Remove(o);
            }

            textureManager.DeleteTexture(o.textureName);
            imGuiController.SelectItem(null, editorData);
            o.Delete(ref textureManager);
            objects.Remove(o);
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

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            imGuiController.Update(this, (float)args.Time);

            #region Fullscreen scissoring
            if (!editorData.isGameFullscreen)
            {
                GL.Viewport((int)editorData.gameWindow.gameWindowPos.X, (int)editorData.gameWindow.gameWindowPos.Y,
                            (int)editorData.gameWindow.gameWindowSize.X, (int)editorData.gameWindow.gameWindowSize.Y);
                GL.Enable(EnableCap.ScissorTest);
                GL.Scissor((int)editorData.gameWindow.gameWindowPos.X, (int)editorData.gameWindow.gameWindowPos.Y,
                           (int)editorData.gameWindow.gameWindowSize.X, (int)editorData.gameWindow.gameWindowSize.Y);
            }
            #endregion

            vertices.Clear();

            FrustumCalculating();

            OcclusionCuller();

            ObjectAndAxisPicking();

            shaderProgram.Use();
            PointLight.SendToGPU(ref pointLights, shaderProgram.id, editorData.gameRunning);

            DrawObjects();

            DrawParticleSystems();

            DrawMoverGizmo();

            //DrawCameraRay();

            TextUpdating();

            #region Fullscreen scissoring
            if (!editorData.isGameFullscreen)
            {
                GL.Disable(EnableCap.ScissorTest);
                GL.Viewport(0, 0, (int)windowSize.X, (int)windowSize.Y);
            }
            #endregion

            editorData.assetStoreManager.DownloadIfNeeded();
            assetManager.UpdateIfNeeded();

            EditorManaging();

            Context.SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            editorData.fps.Update((float)args.Time);

            if (fileDetectorStopWatch.Elapsed.TotalSeconds > 5)
            {
                fileDetectorStopWatch.Restart();
                FileManager.GetAllAssets(ref assetManager);
            }

            CursorAndGameStateSetting();

            MouseMoving();

            if (editorData.gameRunning == GameState.Running)
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
                EditorMoving(args);
            }

            character.AfterUpdate(MouseState, args, editorData.gameRunning);

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

            textGenerator = new TextGenerator();

            #region Editor data
            imGuiController = new ImGuiController(ClientSize.X, ClientSize.Y, ref editorData);
            textureManager.AddTexture("ui_play.png", out bool successPlay, flipY: false);
            if (!successPlay) { throw new Exception("ui_play.png was not found in the embedded resources!"); }
            textureManager.AddTexture("ui_stop.png", out bool successStop, flipY: false);
            if (!successStop) { throw new Exception("ui_stop.png was not found in the embedded resources!"); }
            textureManager.AddTexture("ui_pause.png", out bool successPause, flipY: false);
            if (!successPause) { throw new Exception("ui_pause.png was not found in the embedded resources!"); }
            textureManager.AddTexture("ui_screen.png", out bool successScreen, flipY: false);
            if (!successScreen) { throw new Exception("ui_screen.png was not found in the embedded resources!"); }
            textureManager.AddTexture("ui_missing.png", out bool successMissing, flipY: false);
            if (!successMissing) { throw new Exception("ui_missing.png was not found in the embedded resources!"); }
            textureManager.AddTexture("ui_folder.png", out bool successFolder, flipY: false);
            if (!successFolder) { throw new Exception("ui_folder.png was not found in the embedded resources!"); }
            textureManager.AddTexture("ui_back.png", out bool successBack, flipY: false);
            if (!successBack) { throw new Exception("ui_back.png was not found in the embedded resources!"); }
            textureManager.AddTexture("ui_gizmo_move.png", out bool successMove, flipY: false);
            if (!successMove) { throw new Exception("ui_back.png was not found in the embedded resources!"); }
            textureManager.AddTexture("ui_gizmo_rotate.png", out bool successRotate, flipY: false);
            if (!successRotate) { throw new Exception("ui_back.png was not found in the embedded resources!"); }
            textureManager.AddTexture("ui_gizmo_scale.png", out bool successScale, flipY: false);
            if (!successScale) { throw new Exception("ui_back.png was not found in the embedded resources!"); }
            textureManager.AddTexture("ui_relative.png", out bool successRelative, flipY: false);
            if (!successRelative) { throw new Exception("ui_relative.png was not found in the embedded resources!"); }
            textureManager.AddTexture("ui_absolute.png", out bool successAbsolute, flipY: false);
            if (!successAbsolute) { throw new Exception("ui_absolute.png was not found in the embedded resources!"); }
            editorData.objects = objects;
            editorData.particleSystems = particleSystems;
            editorData.pointLights = pointLights;
            FileManager.GetAllAssets(ref assetManager);
            editorData.assetStoreManager = new AssetStoreManager(ref assetManager);
            fileDetectorStopWatch.Start();
            #endregion

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            #region VBO and VAO Init
            //indirectBuffer = new IndirectBuffer();
            visibilityVbo = new VisibilityVBO(DynamicCopy: true);
            frustumVbo = new VBO(DynamicCopy: true);
            //drawCommandsVbo = new DrawCommandVBO(DynamicCopy: true);

            onlyPosIbo = new IBO();
            onlyPosVbo = new VBO();
            onlyPosVao = new VAO(3);
            onlyPosVao.LinkToVAO(0, 3, onlyPosVbo);

            onlyPosAndNormalIbo = new IBO();
            onlyPosAndNormalVbo = new VBO();
            onlyPosAndNormalVao = new VAO(6);
            onlyPosAndNormalVao.LinkToVAO(0, 3, onlyPosAndNormalVbo);
            onlyPosAndNormalVao.LinkToVAO(1, 3, onlyPosAndNormalVbo);

            meshIbo = new IBO();
            meshVbo = new VBO();
            meshVao = new VAO(Mesh.floatCount);
            meshVao.LinkToVAO(0, 3, meshVbo);
            meshVao.LinkToVAO(1, 3, meshVbo);
            meshVao.LinkToVAO(2, 2, meshVbo);
            meshVao.LinkToVAO(3, 4, meshVbo);
            meshVao.LinkToVAO(4, 3, meshVbo);

            //meshVao = new VAO(3);
            //meshVao.LinkToVAO(0, 3, meshVbo);

            //GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, drawCommandsVbo.id);
            //GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, visibilityVbo.id);
            //GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, meshVbo.id);
            //GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 2, frustumVbo.id);

            instancedMeshVbo = new VBO();
            instancedMeshVao = new InstancedVAO(InstancedMesh.floatCount, InstancedMesh.instancedFloatCount);
            instancedMeshVao.LinkToVAO(0, 3, meshVbo);
            instancedMeshVao.LinkToVAO(1, 3, meshVbo);
            instancedMeshVao.LinkToVAO(2, 2, meshVbo);
            instancedMeshVao.LinkToVAO(3, 4, meshVbo);
            instancedMeshVao.LinkToVAO(4, 3, meshVbo);
            instancedMeshVao.LinkToVAOInstanceData(5, 3, 1, instancedMeshVbo);
            instancedMeshVao.LinkToVAOInstanceData(6, 4, 1, instancedMeshVbo);
            instancedMeshVao.LinkToVAOInstanceData(7, 3, 1, instancedMeshVbo);
            instancedMeshVao.LinkToVAOInstanceData(8, 4, 1, instancedMeshVbo);

            instancedOnlyPosAndNormalVbo = new VBO();
            instancedOnlyPosAndNormalVao = new InstancedVAO(6, InstancedMesh.instancedFloatCount);
            instancedOnlyPosAndNormalVao.LinkToVAO(0, 3, onlyPosAndNormalVbo);
            instancedOnlyPosAndNormalVao.LinkToVAO(1, 3, onlyPosAndNormalVbo);
            instancedOnlyPosAndNormalVao.LinkToVAOInstanceData(2, 3, 1, instancedOnlyPosAndNormalVbo);
            instancedOnlyPosAndNormalVao.LinkToVAOInstanceData(3, 4, 1, instancedOnlyPosAndNormalVbo);
            instancedOnlyPosAndNormalVao.LinkToVAOInstanceData(4, 3, 1, instancedOnlyPosAndNormalVbo);
            instancedOnlyPosAndNormalVao.LinkToVAOInstanceData(5, 4, 1, instancedOnlyPosAndNormalVbo);

            textVbo = new VBO();
            textVao = new VAO(TextMesh.floatCount);
            textVao.LinkToVAO(0, 4, textVbo);
            textVao.LinkToVAO(1, 4, textVbo);
            textVao.LinkToVAO(2, 2, textVbo);

            uiTexVbo = new VBO();
            uiTexVao = new VAO(UITextureMesh.floatCount);
            uiTexVao.LinkToVAO(0, 4, uiTexVbo);
            uiTexVao.LinkToVAO(1, 4, uiTexVbo);
            uiTexVao.LinkToVAO(2, 2, uiTexVbo);

            wireVbo = new VBO();
            wireVao = new VAO(WireframeMesh.floatCount);
            wireVao.LinkToVAO(0, 3, wireVbo);
            wireVao.LinkToVAO(1, 4, wireVbo);

            aabbVbo = new VBO();
            aabbVao = new VAO(3);
            aabbVao.LinkToVAO(0, 3, aabbVbo);

            #endregion

            #region Shader Init

            // Create the shader program
            cullingProgram = new Shader(new List<string>() { "cullingshader.comp" });
            outlineShader = new Shader(new List<string>() { "outline.vert", "outline.frag" });
            outlineInstancedShader = new Shader(new List<string>() { "outlineInstanced.vert", "outlineInstanced.frag" });
            pickingShader = new Shader(new List<string>() { "picking.vert", "picking.frag" });
            pickingInstancedShader = new Shader(new List<string>() { "pickingInstanced.vert", "pickingInstanced.frag" });
            shaderProgram = new Shader(new List<string>() { "Default.vert", "Default.frag" });
            instancedShaderProgram = new Shader(new List<string>() { "Instanced.vert", "Instanced.frag" });
            posTexShader = new Shader(new List<string>() { "postex.vert", "postex.frag" });
            onlyPosShaderProgram = new Shader(new List<string>() { "onlyPos.vert", "onlyPos.frag" });
            aabbShaderProgram = new Shader(new List<string>() { "aabb.vert", "aabb.frag" });
            #endregion

            pickingTexture = new PickingTexture(windowSize);

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

            onlyPosShaderProgram.Use();
            Vector3 characterPos = new Vector3(0, 0, -5);
            character = new Character(new WireframeMesh(wireVao, wireVbo, onlyPosShaderProgram.id, ref camera), ref physx, characterPos, camera);
            character.camera.SetYaw(90f);
            character.camera.SetPitch(0f);

            editorData.gizmoManager = new GizmoManager(meshVao, meshVbo, shaderProgram, ref camera);

            //Point Lights
            //pointLights.Add(new PointLight(new Vector3(0, 5000, 0), Color4.White, meshVao.id, shaderProgram.id, ref frustum, ref camera, noTexVao, noTexVbo, noTextureShaderProgram.id, pointLights.Count));

            shaderProgram.Use();
            PointLight.SendToGPU(ref pointLights, shaderProgram.id, editorData.gameRunning);

            // Projection matrix and mesh loading

            //objects.Add(new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, "spiro.obj", "High.png", windowSize, ref frustum, ref camera, ref textureCount), ObjectType.TriangleMeshWithCollider, ref physx));
            //objects.Last().BuildBVH(shaderProgram, noTextureShaderProgram);

            //Object o = new Object(ObjectType.TriangleMeshWithCollider, ref physx);
            //o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "level2Rot.obj", "level.png", windowSize, ref camera, ref o));
            //objects.Add(o);

            //Object o2 = new Object(ObjectType.Cube, ref physx);
            //o2.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "level2Rot.obj", "level.png", windowSize, ref camera, ref o2));
            //objects.Add(o2);
            //_meshObjects.Add(o2);

            //Object o2 = new Object(ObjectType.Cube, ref physx);
            //o2.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "cube", Object.GetUnitCube(), "red_t.png", windowSize, ref camera, ref o2));
            //objects.Add(o2);
            //_meshObjects.Add(o2);

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

            Object o3 = new Object(ObjectType.TriangleMesh, ref physx);
            o3.AddMesh(new InstancedMesh(instancedMeshVao, instancedMeshVbo, instancedShaderProgram.id, "cube", Object.GetUnitCube(), windowSize, ref camera, ref o3));

            //for (int i = 0; i < 1; i++)
            //{
            //    InstancedMeshData instData = new InstancedMeshData();
            //    //instData.Position = Helper.GetRandomVectorInAABB(new AABB(new Vector3(-10, -10, -10), new Vector3(10, 10, 10)));
            //    instData.Position = new Vector3(0,0,10);
            //    //instData.Rotation = Helper.GetRandomQuaternion();
            //    instData.Rotation = Quaternion.Identity;
            //    //instData.Rotation = Helper.QuaternionFromEuler(new Vector3(45, 0, 0));
            //    //instData.Scale = Helper.GetRandomScale(new AABB(new Vector3(1, 1, 1), new Vector3(5, 5, 5)));
            //    //instData.Scale = new Vector3(3, 3, 3);
            //    instData.Scale = new Vector3(1, 1, 1);
            //    //instData.Color = Helper.GetRandomColor();
            //    instData.Color = Color4.Blue;

            //    ((InstancedMesh)o3.GetMesh()).instancedData.Add(instData);
            //}

            for (int i = 0; i < 5; i++)
            {
                InstancedMeshData instData = new InstancedMeshData();
                //instData.Position = new Vector3(0, 0, 0);
                instData.Position = Helper.GetRandomVectorInAABB(new AABB(new Vector3(-10, -10, -10), new Vector3(10, 10, 10)));
                instData.Rotation = Quaternion.Identity;
                //instData.Scale = Helper.GetRandomScale(new AABB(new Vector3(1, 1, 1), new Vector3(5, 5, 5)));
                instData.Scale = new Vector3(1, 1, 1);
                //instData.Color = Helper.GetRandomColor();
                instData.Color = Color4.Blue;

                ((InstancedMesh)o3.GetMesh()).instancedData.Add(instData);
            }
            objects.Add(o3);
            _instObjects.Add(o3);

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
            GL.DeleteVertexArray(uiTexVao.id);
            GL.DeleteVertexArray(wireVao.id);

            foreach(Object obj in objects)
            {
                BaseMesh mesh = obj.GetMesh();
                GL.DeleteBuffer(mesh.vbo);               
            }

            cullingProgram.Unload();
            outlineShader.Unload();
            pickingShader.Unload();
            shaderProgram.Unload();
            instancedShaderProgram.Unload();
            posTexShader.Unload();
            onlyPosShaderProgram.Unload();
            aabbShaderProgram.Unload();

            FileManager.DisposeStreams();
            textureManager.DeleteTextures();
            editorData.assetStoreManager.DeleteFolderContent("Temp");
            editorData.assetStoreManager.Delete();
        }

        private bool IsMouseInGameWindow(MouseState mouseState)
        {
            float mouseY = windowSize.Y - mouseState.Y;

            bool isInsideHorizontally = mouseState.X >= editorData.gameWindow.gameWindowPos.X && mouseState.X <= (editorData.gameWindow.gameWindowPos.X + editorData.gameWindow.gameWindowSize.X);
            bool isInsideVertically = mouseY >= editorData.gameWindow.gameWindowPos.Y && mouseY <= (editorData.gameWindow.gameWindowPos.Y + editorData.gameWindow.gameWindowSize.Y);

            return isInsideHorizontally && isInsideVertically;
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

            editorData.gameWindow.gameWindowSize = new Vector2(windowSize.X * (1.0f - (editorData.gameWindow.leftPanelPercent + editorData.gameWindow.rightPanelPercent)),
                                                            windowSize.Y * (1 - editorData.gameWindow.bottomPanelPercent) - editorData.gameWindow.topPanelSize - editorData.gameWindow.bottomPanelSize);
            editorData.gameWindow.gameWindowPos = new Vector2(windowSize.X * editorData.gameWindow.leftPanelPercent, windowSize.Y * editorData.gameWindow.bottomPanelPercent + editorData.gameWindow.bottomPanelSize);

            if(character != null && character.camera != null)
                character.camera.SetScreenSize(windowSize, editorData.gameWindow.gameWindowSize, editorData.gameWindow.gameWindowPos);

            if (imGuiController != null)
                imGuiController.WindowResized(ClientSize.X, ClientSize.Y);
        }

        private void ResizedEditorWindow()
        {
            if (editorData.isGameFullscreen)
            {
                editorData.gameWindow.gameWindowSize = new Vector2(windowSize.X, windowSize.Y);
                editorData.gameWindow.gameWindowPos = new Vector2(0, 0);
            }
            else
            {
                editorData.gameWindow.gameWindowSize = new Vector2(windowSize.X * (1.0f - (editorData.gameWindow.leftPanelPercent + editorData.gameWindow.rightPanelPercent)),
                                                                windowSize.Y * (1 - editorData.gameWindow.bottomPanelPercent) - editorData.gameWindow.topPanelSize - editorData.gameWindow.bottomPanelSize);
                editorData.gameWindow.gameWindowPos = new Vector2(windowSize.X * editorData.gameWindow.leftPanelPercent, windowSize.Y * editorData.gameWindow.bottomPanelPercent + editorData.gameWindow.bottomPanelSize);
            }

            character.camera.SetScreenSize(windowSize, editorData.gameWindow.gameWindowSize, editorData.gameWindow.gameWindowPos);
        }
    }
}
