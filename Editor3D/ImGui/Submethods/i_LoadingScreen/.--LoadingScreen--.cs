using ImGuiNET;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void LoadingScreen()
        {
            if (!engineData.assetManager.allLoaded)
            {
                editorData.uiHasMouse = true;

                ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0));
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(windowSize.X, windowSize.Y));
                ImGui.PushStyleColor(ImGuiCol.WindowBg, baseBGColor);
                ImGui.SetNextWindowFocus();

                if (ImGui.Begin("LoadingScreen", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove))
                {
                    ImGui.SetCursorPos(new System.Numerics.Vector2(0, 0));
                    ImGui.PushFont(default20);

                    var loadingSize = ImGui.CalcTextSize("Loading");
                    float loadingSizeX = (windowSize.X - loadingSize.X) * 0.5f;
                    float loadingSizeY = (windowSize.Y - loadingSize.Y) * 0.5f;

                    if (loadingSizeX > 0)
                        ImGui.SetCursorPosX(loadingSizeX);

                    if (loadingSizeY > 0)
                        ImGui.SetCursorPosY(loadingSizeY);

                    ImGui.Text("Loading");
                    ImGui.Spacing();

                    float loadingCount = engineData.assetManager.loaded.Count;
                    float loadingMax = engineData.assetManager.loaded.Count + engineData.assetManager.toLoadString.Count;
                    var progressBarWidth = 300.0f;
                    ImGui.SetCursorPosX((windowSize.X - progressBarWidth) * 0.5f);

                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, baseBGColor * 0.7f);
                    ImGui.ProgressBar(loadingCount/loadingMax, new System.Numerics.Vector2(progressBarWidth, 0), $"{(int)(loadingCount / loadingMax * 100)}%");
                    ImGui.PopStyleColor();

                    ImGui.PopFont();
                }

                ImGui.PopStyleColor();
            }
        }
    }
}
