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
        public void ShadowDepth()
        {
            int gameTexture = engine.GetShadowDepthTexture();

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(200,200));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth * editorData.gameWindow.leftPanelPercent + seperatorSize, editorData.gameWindow.topPanelSize), ImGuiCond.Always);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(255, 0, 0, 255));
            if (ImGui.Begin("ShadowDepth", ImGuiWindowFlags.None))
            {
                ImGui.SetCursorPos(new System.Numerics.Vector2(0, 0));
                ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
            }
            ImGui.PopStyleColor();
        }
    }
}
