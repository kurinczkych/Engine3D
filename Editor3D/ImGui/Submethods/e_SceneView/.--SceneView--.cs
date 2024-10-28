using ImGuiNET;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void SceneView()
        {
            int gameTexture = engine.GetGameViewportTexture();

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));  // Disable padding
            //ImGui.SetNextWindowSize(new System.Numerics.Vector2(editorData.gameWindow.gameWindowSize.X - 20, editorData.gameWindow.gameWindowSize.Y - 20));
            //ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth * gameWindow.leftPanelPercent + seperatorSize + 10, gameWindow.topPanelSize + 10), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(editorData.gameWindow.gameWindowSize.X - seperatorSize, editorData.gameWindow.gameWindowSize.Y));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth * editorData.gameWindow.leftPanelPercent + seperatorSize, editorData.gameWindow.topPanelSize), ImGuiCond.Always);
            if (ImGui.Begin("SceneView", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                ImGui.SetCursorPos(new System.Numerics.Vector2(0, 0));
                ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(editorData.gameWindow.gameWindowSize.X, editorData.gameWindow.gameWindowSize.Y), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                ImGui.End();
            }
            ImGui.PopStyleVar();
        }
    }
}
