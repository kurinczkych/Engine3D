using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
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

    public class EditorProperties
    {
        public bool windowResized = false;
        public MouseCursor mouseType = MouseCursor.Default;

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

        private bool isResizingLeft = false;
        private bool isResizingRight = false;
        private bool isResizingBottom = false;
        private float seperatorSize = 5;
        private System.Numerics.Vector4 baseBGColor = new System.Numerics.Vector4(0.352f, 0.352f, 0.352f, 1.0f);
        private System.Numerics.Vector4 seperatorColor = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);

        public ImGuiController(int width, int height, ref EditorProperties editorProperties) : base(width, height)
        {
            this.editorProperties = editorProperties;
        }

        public void EditorWindow(Engine.GameWindowProperty gameWindow, Dictionary<string, Texture> guiTextures)
        {
            var io = ImGui.GetIO();

            if (editorProperties.gameRunning == GameState.Running)
                io.ConfigFlags |= ImGuiConfigFlags.NoMouse;
            else
                io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;

            editorProperties.windowResized = false;
            bool[] mouseTypes = new bool[3];

            // Remove gray border
            var style = ImGui.GetStyle();
            style.WindowBorderSize = 0.5f; // Removes the window border
            style.Colors[(int)ImGuiCol.WindowBg] = baseBGColor;
            style.Colors[(int)ImGuiCol.Border] = new System.Numerics.Vector4(0, 0, 0, 1.0f);
            style.Colors[(int)ImGuiCol.Tab] = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            style.Colors[(int)ImGuiCol.TabActive] = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            style.Colors[(int)ImGuiCol.TabHovered] = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonHovered] = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonActive] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1.0f);
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

                float totalWidth = 40;  // Total width of the three buttons
                float spaceBetweenButtons = 2;  // Gap between buttons
                float buttonWidth = (totalWidth - 1 * spaceBetweenButtons) / 2;  // Width of each button

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


                style.Colors[(int)ImGuiCol.Button] = button;
            }
            ImGui.End();
            #endregion

            #region Left panel
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
                        ImGui.Text("Content for Tab 1");

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
                        ImGui.Text("Content for Tab 1");

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

            #region Return
            if (mouseTypes[0] || mouseTypes[1])
                editorProperties.mouseType = MouseCursor.HResize;
            else if (mouseTypes[2])
                editorProperties.mouseType = MouseCursor.VResize;
            else
                editorProperties.mouseType = MouseCursor.Default;
            #endregion
        }

    }
}
