using Engine3D;
using ImGuiNET;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void TopPanelWithMenubar(ref GameWindowProperty gameWindow, ref ImGuiStylePtr style)
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, gameWindow.topPanelSize));
            ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero, ImGuiCond.Always);
            if (ImGui.Begin("TopPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.MenuBar |
                                        ImGuiWindowFlags.NoScrollbar))
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("File"))
                    {
                        if (ImGui.MenuItem("Open", "Ctrl+O"))
                        {
                            using (var dialog = new FolderBrowserDialog())
                            {
                                dialog.Description = "Select a folder";
                                dialog.UseDescriptionForTitle = true; // This applies only in some OS versions.

                                DialogResult result = dialog.ShowDialog();

                                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                                {
                                    engine.LoadScene(dialog.SelectedPath);
                                }
                                editorData.recalculateObjects = true;
                                engineData.gizmoManager = engine.GetGizmoManager();
                            }
                        }
                        if (ImGui.MenuItem("Save", "Ctrl+S"))
                        {
                            using (var dialog = new FolderBrowserDialog())
                            {
                                dialog.Description = "Select a folder";
                                dialog.UseDescriptionForTitle = true; // This applies only in some OS versions.

                                DialogResult result = dialog.ShowDialog();

                                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                                {
                                    engine.SaveScene(dialog.SelectedPath);
                                }
                            }
                        }
                    ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Window"))
                    {
                        if (ImGui.MenuItem("Reset panels"))
                        {
                            gameWindow.leftPanelPercent = gameWindow.origLeftPanelPercent;
                            gameWindow.rightPanelPercent = gameWindow.origRightPanelPercent;
                            gameWindow.bottomPanelPercent = gameWindow.origBottomPanelPercent;
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
                    if (ImGui.ImageButton("play", (IntPtr)engineData.textureManager.textures["ui_play.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Running;
                        editorData.justSetGameState = true;
                        engine.SetGameState(editorData.gameRunning);
                    }
                }
                else
                {
                    if (ImGui.ImageButton("stop", (IntPtr)engineData.textureManager.textures["ui_stop.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Stopped;
                        editorData.justSetGameState = true;
                        engine.SetGameState(editorData.gameRunning);
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
                    editorData.windowResized = true;
                }


                style.Colors[(int)ImGuiCol.Button] = button;
            }
            if (ImGui.IsWindowHovered())
                editorData.uiHasMouse = true;
            ImGui.End();
        }
    }
}
