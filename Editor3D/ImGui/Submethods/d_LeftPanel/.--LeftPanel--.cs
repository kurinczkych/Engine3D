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
        public void LeftPanel(ref ImGuiStylePtr style, ref KeyboardState keyboardState)
        {
            if (keyboardState.IsKeyReleased(Keys.Delete) && editorData.selectedItem != null && editorData.selectedItem is Object objectDelete)
            {
                engine.RemoveObject(objectDelete);
                editorData.recalculateObjects = true;
                SelectItem(null, editorData);
                // Todo: particle and lights
            }

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth * editorData.gameWindow.leftPanelPercent,
                                                                _windowHeight - editorData.gameWindow.topPanelSize - editorData.gameWindow.bottomPanelSize - (_windowHeight * editorData.gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, editorData.gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("LeftPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if (ImGui.BeginTabBar("MyTabs"))
                {
                    if (ImGui.BeginTabItem("Objects"))
                    {
                        var windowPadding = style.WindowPadding;
                        var popupRounding = style.PopupRounding;
                        style.WindowPadding = new System.Numerics.Vector2(style.WindowPadding.X, style.WindowPadding.X);
                        style.PopupRounding = 2f;
                        ObjectManagingMenu();
                        style.WindowPadding = windowPadding;
                        style.PopupRounding = popupRounding;

                        ObjectShowing(ref style, windowPadding, popupRounding);

                        ImGui.EndTabItem();
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
