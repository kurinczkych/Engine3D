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
    public enum GameState
    {
        Running,
        Stopped
    }

    public class EditorData
    {
        public List<Object> objects;
        public List<ParticleSystem> particleSystems;
        public List<PointLight> pointLights;

        public object selectedItem;

        public EditorData() { }
    }

    public class EditorProperties
    {
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

        public EditorProperties()
        {
            _gameRunning = GameState.Stopped;
        }
    }

    public class ImGuiController : BaseImGuiController
    {

        private EditorProperties editorProperties;

        private Dictionary<string, byte[]> _inputBuffers = new Dictionary<string, byte[]>();

        private bool isResizingLeft = false;
        private bool isResizingRight = false;
        private bool isResizingBottom = false;
        private float seperatorSize = 5;
        private System.Numerics.Vector4 baseBGColor = new System.Numerics.Vector4(0.352f, 0.352f, 0.352f, 1.0f);
        private System.Numerics.Vector4 seperatorColor = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);

        public ImGuiController(int width, int height, ref EditorProperties editorProperties) : base(width, height)
        {
            this.editorProperties = editorProperties;

            #region Input boxes
            _inputBuffers.Add("##name", new byte[100]);

            _inputBuffers.Add("##positionX", new byte[100]);
            _inputBuffers.Add("##positionY", new byte[100]);
            _inputBuffers.Add("##positionZ", new byte[100]);
            _inputBuffers.Add("##rotationX", new byte[200]);
            _inputBuffers.Add("##rotationY", new byte[200]);
            _inputBuffers.Add("##rotationZ", new byte[200]);
            _inputBuffers.Add("##scaleX", new byte[200]);
            _inputBuffers.Add("##scaleY", new byte[200]);
            _inputBuffers.Add("##scaleZ", new byte[200]);

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

        public void EditorWindow(Engine.GameWindowProperty gameWindow, Dictionary<string, Texture> guiTextures,
                                 ref EditorData editorData, KeyboardState keyboardState)
        {
            var io = ImGui.GetIO();

            if (editorProperties.gameRunning == GameState.Running && !editorProperties.manualCursor)
                io.ConfigFlags |= ImGuiConfigFlags.NoMouse;
            else
                io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;

            editorProperties.windowResized = false;
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
                            editorProperties.windowResized = true;
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenuBar();
                }

                var button = style.Colors[(int)ImGuiCol.Button];
                style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                float totalWidth = 40;
                if (editorProperties.gameRunning == GameState.Running)
                    totalWidth = 60;

                float startX = (ImGui.GetWindowSize().X - totalWidth) * 0.5f;
                float startY = (ImGui.GetWindowSize().Y - 10) * 0.5f;
                ImGui.SetCursorPos(new System.Numerics.Vector2(startX, startY));

                if (editorProperties.gameRunning == GameState.Stopped)
                {
                    if (ImGui.ImageButton("play", (IntPtr)guiTextures["play"].textureDescriptor.TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorProperties.gameRunning = GameState.Running;
                        editorProperties.justSetGameState = true;
                    }
                }
                else
                {
                    if (ImGui.ImageButton("stop", (IntPtr)guiTextures["stop"].textureDescriptor.TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorProperties.gameRunning = GameState.Stopped;
                        editorProperties.justSetGameState = true;
                    }
                }

                ImGui.SameLine();
                if (editorProperties.gameRunning == GameState.Running)
                {
                    if (ImGui.ImageButton("pause", (IntPtr)guiTextures["pause"].textureDescriptor.TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorProperties.isPaused = !editorProperties.isPaused;
                    }
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("screen", (IntPtr)guiTextures["screen"].textureDescriptor.TextureId, new System.Numerics.Vector2(20, 20)))
                {
                    editorProperties.isGameFullscreen = !editorProperties.isGameFullscreen;
                }


                style.Colors[(int)ImGuiCol.Button] = button;
            }
            ImGui.End();
            #endregion

            #region Left panel
            List<Object> meshes = editorData.objects.Where(x => x.meshType == typeof(Mesh)).ToList();
            List<Object> instMeshes = editorData.objects.Where(x => x.meshType == typeof(InstancedMesh)).ToList();
            List<ParticleSystem> particleSystems = editorData.particleSystems;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth * gameWindow.leftPanelPercent, 
                                                                _windowHeight - gameWindow.topPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
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
                                    if(ImGui.Selectable(name))
                                    {
                                        editorData.selectedItem = meshes[i];
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
                                        editorData.selectedItem = meshes[i];
                                    }
                                }

                                ImGui.TreePop();
                            }
                        }

                        if (editorData.particleSystems.Count > 0)
                        {
                            if (ImGui.TreeNode("Particle systems"))
                            {
                                for (int i = 0; i < editorData.particleSystems.Count; i++)
                                {
                                    string name = editorData.particleSystems[i].name == "" ? "Particle system " + i.ToString() : editorData.particleSystems[i].name;
                                    if (ImGui.Selectable(name))
                                    {
                                        editorData.selectedItem = meshes[i];
                                    }
                                }

                                ImGui.TreePop();
                            }
                        }

                        if (editorData.pointLights.Count > 0)
                        {
                            if (ImGui.TreeNode("Point lights"))
                            {
                                for (int i = 0; i < editorData.pointLights.Count; i++)
                                {
                                    string name = editorData.pointLights[i].name == "" ? "Point light " + i.ToString() : editorData.pointLights[i].name;
                                    if (ImGui.Selectable(name))
                                    {
                                        editorData.selectedItem = meshes[i];
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
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(seperatorSize, _windowHeight - gameWindow.topPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
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
                        gameWindow.leftPanelPercent = 1 - gameWindow.rightPanelPercent - 0.15f;
                    }
                    else
                    {
                        if (gameWindow.leftPanelPercent < 0.05f)
                            gameWindow.leftPanelPercent = 0.05f;
                        if (gameWindow.leftPanelPercent > 0.75f)
                            gameWindow.leftPanelPercent = 0.75f;
                    }

                    editorProperties.windowResized = true;

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
                                                                _windowHeight - gameWindow.topPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
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
                                }

                                ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                                if (ImGui.TreeNode("Transform"))
                                {
                                    bool commit = false;
                                    ImGui.PushItemWidth(50);

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
                                            o.Position.X = value;
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { true, false, false });
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
                                            o.Position.Y = value;
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { true, false, false });
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
                                            o.Position.Z = value;
                                            o.GetMesh().recalculate = true;
                                            o.GetMesh().RecalculateModelMatrix(new bool[] { true, false, false });
                                        }
                                    }
                                    #endregion

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
                                        }
                                    }
                                    #endregion

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
                                        }
                                    }
                                    #endregion

                                    ImGui.PopItemWidth();

                                    ImGui.TreePop();
                                }
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
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(seperatorSize, _windowHeight - gameWindow.topPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
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
                        gameWindow.rightPanelPercent = 1 - gameWindow.leftPanelPercent - 0.15f;
                    }
                    else
                    {
                        if (gameWindow.rightPanelPercent < 0.05f)
                            gameWindow.rightPanelPercent = 0.05f;
                        if (gameWindow.rightPanelPercent > 0.75f)
                            gameWindow.rightPanelPercent = 0.75f;
                    }

                    editorProperties.windowResized = true;

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

            #region Bottom panel
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, _windowHeight * gameWindow.bottomPanelPercent - seperatorSize));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight * (1 - gameWindow.bottomPanelPercent) + seperatorSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("BottomPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if (ImGui.BeginTabBar("MyTabs"))
                {
                    if (ImGui.BeginTabItem("Project"))
                    {
                        ImGui.Text("Content for Tab 1");

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

            #region Bottom panel seperator
            style.WindowMinSize = new System.Numerics.Vector2(seperatorSize, seperatorSize);
            style.Colors[(int)ImGuiCol.WindowBg] = seperatorColor;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, seperatorSize));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight * (1 - gameWindow.bottomPanelPercent)), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
            if (ImGui.Begin("BottomSeparator", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings |
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
                    gameWindow.bottomPanelPercent = 1 - mouseY / _windowHeight;
                    if (gameWindow.bottomPanelPercent < 0.05f)
                        gameWindow.bottomPanelPercent = 0.05f;
                    if(gameWindow.bottomPanelPercent > 0.75f)
                        gameWindow.bottomPanelPercent = 0.75f;

                    editorProperties.windowResized = true;

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

            #region MouseType
            if (mouseTypes[0] || mouseTypes[1])
                editorProperties.mouseType = MouseCursor.HResize;
            else if (mouseTypes[2])
                editorProperties.mouseType = MouseCursor.VResize;
            else
                editorProperties.mouseType = MouseCursor.Default;
            #endregion
        }
        public void FullscreenWindow(Engine.GameWindowProperty gameWindow, Dictionary<string, Texture> guiTextures)
        {
            var io = ImGui.GetIO();

            if (editorProperties.gameRunning == GameState.Running && !editorProperties.manualCursor)
                io.ConfigFlags |= ImGuiConfigFlags.NoMouse;
            else
                io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;

            float totalWidth = 40;
            if (editorProperties.gameRunning == GameState.Running)
                totalWidth = 60;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(totalWidth*2, 40));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth/2 - totalWidth, 0), ImGuiCond.Always);
            if (ImGui.Begin("TopFullscreenPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar |
                                                  ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse ))
            {

                if (editorProperties.gameRunning == GameState.Stopped)
                {
                    if (ImGui.ImageButton("play", (IntPtr)guiTextures["play"].textureDescriptor.TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorProperties.gameRunning = GameState.Running;
                        editorProperties.justSetGameState = true;
                    }
                }
                else
                {
                    if (ImGui.ImageButton("stop", (IntPtr)guiTextures["stop"].textureDescriptor.TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorProperties.gameRunning = GameState.Stopped;
                        editorProperties.justSetGameState = true;
                    }
                }

                ImGui.SameLine();
                if (editorProperties.gameRunning == GameState.Running)
                {
                    if (ImGui.ImageButton("pause", (IntPtr)guiTextures["pause"].textureDescriptor.TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorProperties.isPaused = !editorProperties.isPaused;
                    }
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("screen", (IntPtr)guiTextures["screen"].textureDescriptor.TextureId, new System.Numerics.Vector2(20, 20)))
                {
                    editorProperties.isGameFullscreen = !editorProperties.isGameFullscreen;
                }
            }
            ImGui.End();

            return;
        }
    }
}
