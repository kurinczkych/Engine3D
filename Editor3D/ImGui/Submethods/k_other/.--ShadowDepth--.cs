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
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(200,245));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth * editorData.gameWindow.leftPanelPercent + seperatorSize, editorData.gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));  // Disable padding
            //ImGui.SetNextWindowCollapsed(true);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 255));
            if (ImGui.Begin("ShadowDepth", ImGuiWindowFlags.None))
            {
                if (ImGui.BeginTabBar("ShadowMaps"))
                {
                    if (ImGui.BeginTabItem("Small"))
                    {
                        int gameTexture = engine.GetShadowDepthTexture(ShadowType.Small);
                        ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Medium"))
                    {
                        int gameTexture = engine.GetShadowDepthTexture(ShadowType.Medium);
                        ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Large"))
                    {
                        int gameTexture = engine.GetShadowDepthTexture(ShadowType.Large);
                        ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
            }
            if (ImGui.IsWindowHovered())
                editorData.uiHasMouse = true;
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }
    }
}
