using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

namespace Engine3D
{
    public class FPS
    {
        public int fps;

        public int minFps = int.MaxValue;
        public int maxFps = 0;
        public double totalTime;

        private const int SAMPLE_SIZE = 30;
        private Queue<double> sampleTimes = new Queue<double>(SAMPLE_SIZE);

        private Stopwatch maxminStopwatch;

        private bool limitFps = false;
        private const double TargetDeltaTime = 1.0 / 60.0; // for 60 FPS
        private Stopwatch stopwatch;

        public const int fpsLength = 5;

        public FPS()
        {
            maxminStopwatch = new Stopwatch();
            maxminStopwatch.Start();
        }

        public void Update(float delta)
        {
            if (sampleTimes.Count >= SAMPLE_SIZE)
            {
                totalTime -= sampleTimes.Dequeue();
            }

            sampleTimes.Enqueue(delta);
            totalTime += delta;

            double averageDeltaTime = totalTime / sampleTimes.Count;
            double fps = 1.0 / averageDeltaTime;
            this.fps = (int)fps;

            if (!maxminStopwatch.IsRunning)
            {
                if (fps > maxFps)
                    maxFps = (int)fps;
                if (fps < minFps)
                    minFps = (int)fps;
            }
            else
            {
                if (maxminStopwatch.ElapsedMilliseconds > 3000)
                    maxminStopwatch.Stop();
            }
        }

        public string GetFpsString()
        {
            string fpsStr = fps.ToString();
            string maxFpsStr = maxFps.ToString();
            string minFpsStr = minFps.ToString();
            if(fpsStr.Length < fpsLength)
            {
                for (int i = 0; i < (fpsLength + 1 - fpsStr.Length); i++)
                    fpsStr = " " + fpsStr;
            }
            if(maxFpsStr.Length < fpsLength)
            {
                for (int i = 0; i < (fpsLength + 1 - maxFpsStr.Length); i++)
                    maxFpsStr = " " + maxFpsStr;
            }
            if(minFpsStr.Length < fpsLength)
            {
                for (int i = 0; i < (fpsLength + 1 - minFpsStr.Length); i++)
                    minFpsStr = " " + minFpsStr;
            }

            string fpsFullStr = "FPS: " + fpsStr + "    |    MaxFPS: " + maxFpsStr + "    |    MinFPS: " + minFpsStr;

            return fpsFullStr;
        }
    }

    public enum GameState
    {
        Running,
        Stopped
    }

    public class EditorData
    {
        #region Data
        public List<Object> objects;
        public List<ParticleSystem> particleSystems;
        public List<PointLight> pointLights;

        public FPS fps = new FPS();

        public object? selectedItem;

        public List<Asset> assets = new List<Asset>();

        public int currentAssetTexture = 0;
        public List<Asset> AssetTextures;

        public GizmoManager gizmoManager;
        #endregion

        #region Properties 
        public bool windowResized = false;
        public MouseCursor mouseType = MouseCursor.Default;

        public bool manualCursor = false;
        public bool isGameFullscreen = false;
        public bool justSetGameState = false;
        public bool isPaused = false;
        public GameState prevGameState;
        private GameState _gameRunning;
        public GameState gameRunning
        {
            get
            {
                return _gameRunning;
            }
            set
            {
                prevGameState = _gameRunning;
                _gameRunning = value;
            }
        }
        #endregion

        public EditorData()
        {
            _gameRunning = GameState.Stopped;
        }
    }

    public class ImGuiController : BaseImGuiController
    {
        private EditorData editorData;

        private Dictionary<string, byte[]> _inputBuffers = new Dictionary<string, byte[]>();

        private bool isResizingLeft = false;
        private bool isResizingRight = false;
        private bool isResizingBottom = false;
        private float seperatorSize = 5;
        private System.Numerics.Vector4 baseBGColor = new System.Numerics.Vector4(0.352f, 0.352f, 0.352f, 1.0f);
        private System.Numerics.Vector4 seperatorColor = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);

        public ImGuiController(int width, int height, ref EditorData editorData) : base(width, height)
        {
            this.editorData = editorData;

            #region Input boxes
            _inputBuffers.Add("##name", new byte[100]);

            _inputBuffers.Add("##positionX", new byte[100]);
            _inputBuffers.Add("##positionY", new byte[100]);
            _inputBuffers.Add("##positionZ", new byte[100]);
            _inputBuffers.Add("##rotationX", new byte[100]);
            _inputBuffers.Add("##rotationY", new byte[100]);
            _inputBuffers.Add("##rotationZ", new byte[100]);
            _inputBuffers.Add("##scaleX", new byte[100]);
            _inputBuffers.Add("##scaleY", new byte[100]);
            _inputBuffers.Add("##scaleZ", new byte[100]);
            _inputBuffers.Add("##texturePath", new byte[100]);
            _inputBuffers.Add("##textureNormalPath", new byte[100]);
            _inputBuffers.Add("##textureHeightPath", new byte[100]);
            _inputBuffers.Add("##textureAOPath", new byte[100]);
            _inputBuffers.Add("##textureRoughPath", new byte[100]);
            _inputBuffers.Add("##textureMetalPath", new byte[100]);
            _inputBuffers.Add("##meshPath", new byte[100]);

            #endregion
        }

        private string GetStringFromBuffer(string bufferName)
        {
            string s = Encoding.UTF8.GetString(_inputBuffers[bufferName]).TrimEnd('\0');
            int nullIndex = s.IndexOf('\0');
            if (nullIndex >= 0)
            {
                return s.Substring(0, nullIndex);
            }
            return s;
        }

        private string GetStringFromByte(byte[] array)
        {
            string s = Encoding.UTF8.GetString(array).TrimEnd('\0');
            int nullIndex = s.IndexOf('\0');
            if (nullIndex >= 0)
            {
                return s.Substring(0, nullIndex);
            }
            return s;
        }

        private void CopyDataToBuffer(string buffer, byte[] data)
        {
            for (int i = 0; i < _inputBuffers[buffer].Length; i++)
            {
                if(i < data.Length)
                {
                    _inputBuffers[buffer][i] = data[i];
                }
                else
                {
                    if (_inputBuffers[buffer][i] == '\0')
                    {
                        break;
                    }
                    else
                    {
                        _inputBuffers[buffer][i] = 0;
                        break;
                    }
                }
            }
        }

        public void ClearBuffers()
        {
            foreach(var buffer in _inputBuffers)
            {
                int i = 0;
                while (buffer.Value[i] != '\0' && buffer.Value.Length != i)
                {
                    buffer.Value[i] = 0;
                    i++;
                }
            }
            ;
        }

        public void SelectItem(object? selectedObject, EditorData editorData)
        {
            if (editorData.selectedItem != null)
            {
                if (editorData.selectedItem is Object o)
                {
                    o.isSelected = false;
                }
                else if (editorData.selectedItem is ParticleSystem p)
                    p.isSelected = false;
                else if (editorData.selectedItem is PointLight pl)
                    pl.isSelected = false;
            }

            ClearBuffers();

            if (selectedObject == null)
            {
                editorData.selectedItem = null;
                return;
            }

            editorData.selectedItem = selectedObject;
            ((ISelectable)selectedObject).isSelected = true;

            if (selectedObject is Object castedO)
            {
                castedO.GetMesh().recalculate = true;
                castedO.GetMesh().RecalculateModelMatrix(new bool[] { true, true, true });
                castedO.UpdatePhysxPositionAndRotation();
            }
            //TODO pointlight and particle system selection
        }

        public void EditorWindow(Engine.GameWindowProperty gameWindow, ref EditorData editorData, KeyboardState keyboardState)
        {
            var io = ImGui.GetIO();

            if (editorData.gameRunning == GameState.Running && !editorData.manualCursor)
                io.ConfigFlags |= ImGuiConfigFlags.NoMouse;
            else
                io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;

            editorData.windowResized = false;
            bool[] mouseTypes = new bool[3];

            var style = ImGui.GetStyle();
            style.WindowBorderSize = 0.5f;
            style.Colors[(int)ImGuiCol.WindowBg] = baseBGColor;
            style.Colors[(int)ImGuiCol.Border] = new System.Numerics.Vector4(0, 0, 0, 1.0f);
            style.Colors[(int)ImGuiCol.Tab] = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            style.Colors[(int)ImGuiCol.TabActive] = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            style.Colors[(int)ImGuiCol.TabHovered] = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonHovered] = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonActive] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            style.Colors[(int)ImGuiCol.CheckMark] = new System.Numerics.Vector4(1f, 1f, 1f, 1.0f);
            style.Colors[(int)ImGuiCol.FrameBg] = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            style.WindowRounding = 5f;

            #region Top panel with menubar
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, gameWindow.topPanelSize));
            ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero, ImGuiCond.Always);
            if (ImGui.Begin("TopPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.MenuBar |
                                        ImGuiWindowFlags.NoScrollbar))
            {
                if(ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("File"))
                    {
                        if(ImGui.MenuItem("Open", "Ctrl+O"))
                        {

                        }
                        ImGui.EndMenu();
                    }
                    if(ImGui.BeginMenu("Window"))
                    {
                        if(ImGui.MenuItem("Reset panels"))
                        {
                            gameWindow.leftPanelPercent = 0.15f;
                            gameWindow.rightPanelPercent = 0.15f;
                            gameWindow.bottomPanelPercent = 0.15f;
                            editorData.windowResized = true;
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenuBar();
                }

                var button = style.Colors[(int)ImGuiCol.Button];
                style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                float totalWidth = 40;
                if (editorData.gameRunning == GameState.Running)
                    totalWidth = 60;

                float startX = (ImGui.GetWindowSize().X - totalWidth) * 0.5f;
                float startY = (ImGui.GetWindowSize().Y - 10) * 0.5f;
                ImGui.SetCursorPos(new System.Numerics.Vector2(startX, startY));

                if (editorData.gameRunning == GameState.Stopped)
                {
                    if (ImGui.ImageButton("play", (IntPtr)Engine.textureManager.textures["ui_play.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Running;
                        editorData.justSetGameState = true;
                    }
                }
                else
                {
                    if (ImGui.ImageButton("stop", (IntPtr)Engine.textureManager.textures["ui_stop.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Stopped;
                        editorData.justSetGameState = true;
                    }
                }

                ImGui.SameLine();
                if (editorData.gameRunning == GameState.Running)
                {
                    if (ImGui.ImageButton("pause", (IntPtr)Engine.textureManager.textures["ui_pause.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.isPaused = !editorData.isPaused;
                    }
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("screen", (IntPtr)Engine.textureManager.textures["ui_screen.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                {
                    editorData.isGameFullscreen = !editorData.isGameFullscreen;
                }


                style.Colors[(int)ImGuiCol.Button] = button;
            }
            ImGui.End();
            #endregion

            #region Left panel
            List<Object> meshes = editorData.objects.Where(x => x.meshType == typeof(Mesh)).ToList();
            List<Object> instMeshes = editorData.objects.Where(x => x.meshType == typeof(InstancedMesh)).ToList();
            List<ParticleSystem> particleSystems = editorData.particleSystems;
            List<PointLight> pointLights = editorData.pointLights;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth * gameWindow.leftPanelPercent, 
                                                                _windowHeight - gameWindow.topPanelSize - gameWindow.bottomPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("LeftPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if (ImGui.BeginTabBar("MyTabs"))
                {
                    if (ImGui.BeginTabItem("Objects"))
                    {
                        if (meshes.Count > 0)
                        {
                            if (ImGui.TreeNode("Meshes"))
                            {
                                for (int i = 0; i < meshes.Count; i++)
                                {
                                    string name = meshes[i].name == "" ? "Object " + i.ToString() : meshes[i].name;
                                    if (ImGui.Selectable(name))
                                    {
                                        SelectItem(meshes[i], editorData);
                                    }
                                }

                                ImGui.TreePop();
                            }
                        }

                        if (instMeshes.Count > 0)
                        {
                            if (ImGui.TreeNode("Instanced meshes"))
                            {
                                for (int i = 0; i < instMeshes.Count; i++)
                                {
                                    string name = instMeshes[i].name == "" ? "Object " + i.ToString() : instMeshes[i].name;
                                    if (ImGui.Selectable(name))
                                    {
                                        SelectItem(instMeshes[i], editorData);
                                    }
                                }

                                ImGui.TreePop();
                            }
                        }

                        if (particleSystems.Count > 0)
                        {
                            if (ImGui.TreeNode("Particle systems"))
                            {
                                for (int i = 0; i < particleSystems.Count; i++)
                                {
                                    string name = particleSystems[i].name == "" ? "Particle system " + i.ToString() : particleSystems[i].name;
                                    if (ImGui.Selectable(name))
                                    {
                                        SelectItem(particleSystems[i], editorData);
                                    }
                                }

                                ImGui.TreePop();
                            }
                        }

                        if (editorData.pointLights.Count > 0)
                        {
                            if (ImGui.TreeNode("Point lights"))
                            {
                                for (int i = 0; i < pointLights.Count; i++)
                                {
                                    string name = pointLights[i].name == "" ? "Point light " + i.ToString() : pointLights[i].name;
                                    if (ImGui.Selectable(name))
                                    {
                                        SelectItem(pointLights[i], editorData);
                                    }
                                }

                                ImGui.TreePop();
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
            ImGui.PopStyleVar();
            ImGui.End();
            #endregion

            #region Left panel seperator
            style.WindowMinSize = new System.Numerics.Vector2(seperatorSize, seperatorSize);
            style.Colors[(int)ImGuiCol.WindowBg] = seperatorColor;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(seperatorSize, 
                _windowHeight - gameWindow.topPanelSize - gameWindow.bottomPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth * gameWindow.leftPanelPercent, gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
            if (ImGui.Begin("LeftSeparator", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings |
                                         ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.InvisibleButton("##LeftSeparatorButton", new System.Numerics.Vector2(seperatorSize, _windowHeight));

                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        isResizingLeft = true;

                    mouseTypes[0] = true;
                }
                else
                    mouseTypes[0] = false;

                if (isResizingLeft)
                {
                    mouseTypes[0] = true;

                    float mouseX = ImGui.GetIO().MousePos.X;
                    gameWindow.leftPanelPercent = mouseX / _windowWidth;
                    if (gameWindow.leftPanelPercent + gameWindow.rightPanelPercent > 0.75)
                    {
                        gameWindow.leftPanelPercent = 1 - gameWindow.rightPanelPercent - 0.25f;
                    }
                    else
                    {
                        if (gameWindow.leftPanelPercent < 0.05f)
                            gameWindow.leftPanelPercent = 0.05f;
                        if (gameWindow.leftPanelPercent > 0.75f)
                            gameWindow.leftPanelPercent = 0.75f;
                    }

                    editorData.windowResized = true;

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        isResizingLeft = false;
                    }
                }
            }
            ImGui.End();
            ImGui.PopStyleVar(); // Pop the style for padding
            style.WindowMinSize = new System.Numerics.Vector2(32, 32);
            style.Colors[(int)ImGuiCol.WindowBg] = baseBGColor;
            #endregion

            #region Right panel

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth * gameWindow.rightPanelPercent - seperatorSize,
                                                                _windowHeight - gameWindow.topPanelSize - gameWindow.bottomPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(seperatorSize + _windowWidth * (1 - gameWindow.rightPanelPercent), gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("RightPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if (ImGui.BeginTabBar("MyTabs"))
                {
                    if (ImGui.BeginTabItem("Inspector"))
                    {
                        if(editorData.selectedItem != null)
                        {
                            if(editorData.selectedItem is Object o && o.meshType == typeof(Mesh))
                            {
                                if (ImGui.Checkbox("##isMeshEnabled", ref o.isEnabled))
                                {
                                    o.GetMesh().recalculate = true;
                                }

                                ImGui.SameLine();

                                Encoding.UTF8.GetBytes(o.name, 0, o.name.Length, _inputBuffers["##name"], 0);
                                if (ImGui.InputText("##name", _inputBuffers["##name"], (uint)_inputBuffers["##name"].Length))
                                {
                                    o.name = GetStringFromBuffer("##name");
                                    o.GetMesh().recalculate = true;
                                    o.GetMesh().RecalculateModelMatrix(new bool[] { true, true, true });
                                    o.UpdatePhysxPositionAndRotation();
                                }

                                ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                                if (ImGui.TreeNode("Transform"))
                                {
                                    bool commit = false;
                                    ImGui.PushItemWidth(50);

                                    ImGui.Separator();

                                    #region Position
                                    ImGui.Text("Position");

                                    ImGui.Text("X");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.Position.X.ToString(), 0, o.Position.X.ToString().Length, _inputBuffers["##positionX"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##positionX", _inputBuffers["##positionX"], (uint)_inputBuffers["##positionX"].Length))
                                    {
                                        commit = true;
                                    }
                                    if (!ImGui.IsItemActive() && ImGui.IsItemDeactivated())
                                        commit = true;
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##positionX");
                                        float value = o.Position.X;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            o.Position = o.Position + new Vector3(value, 0, 0);
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { true, false, false });
                                            o.UpdatePhysxPositionAndRotation();
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Y");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.Position.Y.ToString(), 0, o.Position.Y.ToString().Length, _inputBuffers["##positionY"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##positionY", _inputBuffers["##positionY"], (uint)_inputBuffers["##positionY"].Length))
                                    {
                                        commit = true;
                                    }
                                    if (!ImGui.IsItemActive() && ImGui.IsItemDeactivated())
                                        commit = true;
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##positionY");
                                        float value = o.Position.Y;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            o.Position = o.Position + new Vector3(0, value, 0);
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { true, false, false });
                                            o.UpdatePhysxPositionAndRotation();
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Z");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.Position.Z.ToString(), 0, o.Position.Z.ToString().Length, _inputBuffers["##positionZ"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##positionZ", _inputBuffers["##positionZ"], (uint)_inputBuffers["##positionZ"].Length))
                                    {
                                        commit = true;
                                    }
                                    if (!ImGui.IsItemActive() && ImGui.IsItemDeactivated())
                                        commit = true;
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##positionZ");
                                        float value = o.Position.Z;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            o.Position = o.Position + new Vector3(0,0,value);
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { true, false, false });
                                            o.UpdatePhysxPositionAndRotation();
                                        }
                                    }
                                    #endregion

                                    ImGui.Separator();

                                    #region Rotation
                                    ImGui.Text("Rotation");

                                    Vector3 rotation = Helper.EulerFromQuaternion(o.Rotation);

                                    ImGui.Text("X");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(rotation.X.ToString(), 0, rotation.X.ToString().Length, _inputBuffers["##rotationX"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##rotationX", _inputBuffers["##rotationX"], (uint)_inputBuffers["##rotationX"].Length, ImGuiInputTextFlags.EnterReturnsTrue))
                                    {
                                        commit = true;
                                    }
                                    if (!ImGui.IsItemActive() && ImGui.IsItemDeactivated())
                                        commit = true;
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;
                                    
                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##rotationX");
                                        float value = rotation.X;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            rotation.X = value;
                                            o.Rotation = Helper.QuaternionFromEuler(rotation);
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { false, true, false });
                                            o.UpdatePhysxPositionAndRotation();
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Y");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(rotation.Y.ToString(), 0, rotation.Y.ToString().Length, _inputBuffers["##rotationY"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##rotationY", _inputBuffers["##rotationY"], (uint)_inputBuffers["##rotationY"].Length, ImGuiInputTextFlags.EnterReturnsTrue))
                                    {
                                        commit = true;
                                    }
                                    if (!ImGui.IsItemActive() && ImGui.IsItemDeactivated())
                                        commit = true;
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = Encoding.UTF8.GetString(_inputBuffers["##rotationY"]).TrimEnd('\0');
                                        float value = rotation.Y;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            rotation.Y = value;
                                            o.Rotation = Helper.QuaternionFromEuler(rotation);
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { false, true, false });
                                            o.UpdatePhysxPositionAndRotation();
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Z");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(rotation.Z.ToString(), 0, rotation.Z.ToString().Length, _inputBuffers["##rotationZ"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##rotationZ", _inputBuffers["##rotationZ"], (uint)_inputBuffers["##rotationZ"].Length, ImGuiInputTextFlags.EnterReturnsTrue))
                                    {
                                        commit = true;
                                    }
                                    if (!ImGui.IsItemActive() && ImGui.IsItemDeactivated())
                                        commit = true;
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = Encoding.UTF8.GetString(_inputBuffers["##rotationZ"]).TrimEnd('\0');
                                        float value = rotation.Z;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            rotation.Z = value;
                                            o.Rotation = Helper.QuaternionFromEuler(rotation);
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { false, true, false });
                                            o.UpdatePhysxPositionAndRotation();
                                        }
                                    }
                                    #endregion

                                    ImGui.Separator();

                                    #region Scale
                                    ImGui.Text("Scale");

                                    ImGui.Text("X");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.Scale.X.ToString(), 0, o.Scale.X.ToString().Length, _inputBuffers["##scaleX"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##scaleX", _inputBuffers["##scaleX"], (uint)_inputBuffers["##scaleX"].Length))
                                    {
                                        commit = true;
                                    }
                                    if (!ImGui.IsItemActive() && ImGui.IsItemDeactivated())
                                        commit = true;
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##scaleX");
                                        float value = o.Scale.X;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            o.Scale.X = value;
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { false, false, true });
                                            o.UpdatePhysxScale();
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Y");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.Scale.Y.ToString(), 0, o.Scale.Y.ToString().Length, _inputBuffers["##scaleY"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##scaleY", _inputBuffers["##scaleY"], (uint)_inputBuffers["##scaleY"].Length))
                                    {
                                        commit = true;
                                    }
                                    if (!ImGui.IsItemActive() && ImGui.IsItemDeactivated())
                                        commit = true;
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##scaleY");
                                        float value = o.Scale.Y;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            o.Scale.Y = value;
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { false, false, true });
                                            o.UpdatePhysxScale();
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Z");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.Scale.Z.ToString(), 0, o.Scale.Z.ToString().Length, _inputBuffers["##scaleZ"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##scaleZ", _inputBuffers["##scaleZ"], (uint)_inputBuffers["##scaleZ"].Length))
                                    {
                                        commit = true;
                                    }
                                    if (!ImGui.IsItemActive() && ImGui.IsItemDeactivated())
                                        commit = true;
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##scaleZ");
                                        float value = o.Scale.Z;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            o.Scale.Z = value;
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { false, false, true });
                                            o.UpdatePhysxScale();
                                        }
                                    }
                                    #endregion

                                    ImGui.Separator();

                                    ImGui.PopItemWidth();

                                    ImGui.TreePop();
                                }

                                ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                                if(ImGui.TreeNode("Render"))
                                {
                                    ImGui.Separator();

                                    ImGui.Text("Mesh");

                                    #region Mesh

                                    if (ImGui.Checkbox("##useBVH", ref o.useBVH))
                                    {
                                        if(o.useBVH)
                                        {
                                            o.BuildBVH();
                                        }
                                        else
                                        {
                                            o.GetMesh().BVHStruct = null;
                                        }
                                    }
                                    ImGui.SameLine();
                                    ImGui.Text("Use BVH for rendering");

                                    ImGui.Separator();

                                    Encoding.UTF8.GetBytes(o.meshName, 0, o.meshName.ToString().Length, _inputBuffers["##meshPath"], 0);
                                    ImGui.InputText("##meshPath", _inputBuffers["##meshPath"], (uint)_inputBuffers["##meshPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        o.meshName = "";
                                        _inputBuffers["##meshPath"][0] = 0;
                                    }

                                    if (ImGui.BeginDragDropTarget())
                                    {
                                        ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("MESH_NAME");
                                        unsafe
                                        {
                                            if (payload.NativePtr != null)
                                            {
                                                byte[] pathBytes = new byte[payload.DataSize];
                                                System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                CopyDataToBuffer("##meshPath", pathBytes);
                                                o.meshName = GetStringFromByte(pathBytes);
                                            }
                                        }
                                        ImGui.EndDragDropTarget();
                                    }
                                    #endregion

                                    ImGui.Separator();

                                    #region Textures
                                    ImGui.Text("Texture");
                                    Encoding.UTF8.GetBytes(o.textureName, 0, o.textureName.ToString().Length, _inputBuffers["##texturePath"], 0);
                                    ImGui.InputText("##texturePath", _inputBuffers["##texturePath"], (uint)_inputBuffers["##texturePath"].Length, ImGuiInputTextFlags.ReadOnly);
                                    if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        o.textureName = "";
                                        _inputBuffers["##texturePath"][0] = 0;
                                    }

                                    if (ImGui.BeginDragDropTarget())
                                    {
                                        ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                        unsafe
                                        {
                                            if (payload.NativePtr != null)
                                            {
                                                byte[] pathBytes = new byte[payload.DataSize];
                                                System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                CopyDataToBuffer("##texturePath", pathBytes);
                                                o.textureName = GetStringFromByte(pathBytes);
                                            }
                                        }
                                        ImGui.EndDragDropTarget();
                                    }

                                    if (ImGui.TreeNode("Custom textures"))
                                    {
                                        ImGui.Text("Normal Texture");
                                        Encoding.UTF8.GetBytes(o.textureNormalName, 0, o.textureNormalName.ToString().Length, _inputBuffers["##textureNormalPath"], 0);
                                        ImGui.InputText("##textureNormalPath", _inputBuffers["##textureNormalPath"], (uint)_inputBuffers["##textureNormalPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                        {
                                            o.textureNormalName = "";
                                            _inputBuffers["##textureNormalPath"][0] = 0;
                                        }

                                        if (ImGui.BeginDragDropTarget())
                                        {
                                            ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                            unsafe
                                            {
                                                if (payload.NativePtr != null)
                                                {
                                                    byte[] pathBytes = new byte[payload.DataSize];
                                                    System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                    CopyDataToBuffer("##textureNormalPath", pathBytes);
                                                    o.textureNormalName = GetStringFromByte(pathBytes);
                                                }
                                            }
                                            ImGui.EndDragDropTarget();
                                        }
                                        ImGui.Text("Height Texture");
                                        Encoding.UTF8.GetBytes(o.textureHeightName, 0, o.textureHeightName.ToString().Length, _inputBuffers["##textureHeightPath"], 0);
                                        ImGui.InputText("##textureHeightPath", _inputBuffers["##textureHeightPath"], (uint)_inputBuffers["##textureHeightPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                        {
                                            o.textureHeightName = "";
                                            _inputBuffers["##textureHeightPath"][0] = 0;
                                        }

                                        if (ImGui.BeginDragDropTarget())
                                        {
                                            ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                            unsafe
                                            {
                                                if (payload.NativePtr != null)
                                                {
                                                    byte[] pathBytes = new byte[payload.DataSize];
                                                    System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                    CopyDataToBuffer("##textureHeightPath", pathBytes);
                                                    o.textureHeightName = GetStringFromByte(pathBytes);
                                                }
                                            }
                                            ImGui.EndDragDropTarget();
                                        }
                                        ImGui.Text("AO Texture");
                                        Encoding.UTF8.GetBytes(o.textureAOName, 0, o.textureAOName.ToString().Length, _inputBuffers["##textureAOPath"], 0);
                                        ImGui.InputText("##textureAOPath", _inputBuffers["##textureAOPath"], (uint)_inputBuffers["##textureAOPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                        {
                                            o.textureAOName = "";
                                            _inputBuffers["##textureAOPath"][0] = 0;
                                        }

                                        if (ImGui.BeginDragDropTarget())
                                        {
                                            ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                            unsafe
                                            {
                                                if (payload.NativePtr != null)
                                                {
                                                    byte[] pathBytes = new byte[payload.DataSize];
                                                    System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                    CopyDataToBuffer("##textureAOPath", pathBytes);
                                                    o.textureAOName = GetStringFromByte(pathBytes);
                                                }
                                            }
                                            ImGui.EndDragDropTarget();
                                        }
                                        ImGui.Text("Rough Texture");
                                        Encoding.UTF8.GetBytes(o.textureRoughName, 0, o.textureRoughName.ToString().Length, _inputBuffers["##textureRoughPath"], 0);
                                        ImGui.InputText("##textureRoughPath", _inputBuffers["##textureRoughPath"], (uint)_inputBuffers["##textureRoughPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                        {
                                            o.textureRoughName = "";
                                            _inputBuffers["##textureRoughPath"][0] = 0;
                                        }

                                        if (ImGui.BeginDragDropTarget())
                                        {
                                            ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                            unsafe
                                            {
                                                if (payload.NativePtr != null)
                                                {
                                                    byte[] pathBytes = new byte[payload.DataSize];
                                                    System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                    CopyDataToBuffer("##textureRoughPath", pathBytes);
                                                    o.textureRoughName = GetStringFromByte(pathBytes);
                                                }
                                            }
                                            ImGui.EndDragDropTarget();
                                        }
                                        ImGui.Text("Metal Texture");
                                        Encoding.UTF8.GetBytes(o.textureMetalName, 0, o.textureMetalName.ToString().Length, _inputBuffers["##textureMetalPath"], 0);
                                        ImGui.InputText("##textureMetalPath", _inputBuffers["##textureMetalPath"], (uint)_inputBuffers["##textureMetalPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                        {
                                            o.textureMetalName = "";
                                            _inputBuffers["##textureMetalPath"][0] = 0;
                                        }

                                        if (ImGui.BeginDragDropTarget())
                                        {
                                            ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                            unsafe
                                            {
                                                if (payload.NativePtr != null)
                                                {
                                                    byte[] pathBytes = new byte[payload.DataSize];
                                                    System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                    CopyDataToBuffer("##textureMetalPath", pathBytes);
                                                    o.textureMetalName = GetStringFromByte(pathBytes);
                                                }
                                            }
                                            ImGui.EndDragDropTarget();
                                        }

                                        ImGui.TreePop();
                                    }
                                    #endregion

                                    ImGui.TreePop();
                                }

                                ImGui.Separator();

                                ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                                if (ImGui.TreeNode("Physics"))
                                {
                                    #region Physx
                                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5.0f);
                                    var buttonColor = style.Colors[(int)ImGuiCol.Button];
                                    style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f);

                                    if (o.HasCollider)
                                    {
                                        System.Numerics.Vector2 buttonSize = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 40);
                                        if (ImGui.Button("Remove\n" + o.colliderStaticType + " " + o.colliderType + " Collider", buttonSize))
                                        {
                                            o.RemoveCollider();
                                        }
                                    }
                                    else
                                    {
                                        System.Numerics.Vector2 buttonSize = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 20);
                                        string[] options = { "Static", "Dynamic" };
                                        for (int i = 0; i < options.Length; i++)
                                        {
                                            if (ImGui.RadioButton(options[i], o.selectedColliderOption == i))
                                            {
                                                o.selectedColliderOption = i;
                                            }

                                            if(i != options.Length - 1)
                                                ImGui.SameLine();
                                        }

                                        if (o.selectedColliderOption == 0)
                                        {
                                            if (ImGui.Button("Add Triangle Mesh Collider", buttonSize))
                                            {
                                                o.AddTriangleMeshCollider();
                                            }
                                            if (ImGui.Button("Add Cube Collider", buttonSize))
                                            {
                                                o.AddCubeCollider(true);
                                            }
                                            if (ImGui.Button("Add Sphere Collider", buttonSize))
                                            {
                                                o.AddSphereCollider(true);
                                            }
                                            if (ImGui.Button("Add Capsule Collider", buttonSize))
                                            {
                                                o.AddCapsuleCollider(true);
                                            }
                                        }
                                        else if(o.selectedColliderOption == 1)
                                        {
                                            if (ImGui.Button("Add Cube Collider", buttonSize))
                                            {
                                                o.AddCubeCollider(false);
                                            }
                                            if (ImGui.Button("Add Sphere Collider", buttonSize))
                                            {
                                                o.AddSphereCollider(false);
                                            }
                                            if (ImGui.Button("Add Capsule Collider", buttonSize))
                                            {
                                                o.AddCapsuleCollider(false);
                                            }
                                        }
                                    }
                                    style.Colors[(int)ImGuiCol.Button] = buttonColor;
                                    ImGui.PopStyleVar();
                                    #endregion

                                    ImGui.TreePop();
                                }

                                ImGui.Dummy(new System.Numerics.Vector2(0, 50));
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
            ImGui.PopStyleVar();
            ImGui.End();
            #endregion

            #region Right panel seperator
            style.WindowMinSize = new System.Numerics.Vector2(seperatorSize, seperatorSize);
            style.Colors[(int)ImGuiCol.WindowBg] = seperatorColor;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(seperatorSize, 
                _windowHeight - gameWindow.topPanelSize - gameWindow.bottomPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth * (1 - gameWindow.rightPanelPercent), gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
            if (ImGui.Begin("RightSeparator", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings |
                                         ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.InvisibleButton("##RightSeparatorButton", new System.Numerics.Vector2(seperatorSize, _windowHeight));

                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        isResizingRight = true;

                    mouseTypes[1] = true;
                }
                else
                    mouseTypes[1] = false;

                if (isResizingRight)
                {
                    mouseTypes[1] = true;

                    float mouseX = ImGui.GetIO().MousePos.X;
                    gameWindow.rightPanelPercent = 1 - mouseX / _windowWidth;
                    if (gameWindow.leftPanelPercent + gameWindow.rightPanelPercent > 0.75)
                    {
                        gameWindow.rightPanelPercent = 1 - gameWindow.leftPanelPercent - 0.25f;
                    }
                    else
                    {
                        if (gameWindow.rightPanelPercent < 0.05f)
                            gameWindow.rightPanelPercent = 0.05f;
                        if (gameWindow.rightPanelPercent > 0.75f)
                            gameWindow.rightPanelPercent = 0.75f;
                    }

                    editorData.windowResized = true;

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        isResizingRight = false;
                    }
                }
            }
            ImGui.End();
            ImGui.PopStyleVar(); // Pop the style for padding
            style.WindowMinSize = new System.Numerics.Vector2(32, 32);
            style.Colors[(int)ImGuiCol.WindowBg] = baseBGColor;
            #endregion

            #region Bottom asset panel seperator
            style.WindowMinSize = new System.Numerics.Vector2(seperatorSize, seperatorSize);
            style.Colors[(int)ImGuiCol.WindowBg] = seperatorColor;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, seperatorSize));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight * (1 - gameWindow.bottomPanelPercent) - gameWindow.bottomPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
            if (ImGui.Begin("BottomAssetSeparator", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings |
                                         ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.InvisibleButton("##BottomSeparatorButton", new System.Numerics.Vector2(_windowWidth, seperatorSize));

                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        isResizingBottom = true;

                    mouseTypes[2] = true;
                }
                else
                    mouseTypes[2] = false;

                if (isResizingBottom)
                {
                    mouseTypes[2] = true;

                    float mouseY = ImGui.GetIO().MousePos.Y;
                    gameWindow.bottomPanelPercent = 1 - mouseY / (_windowHeight - gameWindow.bottomPanelSize-5);
                    if (gameWindow.bottomPanelPercent < 0.05f)
                        gameWindow.bottomPanelPercent = 0.05f;
                    if(gameWindow.bottomPanelPercent > 0.75f)
                        gameWindow.bottomPanelPercent = 0.75f;

                    editorData.windowResized = true;

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        isResizingBottom = false;
                    }
                }
            }
            ImGui.End();
            ImGui.PopStyleVar(); // Pop the style for padding
            style.WindowMinSize = new System.Numerics.Vector2(32, 32);
            style.Colors[(int)ImGuiCol.WindowBg] = baseBGColor;
            #endregion

            #region Bottom asset panel
            style.WindowRounding = 0f;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, _windowHeight * gameWindow.bottomPanelPercent - seperatorSize));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight * (1 - gameWindow.bottomPanelPercent) + seperatorSize - gameWindow.bottomPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("BottomAssetPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if (ImGui.BeginTabBar("MyTabs"))
                {
                    if (ImGui.BeginTabItem("Project"))
                    {
                        if(ImGui.BeginTabBar("Assets"))
                        {
                            float imageWidth = 100;
                            float imageHeight = 100;
                            System.Numerics.Vector2 imageSize = new System.Numerics.Vector2(imageWidth, imageHeight);

                            var groupedAssets = editorData.assets.GroupBy(asset => asset.Type).ToDictionary(group => group.Key, group => group.ToList());
                            if (ImGui.BeginTabItem("Textures"))
                            {

                                if (groupedAssets.TryGetValue(AssetType.Texture, out var textureAssets))
                                {
                                    float padding = ImGui.GetStyle().WindowPadding.X;
                                    float spacing = ImGui.GetStyle().ItemSpacing.X;

                                    System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                                    int columns = (int)((availableSpace.X + spacing) / (imageWidth + spacing));
                                    columns = Math.Max(1, columns);

                                    float itemWidth = availableSpace.X / columns - spacing;

                                    for (int i = 0; i < textureAssets.Count; i++)
                                    {
                                        if (Engine.textureManager.textures.ContainsKey("ui_" + textureAssets[i].Name))
                                        {
                                            if (i % columns != 0)
                                            {
                                                ImGui.SameLine();
                                            }

                                            ImGui.BeginGroup();
                                            ImGui.PushID(textureAssets[i].Id);

                                            System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                            ImGui.Image((IntPtr)Engine.textureManager.textures["ui_" + textureAssets[i].Name].TextureId, imageSize);
                                            ImGui.SetCursorPos(cursorPos);

                                            ImGui.InvisibleButton("##invisible", imageSize);

                                            DragDropImageSourceUI(ref Engine.textureManager, "TEXTURE_NAME", textureAssets[i].Name, imageSize);

                                            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                                            ImGui.TextWrapped(textureAssets[i].Name);
                                            ImGui.PopTextWrapPos();

                                            ImGui.PopID();

                                            ImGui.EndGroup();
                                        }
                                    }

                                    ImGui.Dummy(new System.Numerics.Vector2(0.0f, 10f));
                                }
                                ImGui.EndTabItem();
                            }
                            if(ImGui.BeginTabItem("Models"))
                            {
                                if (groupedAssets.TryGetValue(AssetType.Model, out var modelAssets))
                                {
                                    float padding = ImGui.GetStyle().WindowPadding.X;
                                    float spacing = ImGui.GetStyle().ItemSpacing.X;

                                    System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                                    int columns = (int)((availableSpace.X + spacing) / (imageWidth + spacing));
                                    columns = Math.Max(1, columns);

                                    float itemWidth = availableSpace.X / columns - spacing;

                                    for (int i = 0; i < modelAssets.Count; i++)
                                    {
                                        string modelName = "ui_missing.png";
                                        if (Engine.textureManager.textures.ContainsKey(modelAssets[i].Name))
                                        {
                                            modelName = modelAssets[i].Name;
                                        }

                                        if (i % columns != 0)
                                        {
                                            ImGui.SameLine();
                                        }

                                        ImGui.BeginGroup();
                                        ImGui.PushID(modelAssets[i].Id);

                                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                        ImGui.Image((IntPtr)Engine.textureManager.textures[modelName].TextureId, imageSize);
                                        ImGui.SetCursorPos(cursorPos);

                                        ImGui.InvisibleButton("##invisible", imageSize);

                                        DragDropImageSource(ref Engine.textureManager, "MESH_NAME", modelAssets[i].Name, modelName, imageSize);

                                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                                        ImGui.TextWrapped(modelAssets[i].Name);
                                        ImGui.PopTextWrapPos();

                                        ImGui.PopID();

                                        ImGui.EndGroup();
                                    }
                                }

                                ImGui.EndTabItem();
                            }
                            if(ImGui.BeginTabItem("Audio"))
                            {
                                if (groupedAssets.TryGetValue(AssetType.Audio, out var audioAssets))
                                {
                                    foreach (var asset in audioAssets)
                                    {
                                        ImGui.Text(asset.Name);
                                    }
                                }

                                ImGui.EndTabItem();
                            }

                            ImGui.EndTabBar();
                        }

                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Console"))
                    {
                        ImGui.Text("Content for Tab 1");

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
            ImGui.PopStyleVar();
            ImGui.End();
            #endregion

            #region Bottom Panel
            style.Colors[(int)ImGuiCol.WindowBg] = style.Colors[(int)ImGuiCol.MenuBarBg];
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, gameWindow.bottomPanelSize + 4));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight - gameWindow.bottomPanelSize - 4), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("BottomPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                string fpsStr = editorData.fps.GetFpsString();
                System.Numerics.Vector2 bottomPanelSize = ImGui.GetContentRegionAvail();
                System.Numerics.Vector2 textSize = ImGui.CalcTextSize(fpsStr);
                ImGui.SetCursorPosX(bottomPanelSize.X - textSize.X);
                ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                ImGui.Text(fpsStr);
            }
            #endregion

            #region MouseType
            if (mouseTypes[0] || mouseTypes[1])
                editorData.mouseType = MouseCursor.HResize;
            else if (mouseTypes[2])
                editorData.mouseType = MouseCursor.VResize;
            else
                editorData.mouseType = MouseCursor.Default;
            #endregion
        }

        private void DragDropImageSourceUI(ref TextureManager textureManager, string payloadName, string assetName, System.Numerics.Vector2 size)
        {
            if (ImGui.IsItemActive())
            {
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    byte[] pathBytes = Encoding.UTF8.GetBytes(assetName);
                    unsafe
                    {
                        fixed (byte* pPath = pathBytes)
                        {
                            ImGui.SetDragDropPayload(payloadName, (IntPtr)pPath, (uint)pathBytes.Length);
                        }
                    }
                    ImGui.Image((IntPtr)textureManager.textures["ui_" + assetName].TextureId, size);
                    ImGui.TextWrapped(assetName);

                    ImGui.EndDragDropSource();
                }
            }
        }

        private void DragDropImageSource(ref TextureManager textureManager, string payloadName, string assetName, string textureName, System.Numerics.Vector2 size)
        {
            if (ImGui.IsItemActive())
            {
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    byte[] pathBytes = Encoding.UTF8.GetBytes(assetName);
                    unsafe
                    {
                        fixed (byte* pPath = pathBytes)
                        {
                            ImGui.SetDragDropPayload(payloadName, (IntPtr)pPath, (uint)pathBytes.Length);
                        }
                    }
                    ImGui.Image((IntPtr)textureManager.textures[textureName].TextureId, size);
                    ImGui.TextWrapped(assetName);

                    ImGui.EndDragDropSource();
                }
            }
        }

        public void FullscreenWindow(Engine.GameWindowProperty gameWindow, ref EditorData editorData)
        {
            var io = ImGui.GetIO();

            if (editorData.gameRunning == GameState.Running && !editorData.manualCursor)
                io.ConfigFlags |= ImGuiConfigFlags.NoMouse;
            else
                io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;

            float totalWidth = 40;
            if (editorData.gameRunning == GameState.Running)
                totalWidth = 60;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(totalWidth*2, 40));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth/2 - totalWidth, 0), ImGuiCond.Always);
            if (ImGui.Begin("TopFullscreenPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar |
                                                  ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse ))
            {

                if (editorData.gameRunning == GameState.Stopped)
                {
                    if (ImGui.ImageButton("play", (IntPtr)Engine.textureManager.textures["ui_play.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Running;
                        editorData.justSetGameState = true;
                    }
                }
                else
                {
                    if (ImGui.ImageButton("stop", (IntPtr)Engine.textureManager.textures["ui_stop.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Stopped;
                        editorData.justSetGameState = true;
                    }
                }

                ImGui.SameLine();
                if (editorData.gameRunning == GameState.Running)
                {
                    if (ImGui.ImageButton("pause", (IntPtr)Engine.textureManager.textures["ui_pause.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.isPaused = !editorData.isPaused;
                    }
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("screen", (IntPtr)Engine.textureManager.textures["ui_screen.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                {
                    editorData.isGameFullscreen = !editorData.isGameFullscreen;
                }
            }
            ImGui.End();

            return;
        }
    }
}
