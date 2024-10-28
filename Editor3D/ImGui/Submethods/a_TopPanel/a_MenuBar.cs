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
        public void MenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Open", "Ctrl+O"))
                    {
                        using (var dialog = new OpenFileDialog())
                        {
                            dialog.Title = "Select a project";
                            dialog.Filter = "Project Files (*.proj)|*.proj";
                            dialog.Multiselect = false;

                            DialogResult result = dialog.ShowDialog();

                            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                            {
                                engine.LoadScene(dialog.FileName);
                            }
                            editorData.recalculateObjects = true;
                            engineData.gizmoManager = engine.GetGizmoManager();
                        }
                    }
                    if (ImGui.MenuItem("Save", "Ctrl+S"))
                    {
                        using (var dialog = new SaveFileDialog())
                        {
                            dialog.Title = "Save the project";
                            dialog.Filter = "Project Files (*.proj)|*.proj";

                            DialogResult result = dialog.ShowDialog();

                            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                            {
                                engine.SaveScene(dialog.FileName);
                            }
                        }
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Window"))
                {
                    if (ImGui.MenuItem("Reset panels"))
                    {
                        editorData.gameWindow.leftPanelPercent = editorData.gameWindow.origLeftPanelPercent;
                        editorData.gameWindow.rightPanelPercent = editorData.gameWindow.origRightPanelPercent;
                        editorData.gameWindow.bottomPanelPercent = editorData.gameWindow.origBottomPanelPercent;
                        editorData.windowResized = true;
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }
        }
    }
}
