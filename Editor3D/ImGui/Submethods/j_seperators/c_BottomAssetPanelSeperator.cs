using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void BottomAssetPanelSeperator(ref ImGuiStylePtr style)
        {
            style.WindowMinSize = new System.Numerics.Vector2(seperatorSize, seperatorSize);
            style.Colors[(int)ImGuiCol.WindowBg] = seperatorColor;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, seperatorSize));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight * (1 - editorData.gameWindow.bottomPanelPercent) - editorData.gameWindow.bottomPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
            if (ImGui.Begin("BottomAssetSeparator", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings |
                                         ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.InvisibleButton("##BottomSeparatorButton", new System.Numerics.Vector2(_windowWidth, seperatorSize));

                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        isResizingBottom = true;

                    editorData.mouseTypes[2] = true;
                }
                else
                    editorData.mouseTypes[2] = false;

                if (isResizingBottom)
                {
                    editorData.mouseTypes[2] = true;

                    float mouseY = ImGui.GetIO().MousePos.Y;
                    editorData.gameWindow.bottomPanelPercent = 1 - mouseY / (_windowHeight - editorData.gameWindow.bottomPanelSize - 5);
                    if (editorData.gameWindow.bottomPanelPercent < 0.05f)
                        editorData.gameWindow.bottomPanelPercent = 0.05f;
                    if (editorData.gameWindow.bottomPanelPercent > 0.70f)
                        editorData.gameWindow.bottomPanelPercent = 0.70f;

                    editorData.windowResized = true;

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        isResizingBottom = false;
                    }
                }
            }
            if (ImGui.IsWindowHovered())
                editorData.uiHasMouse = true;
            ImGui.End();
            ImGui.PopStyleVar(); // Pop the style for padding
            style.WindowMinSize = new System.Numerics.Vector2(32, 32);
            style.Colors[(int)ImGuiCol.WindowBg] = baseBGColor;
        }
    }
}
