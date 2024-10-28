using ImGuiNET;
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
        public void BottomAssetPanel(ref ImGuiStylePtr style, ref KeyboardState keyboardState, ref MouseState mouseState)
        {
            style.WindowRounding = 0f;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, _windowHeight * editorData.gameWindow.bottomPanelPercent - seperatorSize - 1));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight * (1 - editorData.gameWindow.bottomPanelPercent) + seperatorSize - editorData.gameWindow.bottomPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("BottomAssetPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if (ImGui.BeginTabBar("MyTabs"))
                {
                    float imageWidth = 100;
                    float imageHeight = 100;
                    System.Numerics.Vector2 imageSize = new System.Numerics.Vector2(imageWidth, imageHeight);

                    if (ImGui.BeginTabItem("Project"))
                    {
                        if (currentBottomPanelTab != "Project")
                            currentBottomPanelTab = "Project";

                        if (ImGui.BeginTabBar("Assets"))
                        {
                            Textures(ref style, ref imageSize);
                            Models(ref style, ref imageSize);
                            Audio(ref style, ref imageSize);

                            ImGui.EndTabBar();
                        }

                        ImGui.EndTabItem();
                    }

                    Console();

                    AssetStore(ref keyboardState, ref mouseState, ref imageSize);

                    if (Engine.consoleManager.justAdded)
                    {
                        ImGui.SetTabItemClosed("Project");
                        ImGui.SetTabItemClosed("Asset store");

                        Engine.consoleManager.justAdded = false;
                    }

                    ImGui.EndTabBar();
                }
            }
            if (ImGui.IsWindowHovered())
                editorData.uiHasMouse = true;
            ImGui.PopStyleVar();
            ImGui.End();
        }
    }
}
