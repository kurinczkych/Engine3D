using Assimp;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Engine3D.Light;
using static System.Net.Mime.MediaTypeNames;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

#pragma warning disable CS8602

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

        //private bool limitFps = false;
        private const double TargetDeltaTime = 1.0 / 60.0; // for 60 FPS

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
        public bool recalculateObjects = true;

        public FPS fps = new FPS();

        public object? selectedItem;
        public int instIndex = -1;

        public AssetStoreManager assetStoreManager;
        public AssetManager assetManager;
        public TextureManager textureManager;

        public int currentAssetTexture = 0;
        public List<Asset> AssetTextures;

        public GizmoManager gizmoManager;

        public GameWindowProperty gameWindow;

        public Vector2 gizmoWindowPos = Vector2.Zero;
        public Vector2 gizmoWindowSize = Vector2.Zero;

        public Physx physx;

        public bool runParticles = false;
        public bool resetParticles = false;
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

        public int animType = 0;
        public int animEndType = 0;
        public int matrixType = 5;

        public EditorData()
        {
            _gameRunning = GameState.Stopped;
        }
    }

    public partial class ImGuiController : BaseImGuiController
    {
        private EditorData editorData;

        private string currentBottomPanelTab = "Project";
        private Dictionary<string, byte[]> _inputBuffers = new Dictionary<string, byte[]>();

        private bool isResizingLeft = false;
        private bool isResizingRight = false;
        private bool isResizingBottom = false;
        private float seperatorSize = 5;
        private System.Numerics.Vector4 baseBGColor = new System.Numerics.Vector4(0.352f, 0.352f, 0.352f, 1.0f);
        private System.Numerics.Vector4 seperatorColor = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);
        public bool shouldOpenTreeNodeMeshes = true;
        private int isObjectHovered = -1;
        private bool justSelectedItem = false;

        private AssetFolder currentTextureAssetFolder;
        private AssetFolder currentModelAssetFolder;
        private AssetFolder currentAudioAssetFolder;

        List<Object> objects = new List<Object>();

        private Dictionary<string,bool> colorPickerOpen = new Dictionary<string, bool>();
        private bool advancedLightSetting = false;

        private string[] showConsoleTypeList = new string[0];
        private int showConsoleTypeListIndex = 2;

        private bool showAddComponentWindow = false;
        private string searchQueryAddComponent = "";
        private List<ComponentType> availableComponents = new List<ComponentType>();
        private List<ComponentType> filteredComponents = new List<ComponentType>();
        private HashSet<string> exludedComponents = new HashSet<string>
        {
            "BaseMesh"
        };

        public System.Numerics.Vector2? particlesWindowPos = null;
        public System.Numerics.Vector2 particlesWindowSize = new System.Numerics.Vector2(150, 150);

        public ImGuiController(int width, int height, ref EditorData editorData) : base(width, height)
        {
            this.editorData = editorData;
            currentTextureAssetFolder = editorData.assetManager.assets.folders[FileType.Textures.ToString()];
            currentModelAssetFolder = editorData.assetManager.assets.folders[FileType.Models.ToString()];
            currentAudioAssetFolder = editorData.assetManager.assets.folders[FileType.Audio.ToString()];

            showConsoleTypeList = Enum.GetNames(typeof(ShowConsoleType));

            #region GetComponents
            var types = Assembly.GetExecutingAssembly().GetTypes();

            foreach (var type in types)
            {
                if (type.IsClass && typeof(IComponent).IsAssignableFrom(type) && !exludedComponents.Contains(type.Name))
                {
                    availableComponents.Add(new ComponentType(type.Name, type.BaseType.Name, type));
                }
            }

            filteredComponents = new List<ComponentType>(availableComponents);
            #endregion

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
            _inputBuffers.Add("##texturePath", new byte[300]);
            _inputBuffers.Add("##textureNormalPath", new byte[300]);
            _inputBuffers.Add("##textureHeightPath", new byte[300]);
            _inputBuffers.Add("##textureAOPath", new byte[300]);
            _inputBuffers.Add("##textureRoughPath", new byte[300]);
            _inputBuffers.Add("##textureMetalPath", new byte[300]);
            _inputBuffers.Add("##meshPath", new byte[300]);
            _inputBuffers.Add("##assetSearch", new byte[200]);

            #endregion
        }

        #region Buffer Methods
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
            ClearBuffer(buffer);
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
                while (buffer.Value.Length != i && buffer.Value[i] != '\0')
                {
                    buffer.Value[i] = 0;
                    i++;
                }
            }
            ;
        }

        public void ClearBuffer(string buffer)
        {
            int i = 0;
            while (_inputBuffers[buffer][i] != '\0' && _inputBuffers[buffer].Length != i)
            {
                _inputBuffers[buffer][i] = 0;
                i++;
            }
        }
        #endregion

        public void CalculateObjectList()
        {
            objects.Clear();
            objects = new List<Object>(editorData.objects);
            Dictionary<string, int> names = new Dictionary<string, int>();

            for(int i = 0; i < objects.Count; i++)
            {
                string name = objects[i].name == "" ? "Object " + i.ToString() : objects[i].name;
                if (objects[i].GetComponent<BaseMesh>() != null)
                {
                    if (objects[i].GetComponent<BaseMesh>() is InstancedMesh)
                        name += " (Instanced)";
                }

                if (names.ContainsKey(name))
                    names[name] += 1;
                else
                    names[name] = 0;

                if (names[name] == 0)
                    objects[i].displayName = name;
                else
                    objects[i].displayName = name + " (" + names[name] + ")";
            }
        }

        public bool CursorInImGuiWindow(Vector2 cursor)
        {
            System.Numerics.Vector2 pos = particlesWindowPos ?? new System.Numerics.Vector2();
            bool isInHorizontalBounds = cursor.X >= pos.X && cursor.X <= (pos.X + particlesWindowSize.X);

            bool isInVerticalBounds = cursor.Y >= pos.Y && cursor.Y <= (pos.Y + particlesWindowSize.Y);

            return isInHorizontalBounds && isInVerticalBounds;
        }

        #region Helpers

        private Vector3 InputFloat3(string title, string[] names, float[] v3, ref KeyboardState keyboardState, bool hideNames = false)
        {
            if (names.Length != 3)
                throw new Exception("InputFloat3 names length must be 3!");

            float[] vec = new float[]{ v3[0], v3[1], v3[2] };

            if (!hideNames)
            {
                if(title != "")
                    ImGui.Text(title);
            }

            for(int i = 0; i < names.Length; i++)
            {
                string bufferName = "##" + title + names[i];
                if (!_inputBuffers.ContainsKey(bufferName))
                    _inputBuffers.Add(bufferName, new byte[100]);

                if (!hideNames)
                {
                    ImGui.Text(names[i]);
                    ImGui.SameLine();
                }
                Encoding.UTF8.GetBytes(v3[i].ToString(), 0, v3[i].ToString().Length, _inputBuffers[bufferName], 0);
                bool commit = false;
                bool reset = false;
                ImGui.SetNextItemWidth(50);
                if (ImGui.InputText(bufferName, _inputBuffers[bufferName], (uint)_inputBuffers[bufferName].Length))
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer(bufferName);
                    float value = -1;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            vec[i] = value;
                        else
                        {
                            ClearBuffer(bufferName);
                            vec[i] = 0;
                        }
                    }
                }

                if(i != names.Length-1)
                    ImGui.SameLine();
            }

            return new Vector3(vec[0], vec[1], vec[2]);
        }

        private float[] InputFloat2(string title, string[] names, float[] v2, ref KeyboardState keyboardState, bool hideNames = false)
        {
            if (names.Length != 2)
                throw new Exception("InputFloat3 names length must be 3!");

            float[] vec = new float[]{ v2[0], v2[1] };

            if (!hideNames)
            {
                if (title != "")
                    ImGui.Text(title);
            }

            for(int i = 0; i < names.Length; i++)
            {
                string bufferName = "##" + title + names[i];
                if (!_inputBuffers.ContainsKey(bufferName))
                    _inputBuffers.Add(bufferName, new byte[100]);

                if (!hideNames)
                {
                    ImGui.Text(names[i]);
                    ImGui.SameLine();
                }
                Encoding.UTF8.GetBytes(v2[i].ToString(), 0, v2[i].ToString().Length, _inputBuffers[bufferName], 0);
                bool commit = false;
                bool reset = false;
                ImGui.SetNextItemWidth(50);
                if (ImGui.InputText(bufferName, _inputBuffers[bufferName], (uint)_inputBuffers[bufferName].Length))
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer(bufferName);
                    float value = -1;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            vec[i] = value;
                        else
                        {
                            ClearBuffer(bufferName);
                            vec[i] = 0;
                        }
                    }
                }

                if(i != names.Length-1)
                    ImGui.SameLine();
            }

            return vec;
        }

        private float InputFloat1(string title, string[] names, float[] v1, ref KeyboardState keyboardState, bool titleSameLine = false)
        {
            if (names.Length != 1)
                throw new Exception("InputFloat1 names length must be 1!");

            float[] vec = new float[]{ v1[0] };

            if (title != "")
                ImGui.Text(title);

            if(titleSameLine)
                ImGui.SameLine();

            for(int i = 0; i < names.Length; i++)
            {
                string bufferName = "##" + title + names[i];
                if (!_inputBuffers.ContainsKey(bufferName))
                    _inputBuffers.Add(bufferName, new byte[100]);

                ImGui.Text(names[i]);
                ImGui.SameLine();
                Encoding.UTF8.GetBytes(v1[i].ToString(), 0, v1[i].ToString().Length, _inputBuffers[bufferName], 0);
                bool commit = false;
                bool reset = false;
                ImGui.SetNextItemWidth(50);
                if (ImGui.InputText(bufferName, _inputBuffers[bufferName], (uint)_inputBuffers[bufferName].Length))
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer(bufferName);
                    float value = -1;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            vec[i] = value;
                        else
                        {
                            ClearBuffer(bufferName);
                            vec[i] = 0;
                        }
                    }
                }

                if(i != names.Length-1)
                    ImGui.SameLine();
            }

            return vec[0];
        }

        private void ColorPicker(string title, ref Color4 color, float colorPickerRange = 25)
        {
            if (!colorPickerOpen.ContainsKey(title))
                colorPickerOpen.Add(title, false);

            System.Numerics.Vector3 colorv3 = new System.Numerics.Vector3(color.R, color.G, color.B);
            if (colorPickerOpen[title])
            {
                if (ImGui.ColorPicker3(title, ref colorv3))
                {
                    color = new Color4(colorv3.X, colorv3.Y, colorv3.Z, 1.0f);
                }
                System.Numerics.Vector2 pickerMax = ImGui.GetItemRectMax();
                System.Numerics.Vector2 pickerMin = ImGui.GetItemRectMin();

                System.Numerics.Vector2 mousePos = ImGui.GetMousePos();

                bool isOutside =
                    mousePos.X < pickerMin.X - colorPickerRange || mousePos.X > pickerMax.X + colorPickerRange ||
                    mousePos.Y < pickerMin.Y - colorPickerRange || mousePos.Y > pickerMax.Y + colorPickerRange;

                if (isOutside)
                {
                    colorPickerOpen[title] = false;
                }
            }
            else
            {
                ImGui.Text(title);
                ImGui.SameLine();
                if (ImGui.ColorButton(title, new System.Numerics.Vector4(color.R, color.G, color.B, 1.0f)))
                {
                    colorPickerOpen[title] = true;
                }
            }
        }
        private void CenteredText(string text)
        {
            var originalXPos = ImGui.GetCursorPosX();
            var windowSize = ImGui.GetWindowSize();
            var textSize = ImGui.CalcTextSize(text);
            float centeredXPos = (windowSize.X - textSize.X) / 2.0f;

            ImGui.SetCursorPosX(centeredXPos);
            ImGui.Text(text);

            ImGui.SetCursorPosX(originalXPos);
        }

        private void RightAlignedText(string text)
        {
            var originalXPos = ImGui.GetCursorPosX();
            float windowWidth = ImGui.GetWindowWidth();
            float textWidth = ImGui.CalcTextSize(text).X;
            float centeredXPos = windowWidth - textWidth - ImGui.GetStyle().WindowPadding.X;

            ImGui.SetCursorPosX(centeredXPos);
            ImGui.Text(text);

            ImGui.SetCursorPosX(originalXPos);
        }


        private float CenterCursor(float sizeXOfComp)
        {
            var originalXPos = ImGui.GetCursorPosX();
            var windowSize = ImGui.GetWindowSize();
            float centeredXPos = (windowSize.X - sizeXOfComp) / 2.0f;

            ImGui.SetCursorPosX(centeredXPos);

            return originalXPos;
        }
        private float RightAlignCursor(float sizeXOfComp)
        {
            var originalXPos = ImGui.GetCursorPosX();
            float windowWidth = ImGui.GetWindowWidth();
            float centeredXPos = windowWidth - sizeXOfComp - ImGui.GetStyle().WindowPadding.X;

            ImGui.SetCursorPosX(centeredXPos);

            return originalXPos;
        }
        #endregion

        public void SelectItem(object? selectedObject, EditorData editorData, int instIndex = -1)
        {
            if (editorData.selectedItem != null)
            {
                if (editorData.selectedItem is Object o)
                {
                    o.isSelected = false;
                }
            }

            ClearBuffers();

            if (selectedObject == null)
            {
                editorData.selectedItem = null;
                editorData.instIndex = -1;
                return;
            }

            justSelectedItem = true;
            editorData.selectedItem = selectedObject;
            if (instIndex != -1)
                editorData.instIndex = instIndex;

            ((ISelectable)selectedObject).isSelected = true;

            if (selectedObject is Object castedO)
            {
                if (castedO.GetComponent<BaseMesh>() is BaseMesh mesh)
                {
                    mesh.recalculate = true;
                    mesh.RecalculateModelMatrix(new bool[] { true, true, true });
                }

                if(castedO.GetComponent<Physics>() is Physics physics)
                {
                    physics.UpdatePhysxPositionAndRotation(castedO.transformation);
                }
            }
            //TODO pointlight and particle system selection
        }

        public void EditorWindow(ref EditorData editorData, 
                                 KeyboardState keyboardState, MouseState mouseState, Engine engine)
        {
            GameWindowProperty gameWindow = editorData.gameWindow;
            if(editorData.recalculateObjects)
            {
                CalculateObjectList();
                editorData.recalculateObjects = false;
            }

            var io = ImGui.GetIO();
            int anyObjectHovered = -1;

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
            style.Colors[(int)ImGuiCol.PopupBg] = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f); // RGBA
            style.WindowRounding = 5f;
            style.PopupRounding = 5f;

            TopPanelWithMenubar(ref gameWindow, ref style);

            GameWindowFrame(ref gameWindow);

            ManipulationGizmosMenu(ref gameWindow, ref style);

            LeftPanel(ref gameWindow, ref style, ref keyboardState);

            LeftPanelSeperator(ref gameWindow, ref style);

            RightPanel(ref gameWindow, ref style, ref keyboardState);

            RightPanelSeperator(ref gameWindow, ref style);

            BottomAssetPanelSeperator(ref gameWindow, ref style);

            BottomAssetPanel(ref gameWindow, ref style, ref keyboardState, ref mouseState);

            BottomPanel(ref gameWindow, ref style);

            #region Every frame variable updates
            if (isObjectHovered != anyObjectHovered)
                isObjectHovered = anyObjectHovered;

            if (justSelectedItem)
                justSelectedItem = false;

            if (mouseTypes[0] || mouseTypes[1])
                editorData.mouseType = MouseCursor.HResize;
            else if (mouseTypes[2])
                editorData.mouseType = MouseCursor.VResize;
            else
                editorData.mouseType = MouseCursor.Default;
            #endregion

            #region Debug
            //ImGui.SetNextWindowSize(new System.Numerics.Vector2(480, 300));
            //if (ImGui.Begin("DebugPanel"))
            //{
            //    int sizeX = 10;
            //    int sizeY = 20;
            //    ImGui.SliderInt("AnimMatrix", ref editorData.animType, 0, 7);
            //    ImGui.SameLine();
            //    if (ImGui.Button("<1", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if(editorData.animType > 0) editorData.animType--;
            //    }
            //    ImGui.SameLine();
            //    if (ImGui.Button(">1", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if(editorData.animType < 7) editorData.animType++;
            //    }
            //    ImGui.SliderInt("AnimEndMatrix", ref editorData.animEndType, 0, 1);
            //    ImGui.SameLine();
            //    if (ImGui.Button("<2", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if (editorData.animEndType == 1) editorData.animEndType--;
            //    }
            //    ImGui.SameLine();
            //    if (ImGui.Button(">2", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if (editorData.animEndType == 0) editorData.animEndType++;
            //    }
            //    ImGui.SliderInt("Matrix", ref editorData.matrixType, 0, 7);
            //    ImGui.SameLine();
            //    if (ImGui.Button("<3", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if (editorData.matrixType > 0) editorData.matrixType--;
            //    }
            //    ImGui.SameLine();
            //    if (ImGui.Button(">3", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if (editorData.matrixType < 7) editorData.matrixType++;
            //    }
            //}
            #endregion

        }

        private void DragDropImageSourceUI(ref TextureManager textureManager, string payloadName, string assetName, string payload, System.Numerics.Vector2 size)
        {
            if (ImGui.IsItemActive())
            {
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    byte[] pathBytes = Encoding.UTF8.GetBytes(payload);
                    unsafe
                    {
                        fixed (byte* pPath = pathBytes)
                        {
                            ImGui.SetDragDropPayload(payloadName, (IntPtr)pPath, (uint)pathBytes.Length);
                        }
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(size.X, 5));

                    if(textureManager.textures.ContainsKey("ui_" + assetName))
                        ImGui.Image((IntPtr)textureManager.textures["ui_" + assetName].TextureId, size);
                    else
                        ImGui.Image((IntPtr)textureManager.textures["ui_missing.png"].TextureId, size);
                    ImGui.TextWrapped(assetName);

                    ImGui.Dummy(new System.Numerics.Vector2(size.X, 5));

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

        public void FullscreenWindow(ref EditorData editorData)
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
