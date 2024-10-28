using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Engine3D.Light;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void RightPanel(ref ImGuiStylePtr style, ref KeyboardState keyboardState)
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth * editorData.gameWindow.rightPanelPercent - seperatorSize,
                                                                _windowHeight - editorData.gameWindow.topPanelSize - editorData.gameWindow.bottomPanelSize - (_windowHeight * editorData.gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(seperatorSize + _windowWidth * (1 - editorData.gameWindow.rightPanelPercent), editorData.gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("RightPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if (ImGui.BeginTabBar("MyTabs"))
                {
                    if (ImGui.BeginTabItem("Inspector"))
                    {
                        if (editorData.selectedItem != null)
                        {
                            if (editorData.selectedItem is Object o)
                            {
                                if (ImGui.Checkbox("##isMeshEnabled", ref o.isEnabled))
                                {
                                    if (o.GetComponent<BaseMesh>() is BaseMesh enabledMesh)
                                        enabledMesh.recalculate = true;
                                }
                                ImGui.SameLine();

                                Encoding.UTF8.GetBytes(o.name, 0, o.name.Length, _inputBuffers["##name"], 0);
                                if (ImGui.InputText("##name", _inputBuffers["##name"], (uint)_inputBuffers["##name"].Length))
                                {
                                    o.name = GetStringFromBuffer("##name");
                                }

                                TransformMenu(ref o, ref keyboardState);

                                //-----------------------------------------------------------

                                ImGui.PushFont(default20);
                                CenteredText("Components");
                                ImGui.PopFont();

                                List<IComponent> toRemoveComp = new List<IComponent>();
                                foreach (IComponent c in o.components)
                                {
                                    if (c is BaseMesh baseMesh)
                                    {
                                        BaseMeshComponent(ref o, ref baseMesh);
                                    }

                                    if (c is Physics physics)
                                    {
                                        PhysicsComponent(c, ref style, ref toRemoveComp, ref physics, ref o);
                                    }

                                    if (c is Light light)
                                    {
                                        LightComponent(ref toRemoveComp, ref style, c, ref light);
                                    }

                                    if (c is ParticleSystem ps)
                                    {
                                        ParticleSystemComponent(ref toRemoveComp, ref style, ref ps, c);
                                    }

                                    if (c is Camera cam)
                                    {
                                        CameraComponent(ref cam);
                                    }

                                    ImGui.Separator();
                                }

                                foreach (IComponent c in toRemoveComp)
                                {
                                    o.DeleteComponent(c, ref engineData.textureManager);
                                    editorData.recalculateObjects = true;
                                }

                                AddComponent(ref o);

                                ImGui.Dummy(new System.Numerics.Vector2(0, 50));
                            }
                        }

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
