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
        public void Console()
        {
            if (ImGui.BeginTabItem("Console"))
            {
                if (currentBottomPanelTab != "Console")
                    currentBottomPanelTab = "Console";

                ImGui.PushFont(default18);
                foreach (Log log in Engine.consoleManager.Logs)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Engine.consoleManager.LogColors[log.logType]);
                    ImGui.TextWrapped(log.message);
                    ImGui.PopStyleColor();
                }
                ImGui.PopFont();
                ImGui.SetScrollHereY(1.0f);

                ImGui.SetCursorPosY(ImGui.GetWindowSize().Y - editorData.gameWindow.bottomPanelSize - 4);
                ImGui.Separator();
                ImGui.SetNextItemWidth(200);
                ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 0);
                if (ImGui.BeginCombo("##showConsoleTypeDropdown", showConsoleTypeList[showConsoleTypeListIndex]))
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0, 5));
                    for (int i = 0; i < showConsoleTypeList.Length; i++)
                    {
                        bool isSelected = (i == showConsoleTypeListIndex);

                        if (ImGui.Selectable(showConsoleTypeList[i], isSelected))
                        {
                            showConsoleTypeListIndex = i;
                            Engine.consoleManager.showConsoleType = (ShowConsoleType)Enum.Parse(typeof(ShowConsoleType), showConsoleTypeList[showConsoleTypeListIndex]);
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                    ImGui.EndCombo();
                }
                ImGui.PopStyleVar();

                ImGui.Dummy(new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 20));

                ImGui.EndTabItem();
            }
        }
    }
}
