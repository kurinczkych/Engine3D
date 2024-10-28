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
        public void BottomInfoPanel(ref ImGuiStylePtr style)
        {
            var windowBg = style.Colors[(int)ImGuiCol.WindowBg];
            style.Colors[(int)ImGuiCol.WindowBg] = style.Colors[(int)ImGuiCol.MenuBarBg];
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, editorData.gameWindow.bottomPanelSize + 4));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight - editorData.gameWindow.bottomPanelSize - 4), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("BottomInfoPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {

                string fpsStr = engine.GetFpsString();
                System.Numerics.Vector2 bottomPanelSize = ImGui.GetContentRegionAvail();
                System.Numerics.Vector2 textSize = ImGui.CalcTextSize(fpsStr);
                if (editorData.assetStoreManager.IsZipDownloadInProgress)
                {
                    ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                    ImGui.Text(editorData.assetStoreManager.ZipDownloadProgress);
                }
                else if (editorData.assetStoreManager.tryingToDownload)
                {
                    ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                    ImGui.Text("Starting the download...");
                }
                ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                ImGui.SetCursorPosX(bottomPanelSize.X - textSize.X);
                ImGui.Text(fpsStr);
            }
            if (ImGui.IsWindowHovered())
                editorData.uiHasMouse = true;
            style.Colors[(int)ImGuiCol.WindowBg] = windowBg;
        }
    }
}
