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
        public void TopPanelWithMenubar(ref ImGuiStylePtr style)
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, editorData.gameWindow.topPanelSize));
            ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero, ImGuiCond.Always);
            if (ImGui.Begin("TopPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.MenuBar |
                                        ImGuiWindowFlags.NoScrollbar))
            {
                MenuBar();

                GamePlayBar(ref style);
            }
            if (ImGui.IsWindowHovered())
                editorData.uiHasMouse = true;
            ImGui.End();
        }
    }
}
