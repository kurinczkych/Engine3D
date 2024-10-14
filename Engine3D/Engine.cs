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
using MagicPhysX;
using Assimp;
using System.Drawing;
using System.ComponentModel.DataAnnotations;

#pragma warning disable CS0649
#pragma warning disable CS8618

namespace Engine3D
{
    public enum GameState
    {
        Running,
        Stopped
    }

    public partial class Engine : GameWindow
    {

        #region OPENGL
        private int framebuffer = -1;
        private int textureColorBuffer = -1;
        private int depthRenderbuffer = -1;

        public static GLState GLState = new GLState();

        //private IndirectBuffer indirectBuffer;
        //private VBO visibilityVbo;
        //private VBO drawCommandsVbo;
        //private VBO frustumVbo;

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

        private VAO meshAnimVao;
        private VBO meshAnimVbo;
        private IBO meshAnimIbo;

        public InstancedVAO instancedMeshVao;
        public VBO instancedMeshVbo;

        private VAO textVao;
        private VBO textVbo;

        private VAO uiTexVao;
        private VBO uiTexVbo;

        private VAO wireVao;
        private VBO wireVbo;

        private VAO aabbVao;
        private VBO aabbVbo;

        private VAO infiniteFloorVao;
        private VBO infiniteFloorVbo;

        private Shader cullingProgram;
        private Shader outlineShader;
        private Shader outlineInstancedShader;
        private Shader pickingShader;
        private Shader pickingInstancedShader;
        public Shader shaderProgram;
        private Shader shaderAnimProgram;
        public Shader instancedShaderProgram;
        private Shader posTexShader;
        private Shader onlyPosShaderProgram;
        private Shader aabbShaderProgram;
        private Shader infiniteFloorShader;
        #endregion

        #region Program variables
        public static Random rnd = new Random((int)DateTime.Now.Ticks);
        private bool haveText = false;
        private bool firstRun = true;

        private Vector2 origWindowSize;
        public Vector2 windowSize;
        public GameWindowProperty gameWindowProperty;
        //private Vector2 gameWindowMousePos;

        private SoundManager soundManager;
        private AssetManager assetManager;
        private Stopwatch fileDetectorStopWatch;
        public static TextureManager textureManager;
        public static ConsoleManager consoleManager = new ConsoleManager();
        public static AssimpManager assimpManager = new AssimpManager();
        private GizmoManager gizmoManager;

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
        private GameState gameState = GameState.Stopped;
        private bool runParticles = false;
        private Vector2 gizmoWindowPos = new Vector2();
        private Vector2 gizmoWindowSize = new Vector2();
        private bool UIHasMouse = false;
        #endregion

        #region Engine variables
        public FPS fps = new FPS();

        public delegate void RenderDelegate(FrameEventArgs args);
        public delegate void UpdateDelegate(FrameEventArgs args);
        public delegate void UnloadDelegate();
        public delegate void OnLoadDelegate();
        public delegate void WindowResizedDelegate(ResizeEventArgs e);
        public delegate void ObjectSelectedDelegate(Object? o, int inst);
        public delegate void CharInputDelegate(TextInputEventArgs e);
        public delegate void MouseWheelInputDelegate(MouseWheelEventArgs e);

        private List<RenderDelegate> renderMethods = new List<RenderDelegate>();
        private List<UpdateDelegate> updateMethods = new List<UpdateDelegate>();
        private List<UnloadDelegate> unloadMethods = new List<UnloadDelegate>();
        private List<OnLoadDelegate> onLoadMethods = new List<OnLoadDelegate>();
        private List<CharInputDelegate> charInputMethods = new List<CharInputDelegate>();
        private List<MouseWheelInputDelegate> mouseWheelInputMethods = new List<MouseWheelInputDelegate>();
        private WindowResizedDelegate? windowResized;
        private ObjectSelectedDelegate? objectSelected;

        public Object? selectedObject;
        public static int objectID = 1;

        public List<Object> objects = new List<Object>();
        public List<Object> _meshObjects = new List<Object>();
        public List<Object> _instObjects = new List<Object>();

        public Character character;
        public Camera mainCamera;
        private List<float> vertices = new List<float>();
        private List<uint> indices = new List<uint>();
        private List<float> verticesUnique = new List<float>();

        private List<Light>? lights_;
        public List<Light> lights
        {
            get
            {
                if(lights_ == null)
                {
                    lights_ = new List<Light>();
                    lights_ = objects
                        .SelectMany(o => o.components.OfType<Light>())
                        .ToList();

                    if (lights_.Count > 0)
                        SetBackgroundColor(true);
                    else
                        SetBackgroundColor(false);
                }
                return lights_??new List<Light>();
            }
            set
            {
                lights_ = null;
            }
        }

        private List<ParticleSystem>? particleSystems_ = null;
        public List<ParticleSystem> particleSystems
        {
            get
            {
                if (particleSystems_ == null)
                {
                    particleSystems_ = new List<ParticleSystem>();
                    particleSystems_ = objects
                        .SelectMany(o => o.components.OfType<ParticleSystem>())
                        .ToList();
                }
                return particleSystems_ ?? new List<ParticleSystem>();
            }
            set
            {
                particleSystems_ = null;
            }
        }

        private Physx physx;

        private bool useOcclusionCulling = false;
        private QueryPool queryPool;
        private Dictionary<int, Tuple<int, BVHNode>> pendingQueries;

        private Color4 backgroundColor = Color4.Black;
        private Color4 gridColor = Color4.White;
        #endregion

        public Engine(int width, int height) : base(GameWindowSettings.Default, new NativeWindowSettings() { StencilBits = 8, DepthBits = 32 })
        {
            Title = "3D Engine";

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            windowSize = new Vector2(width, height);
            origWindowSize = new Vector2(width, height);

            gameWindowProperty = new GameWindowProperty();
            gameWindowProperty.gameWindowSize = new Vector2(width, height);
            CenterWindow(new Vector2i(width, height));

            shaderProgram = new Shader();
            posTexShader = new Shader();
            texts = new Dictionary<string, TextMesh>();
            textureManager = new TextureManager();
            fileDetectorStopWatch = new Stopwatch();

            assetManager = new AssetManager(ref textureManager);

            queryPool = new QueryPool(1000);
            pendingQueries = new Dictionary<int, Tuple<int, BVHNode>>();

            SetBackgroundColor(true);
        }

        private void SetBackgroundColor(bool light)
        {
            float t = 0.4f;
            if (light)
            {
                backgroundColor = Color4.Cyan;
                t = 0.6f;
            }
            else
                backgroundColor = new Color4(0.118f, 0.118f, 0.118f, 1.0f);

            float r = backgroundColor.R + (Color4.White.R - backgroundColor.R) * t;
            float g = backgroundColor.G + (Color4.White.G - backgroundColor.G) * t;
            float b = backgroundColor.B + (Color4.White.B - backgroundColor.B) * t;
            gridColor = new Color4(r, g, b, 1.0f);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            #region Fullscreen scissoring
            //if (!editorData.isGameFullscreen)
            //{
            //    GL.Viewport((int)editorData.gameWindow.gameWindowPos.X, (int)editorData.gameWindow.gameWindowPos.Y,
            //                (int)editorData.gameWindow.gameWindowSize.X, (int)editorData.gameWindow.gameWindowSize.Y);
            //    GL.Enable(EnableCap.ScissorTest);
            //    GL.Scissor((int)editorData.gameWindow.gameWindowPos.X, (int)editorData.gameWindow.gameWindowPos.Y,
            //               (int)editorData.gameWindow.gameWindowSize.X, (int)editorData.gameWindow.gameWindowSize.Y);
            //}
            #endregion

            vertices.Clear();

            FrustumCalculating();

            OcclusionCuller();

            ObjectAndAxisPicking();

            GL.Viewport(0, 0, (int)gameWindowProperty.gameWindowSize.X, (int)gameWindowProperty.gameWindowSize.Y);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);

            GL.ClearColor(backgroundColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            RenderInfiniteFloor();

            shaderProgram.Use();
            Light.SendToGPU(lights, shaderProgram.id);

            DrawObjects(args.Time);

            DrawMoverGizmo();

            TextUpdating();

            instancedShaderProgram.Use();
            Light.SendToGPU(lights, instancedShaderProgram.id);
            DrawParticleSystems();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, (int)windowSize.X, (int)windowSize.Y);

            #region Fullscreen scissoring
            //if (!editorData.isGameFullscreen)
            //{
            //    GL.Disable(EnableCap.ScissorTest);
            //    GL.Viewport(0, 0, (int)windowSize.X, (int)windowSize.Y);
            //}
            #endregion

            foreach (var renderMethod in renderMethods)
            {
                renderMethod.Invoke(args);
            }

            Context.SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            foreach (var updateMethod in updateMethods)
            {
                updateMethod.Invoke(args);
            }

            fps.Update((float)args.Time);

            assetManager.UpdateIfNeeded();

            if (fileDetectorStopWatch.Elapsed.TotalSeconds > 5)
            {
                fileDetectorStopWatch.Restart();
                FileManager.GetAllAssets(ref assetManager);
            }

            MouseMoving();

            if (gameState == GameState.Running)
            {
                character.CalculateVelocity(KeyboardState, MouseState, args);
                character.UpdatePosition(KeyboardState, MouseState, args);

                soundManager.SetListener(mainCamera.GetPosition());
            }
            else
            {
                EditorMoving(args);
            }

            
            if(gameState == GameState.Running || runParticles) 
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    ps.Update((float)args.Time);
                }
            }

            character.AfterUpdate(MouseState, args, gameState);

            if (fps.totalTime > 0 && gameState == GameState.Running)
            {
                physx.Simulate((float)args.Time);
                foreach (Object o in objects)
                {
                    if (o.GetComponent<Physics>() is Physics p)
                    {
                        var trans = p.CollisionResponse();
                        if (trans != null)
                        {
                            BaseMesh? pmesh = (BaseMesh?)o.GetComponent<BaseMesh>();
                            if (pmesh == null)
                                continue;

                            bool[] which = new bool[3] { false, false, false };

                            o.transformation.Position = trans.Item1;
                            if (o.transformation.Position != o.transformation.LastPosition)
                            {
                                pmesh.recalculate = true;
                                which[0] = true;
                            }

                            o.transformation.Rotation = trans.Item2;
                            if(o.transformation.Rotation != o.transformation.LastRotation)
                            {
                                pmesh.recalculate = true;
                                which[1] = true;
                            }

                            pmesh.RecalculateModelMatrix(which);
                        }
                    }
                }
            }
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            CursorState = CursorState.Normal; 

            textGenerator = new TextGenerator();

            #region Editor data
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

            FileManager.GetAllAssets(ref assetManager);
            fileDetectorStopWatch.Start();
            #endregion

            InitFramebuffer(gameWindowProperty.gameWindowSize);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            #region VBO and VAO Init
            //indirectBuffer = new IndirectBuffer();
            //visibilityVbo = new VisibilityVBO(DynamicCopy: true);
            //frustumVbo = new VBO(DynamicCopy: true);
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

            meshAnimIbo = new IBO();
            meshAnimVbo = new VBO();
            meshAnimVao = new VAO(Mesh.floatAnimCount);
            meshAnimVao.LinkToVAO(0, 3, meshAnimVbo);
            meshAnimVao.LinkToVAO(1, 3, meshAnimVbo);
            meshAnimVao.LinkToVAO(2, 2, meshAnimVbo);
            meshAnimVao.LinkToVAO(3, 4, meshAnimVbo);
            meshAnimVao.LinkToVAO(4, 3, meshAnimVbo);
            meshAnimVao.LinkToVAO(5, 4, meshAnimVbo);
            meshAnimVao.LinkToVAO(6, 4, meshAnimVbo);
            meshAnimVao.LinkToVAO(7, 1, meshAnimVbo);

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

            infiniteFloorVbo = new VBO();
            infiniteFloorVao = new VAO(3);
            infiniteFloorVao.LinkToVAO(0, 3, infiniteFloorVbo);

            #endregion

            #region Shader Init

            // Create the shader program
            cullingProgram = new Shader(new List<string>() { "cullingshader.comp" });
            outlineShader = new Shader(new List<string>() { "outline.vert", "outline.frag" });
            outlineInstancedShader = new Shader(new List<string>() { "outlineInstanced.vert", "outlineInstanced.frag" });
            pickingShader = new Shader(new List<string>() { "picking.vert", "picking.frag" });
            pickingInstancedShader = new Shader(new List<string>() { "pickingInstanced.vert", "pickingInstanced.frag" });
            shaderProgram = new Shader(new List<string>() { "Default.vert", "Default.frag" });
            shaderAnimProgram = new Shader(new List<string>() { "DefaultWithBone.vert", "Default.frag" });
            instancedShaderProgram = new Shader(new List<string>() { "Instanced.vert", "Instanced.frag" });
            posTexShader = new Shader(new List<string>() { "postex.vert", "postex.frag" });
            onlyPosShaderProgram = new Shader(new List<string>() { "onlyPos.vert", "onlyPos.frag" });
            aabbShaderProgram = new Shader(new List<string>() { "aabb.vert", "aabb.frag" });
            infiniteFloorShader = new Shader(new List<string>() { "infiniteFloor.vert", "infiniteFloor.frag" });
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
            Object camObj = new Object(ObjectType.Empty) { name = "MainCamera" };
            camObj.components.Add(new Camera(windowSize, camObj));
            mainCamera = (Camera)camObj.components[camObj.components.Count-1];
            mainCamera.SetYaw(358);
            mainCamera.SetPitch(-4.23f);
            objects.Add(camObj);

            onlyPosShaderProgram.Use();

            Vector3 characterPos = new Vector3(-5, 10, 0);
            character = new Character(new WireframeMesh(wireVao, wireVbo, onlyPosShaderProgram.id, ref mainCamera), ref physx, characterPos, ref mainCamera);

            gizmoManager = new GizmoManager(meshVao, meshVbo, shaderProgram, ref mainCamera);

            //Point Lights
            //objects.Add(new PointLight(Color4.White, shaderProgram.id, pointLights.Count));
            //pointLights[0].transformation.Position = new Vector3(0, 110, 0);

            shaderProgram.Use();
            objects.Add(new Object(ObjectType.Empty) { name = "Light" });
            objects[objects.Count - 1].components.Add(new Light(objects[objects.Count - 1], shaderProgram.id, 0));
            objects[objects.Count - 1].transformation.Position = new Vector3(0, 10, 0);
            Light.SendToGPU(lights, shaderProgram.id);

            #region DebugLines
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

            //Object o2 = new Object(ObjectType.Cube);
            //o2.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "rotating.fbx", windowSize, ref camera, ref o2));
            //o2.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "rotatingSingle.fbx", windowSize, ref camera, ref o2));
            //o2.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "bob.fbx", windowSize, ref camera, ref o2)); o2.Rotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(270), 0, MathHelper.DegreesToRadians(270)); o2.Position = new Vector3(0, -3, 0); o2.GetMesh().RecalculateModelMatrix(new bool[3] { true, true, false });

            // TODO if animations
            //o2.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "roblox4.fbx", windowSize, ref camera, ref o2));
            //((Mesh)o2.GetMesh()).animation = assimpManager.animations.First().Value;
            //((Mesh)o2.GetMesh()).GetUniformLocationsAnim(shaderAnimProgram);
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

            //////Object o3 = new Object(ObjectType.TriangleMesh, ref physx);
            //////o3.AddMesh(new InstancedMesh(instancedMeshVao, instancedMeshVbo, instancedShaderProgram.id, "cube", Object.GetUnitCube(), windowSize, ref camera, ref o3));

            ////////for (int i = 0; i < 1; i++)
            ////////{
            ////////    InstancedMeshData instData = new InstancedMeshData();
            ////////    //instData.Position = Helper.GetRandomVectorInAABB(new AABB(new Vector3(-10, -10, -10), new Vector3(10, 10, 10)));
            ////////    instData.Position = new Vector3(0,0,10);
            ////////    //instData.Rotation = Helper.GetRandomQuaternion();
            ////////    instData.Rotation = Quaternion.Identity;
            ////////    //instData.Rotation = Helper.QuaternionFromEuler(new Vector3(45, 0, 0));
            ////////    //instData.Scale = Helper.GetRandomScale(new AABB(new Vector3(1, 1, 1), new Vector3(5, 5, 5)));
            ////////    //instData.Scale = new Vector3(3, 3, 3);
            ////////    instData.Scale = new Vector3(1, 1, 1);
            ////////    //instData.Color = Helper.GetRandomColor();
            ////////    instData.Color = Color4.Blue;

            ////////    ((InstancedMesh)o3.GetMesh()).instancedData.Add(instData);
            ////////}

            //////for (int i = 0; i < 5; i++)
            //////{
            //////    InstancedMeshData instData = new InstancedMeshData();
            //////    //instData.Position = new Vector3(0, 0, 0);
            //////    instData.Position = Helper.GetRandomVectorInAABB(new AABB(new Vector3(-10, -10, -10), new Vector3(10, 10, 10)));
            //////    instData.Rotation = Quaternion.Identity;
            //////    //instData.Scale = Helper.GetRandomScale(new AABB(new Vector3(1, 1, 1), new Vector3(5, 5, 5)));
            //////    instData.Scale = new Vector3(1, 1, 1);
            //////    //instData.Color = Helper.GetRandomColor();
            //////    instData.Color = Color4.Blue;

            //////    ((InstancedMesh)o3.GetMesh()).instancedData.Add(instData);
            //////}
            //////objects.Add(o3);
            //////_instObjects.Add(o3);

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
            #endregion

            // We have text on screen
            if (haveText)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }

            objects.Sort();

            foreach (var onLoadMethod in onLoadMethods)
            {
                onLoadMethod.Invoke();
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            foreach (var method in charInputMethods)
                method.Invoke(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            foreach (var method in mouseWheelInputMethods)
                method.Invoke(e);
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
                BaseMesh? mesh = (BaseMesh?)obj.GetComponent<BaseMesh>();
                if (mesh == null)
                    continue;

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

            foreach (var unloadMethod in unloadMethods)
            {
                unloadMethod.Invoke();
            }
        }

        private bool IsMouseInGameWindow(MouseState mouseState)
        {
            float mouseY = windowSize.Y - mouseState.Y;

            bool isInsideHorizontally = mouseState.X >= gameWindowProperty.gameWindowPos.X && mouseState.X <= (gameWindowProperty.gameWindowPos.X + gameWindowProperty.gameWindowSize.X);
            bool isInsideVertically = mouseY >= gameWindowProperty.gameWindowPos.Y && mouseY <= (gameWindowProperty.gameWindowPos.Y + gameWindowProperty.gameWindowSize.Y);

            return isInsideHorizontally && isInsideVertically;
        }

        private void InitFramebuffer(Vector2 viewportSize)
        {
            framebuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);

            // Create a texture to render to
            textureColorBuffer = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureColorBuffer);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)viewportSize.X, (int)viewportSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            // Set texture filtering
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Attach the texture to the framebuffer as a color attachment
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textureColorBuffer, 0);

            // Create a renderbuffer object for depth and stencil (if needed)
            depthRenderbuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthRenderbuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, (int)viewportSize.X, (int)viewportSize.Y);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, depthRenderbuffer);

            // Check if framebuffer is complete
            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception($"Framebuffer is incomplete: {status}");
            }

            // Unbind the framebuffer to avoid accidental rendering to it
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void ResizeFramebuffer(Vector2 newViewPortSize)
        {
            if (framebuffer == -1)
                return;

            GL.BindTexture(TextureTarget.Texture2D, textureColorBuffer);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)newViewPortSize.X, (int)newViewPortSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthRenderbuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, (int)newViewPortSize.X, (int)newViewPortSize.Y);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception($"Framebuffer is incomplete: {status}");
            }
        }

        public int GetGameViewportTexture()
        {
            return textureColorBuffer;
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

            if(windowResized != null)
            {
                windowResized.Invoke(e);
            }
        }
    }
}
