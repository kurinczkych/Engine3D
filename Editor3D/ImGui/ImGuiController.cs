using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable CS8602

namespace Engine3D
{
    
    public partial class ImGuiController : BaseImGuiController
    {
        private EditorData editorData;
        private EngineData engineData;
        private Engine engine;
        private Vector2i windowSize = new Vector2i();

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

        private KeyboardState keyboardState;
        private MouseState mouseState;

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

        public ImGuiController(int width, int height, ref Engine engine) : base(width, height)
        {
            windowSize = new Vector2i(width, height);

            editorData = new EditorData();
            editorData.gameWindow = new GameWindowProperty();
            editorData.gameWindow.gameWindowSize = new Vector2(width,height);
            editorData.gameWindow.gameWindowPos = new Vector2();
            engine.SetGameWindow(editorData.gameWindow);

            this.engine = engine;
            engineData = new EngineData();

            showConsoleTypeList = Enum.GetNames(typeof(ShowConsoleType));

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

            #region GetComponents
            Assembly engineAssembly = typeof(IComponent).Assembly;
            var types = engineAssembly.GetTypes();

            foreach (var type in types)
            {
                if (type.IsClass && typeof(IComponent).IsAssignableFrom(type) && !exludedComponents.Contains(type.Name))
                {
                    availableComponents.Add(new ComponentType(type.Name, type.BaseType?.Name, type));
                }
            }

            filteredComponents = new List<ComponentType>(availableComponents);
            #endregion

            #region Input boxes
            _inputBuffers.Add("##name", new byte[100]);

            _inputBuffers.Add("##positionX", new byte[100]);
            _inputBuffers.Add("##positionY", new byte[100]);
            _inputBuffers.Add("##positionZ", new byte[100]);
            _inputBuffers.Add("##rotationX", new byte[200]);
            _inputBuffers.Add("##rotationY", new byte[200]);
            _inputBuffers.Add("##rotationZ", new byte[200]);
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
        private void FillInputBuffer(string value, string bufferName)
        {
            ClearBuffer(bufferName);
            Encoding.UTF8.GetBytes(value, 0, value.Length, _inputBuffers[bufferName], 0);
        }
        #endregion

        #region MainMethods
        public void OnLoad()
        {
            engine.SubscribeToResizeEvent(OnResize);
            engine.SubscribeToObjectSelectedEvent(ObjectSelected);

            keyboardState = engine.GetKeyboardState();
            mouseState = engine.GetMouseState();

            engine.AddRenderMethod(OnRender);
            engine.AddUpdateMethod(OnUpdate);
            engine.AddUnloadMethod(OnUnload);
            engine.AddCharInputMethod(OnTextInput);
            engine.AddMouseWheelInputMethod(OnMouseWheel);

            engineData.assetManager = engine.GetAssetManager();
            engineData.textureManager = engine.GetTextureManager();
            editorData.assetStoreManager = new AssetStoreManager(ref engineData.assetManager);
            engineData.gizmoManager = engine.GetGizmoManager();
            editorData.physx = engine.physx;

            keyboardState = engine.GetKeyboardState();
            mouseState = engine.GetMouseState();

            engineData.objects = engine.GetObjects();

            currentTextureAssetFolder = engineData.assetManager.assets.folders[FileType.Textures.ToString()];
            currentModelAssetFolder = engineData.assetManager.assets.folders[FileType.Models.ToString()];
            currentAudioAssetFolder = engineData.assetManager.assets.folders[FileType.Audio.ToString()];
        }

        public void OnRender(FrameEventArgs args)
        {
            Update(engine, (float)args.Time);

            if (!editorData.isGameFullscreen)
                EditorWindow(ref editorData);
            else
                FullscreenWindow(ref editorData);

            Render();
        }

        public void OnUpdate(FrameEventArgs args)
        {
            editorData.assetStoreManager.DownloadIfNeeded();

            if (editorData.gameRunning == GameState.Stopped &&
                   editorData.justSetGameState)
            {
                editorData.manualCursor = false;
                editorData.justSetGameState = false;
            }

            if (editorData.gameRunning == GameState.Running && engine.GetCursorState() != CursorState.Grabbed && !editorData.manualCursor)
            {
                engine.SetCursorState(CursorState.Grabbed);
            }
            else if (editorData.gameRunning == GameState.Stopped && engine.GetCursorState() != CursorState.Normal)
            {
                engine.SetCursorState(CursorState.Normal);
            }

            if (keyboardState.IsKeyReleased(Keys.F5))
            {
                editorData.gameRunning = editorData.gameRunning == GameState.Stopped ? GameState.Running : GameState.Stopped;
                engine.SetGameState(editorData.gameRunning);
            }

            if (keyboardState.IsKeyReleased(Keys.F2))
            {
                if (engine.GetCursorState() == CursorState.Normal)
                {
                    engine.SetCursorState(CursorState.Grabbed);
                    editorData.manualCursor = false;
                }
                else if (engine.GetCursorState() == CursorState.Grabbed)
                {
                    engine.SetCursorState(CursorState.Normal);
                    editorData.manualCursor = true;
                }
            }

            if (editorData.windowResized)
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

                engine.ResizedEditorWindow(editorData.gameWindow.gameWindowSize, editorData.gameWindow.gameWindowPos);
            }
            if (engine.GetCursor() != editorData.mouseType)
                engine.SetCursor(editorData.mouseType);
        }

        public void OnUnload()
        {
            editorData.assetStoreManager.DeleteFolderContent("Temp");
            editorData.assetStoreManager.Delete();
        }

        public void ObjectSelected(Engine3D.Object? o, int inst)
        {
            SelectItem(o, editorData, inst);
        }

        public void OnResize(ResizeEventArgs e)
        {
            windowSize.X = e.Width;
            windowSize.Y = e.Height;

            editorData.gameWindow.gameWindowSize = new Vector2(windowSize.X * (1.0f - (editorData.gameWindow.leftPanelPercent + editorData.gameWindow.rightPanelPercent)),
                                                            windowSize.Y * (1 - editorData.gameWindow.bottomPanelPercent) - editorData.gameWindow.topPanelSize - editorData.gameWindow.bottomPanelSize);
            editorData.gameWindow.gameWindowPos = new Vector2(windowSize.X * editorData.gameWindow.leftPanelPercent, windowSize.Y * editorData.gameWindow.bottomPanelPercent + editorData.gameWindow.bottomPanelSize);

            engine.ResizedEditorWindow(editorData.gameWindow.gameWindowSize, editorData.gameWindow.gameWindowPos);

            WindowResized(windowSize.X, windowSize.Y);
        }

        protected void OnTextInput(TextInputEventArgs e)
        {
            PressChar((char)e.Unicode);
        }

        protected void OnMouseWheel(MouseWheelEventArgs e)
        {
            MouseScroll(e.Offset);
        }
        #endregion

        #region Other
        public void CalculateObjectList()
        {
            Dictionary<string, int> names = new Dictionary<string, int>();

            for (int i = 0; i < engineData.objects.Count; i++)
            {
                string name = engineData.objects[i].name == "" ? "Object " + i.ToString() : engineData.objects[i].name;
                if (engineData.objects[i].GetComponent<BaseMesh>() != null)
                {
                    if (engineData.objects[i].GetComponent<BaseMesh>() is InstancedMesh)
                        name += " (Instanced)";
                }

                if (names.ContainsKey(name))
                    names[name] += 1;
                else
                    names[name] = 0;

                if (names[name] == 0)
                    engineData.objects[i].displayName = name;
                else
                    engineData.objects[i].displayName = name + " (" + names[name] + ")";
            }
        }

        public bool CursorInImGuiWindow(Vector2 cursor)
        {
            System.Numerics.Vector2 pos = particlesWindowPos ?? new System.Numerics.Vector2();
            bool isInHorizontalBounds = cursor.X >= pos.X && cursor.X <= (pos.X + particlesWindowSize.X);

            bool isInVerticalBounds = cursor.Y >= pos.Y && cursor.Y <= (pos.Y + particlesWindowSize.Y);

            return isInHorizontalBounds && isInVerticalBounds;
        }
        #endregion

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

        #region SelectItem
        public void SelectItem(Object? selectedObject, EditorData editorData, int instIndex = -1)
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
                engine.SetSelectedObject(null);
                engine.SetGizmoInstIndex(-1);
                return;
            }

            shouldOpenTreeNodeMeshes = true;

            justSelectedItem = true;
            editorData.selectedItem = selectedObject;
            engine.SetSelectedObject(selectedObject);
            if (instIndex != -1)
                engine.SetGizmoInstIndex(instIndex);

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
        #endregion

        public void EditorWindow(ref EditorData editorData)
        {
            if(editorData.recalculateObjects)
            {
                CalculateObjectList();
                editorData.recalculateObjects = false;
            }

            editorData.io = ImGui.GetIO();
            editorData.uiHasMouse = false;

            editorData.anyObjectHovered = -1;

            if (editorData.gameRunning == GameState.Running && !editorData.manualCursor)
                editorData.io.ConfigFlags |= ImGuiConfigFlags.NoMouse;
            else
                editorData.io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;

            editorData.windowResized = false;

            var style = ImGui.GetStyle();

            TopPanelWithMenubar(ref style);

            DragDropFrame();

            ManipulationGizmosMenu(ref style);

            LeftPanel(ref style, ref keyboardState);

            LeftPanelSeperator(ref style);

            RightPanel(ref style, ref keyboardState);

            RightPanelSeperator(ref style);

            BottomAssetPanelSeperator(ref style);

            BottomAssetPanel(ref style, ref keyboardState, ref mouseState);

            BottomInfoPanel(ref style);

            engine.SetUIHasMouse(editorData.uiHasMouse);

            SceneView();

            #region Every frame variable updates
            if (isObjectHovered != editorData.anyObjectHovered)
                isObjectHovered = editorData.anyObjectHovered;

            if (justSelectedItem)
                justSelectedItem = false;

            if (editorData.mouseTypes[0] || editorData.mouseTypes[1])
                editorData.mouseType = MouseCursor.HResize;
            else if (editorData.mouseTypes[2])
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
                    if (ImGui.ImageButton("play", (IntPtr)engineData.textureManager.textures["ui_play.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Running;
                        editorData.justSetGameState = true;
                    }
                }
                else
                {
                    if (ImGui.ImageButton("stop", (IntPtr)engineData.textureManager.textures["ui_stop.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Stopped;
                        editorData.justSetGameState = true;
                    }
                }

                ImGui.SameLine();
                if (editorData.gameRunning == GameState.Running)
                {
                    if (ImGui.ImageButton("pause", (IntPtr)engineData.textureManager.textures["ui_pause.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.isPaused = !editorData.isPaused;
                    }
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("screen", (IntPtr)engineData.textureManager.textures["ui_screen.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                {
                    editorData.isGameFullscreen = !editorData.isGameFullscreen;
                }
            }
            ImGui.End();

            return;
        }
    }
}
