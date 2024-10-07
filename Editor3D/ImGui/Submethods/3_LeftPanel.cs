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
        public void LeftPanel(ref GameWindowProperty gameWindow, ref ImGuiStylePtr style, ref KeyboardState keyboardState)
        {
            if (keyboardState.IsKeyReleased(Keys.Delete) && editorData.selectedItem != null && editorData.selectedItem is Object objectDelete)
            {
                engine.RemoveObject(objectDelete);
                // Todo: particle and lights
            }

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth * gameWindow.leftPanelPercent,
                                                                _windowHeight - gameWindow.topPanelSize - gameWindow.bottomPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, gameWindow.topPanelSize), ImGuiCond.Always);
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
                        if (ImGui.BeginPopupContextWindow("objectManagingMenu", ImGuiPopupFlags.MouseButtonRight))
                        {
                            if (ImGui.MenuItem("Empty Object"))
                            {
                                engine.AddObject(ObjectType.Empty);
                                shouldOpenTreeNodeMeshes = true;
                            }
                            if (ImGui.BeginMenu("3D Object"))
                            {
                                if (ImGui.MenuItem("Cube"))
                                {
                                    engine.AddObject(ObjectType.Cube);
                                    shouldOpenTreeNodeMeshes = true;
                                }
                                if (ImGui.MenuItem("Sphere"))
                                {
                                    engine.AddObject(ObjectType.Sphere);
                                    shouldOpenTreeNodeMeshes = true;
                                }
                                if (ImGui.MenuItem("Capsule"))
                                {
                                    engine.AddObject(ObjectType.Capsule);
                                    shouldOpenTreeNodeMeshes = true;
                                }
                                if (ImGui.MenuItem("Plane"))
                                {
                                    engine.AddObject(ObjectType.Plane);
                                    shouldOpenTreeNodeMeshes = true;
                                }
                                if (ImGui.MenuItem("Mesh"))
                                {
                                    engine.AddObject(ObjectType.TriangleMesh);
                                    shouldOpenTreeNodeMeshes = true;
                                }

                                ImGui.EndMenu();
                            }
                            if (ImGui.MenuItem("Particle system"))
                            {
                                engine.AddParticleSystem();
                                shouldOpenTreeNodeMeshes = true;
                            }
                            if (ImGui.MenuItem("Audio emitter"))
                            {
                                engine.AddObject(ObjectType.AudioEmitter);
                                shouldOpenTreeNodeMeshes = true;
                            }
                            if (ImGui.BeginMenu("Lighting"))
                            {
                                if (ImGui.MenuItem("Point Light"))
                                {
                                    engine.AddLight(Light.LightType.PointLight);
                                    shouldOpenTreeNodeMeshes = true;
                                }
                                if (ImGui.MenuItem("Directional Light"))
                                {
                                    engine.AddLight(Light.LightType.DirectionalLight);
                                    shouldOpenTreeNodeMeshes = true;
                                }

                                ImGui.EndMenu();
                            }

                            ImGui.EndPopup();
                        }
                        style.WindowPadding = windowPadding;
                        style.PopupRounding = popupRounding;

                        if (editorData.objects.Count > 0)
                        {

                            if (shouldOpenTreeNodeMeshes)
                            {
                                ImGui.SetNextItemOpen(true, ImGuiCond.Once); // Open the tree node once.
                                shouldOpenTreeNodeMeshes = false; // Reset the flag so it doesn't open again automatically.
                            }

                            if (objects.Count > 0 && ImGui.TreeNode("Objects"))
                            {
                                ImGui.Separator();
                                for (int i = 0; i < objects.Count; i++)
                                {
                                    Object ro = objects[i];

                                    if (ImGui.Selectable(ro.displayName))
                                    {
                                        SelectItem(ro, editorData);
                                    }
                                    if (ImGui.IsItemHovered())
                                        anyObjectHovered = ro.id;

                                    if (isObjectHovered != -1 && isObjectHovered == ro.id)
                                    {
                                        style.WindowPadding = new System.Numerics.Vector2(style.WindowPadding.X, style.WindowPadding.X);
                                        style.PopupRounding = 2f;
                                        if (ImGui.BeginPopupContextWindow("objectManagingMenu", ImGuiPopupFlags.MouseButtonRight))
                                        {
                                            anyObjectHovered = ro.id;
                                            if (ImGui.MenuItem("Delete"))
                                            {
                                                engine.RemoveObject(ro);
                                            }

                                            ImGui.EndPopup();
                                        }
                                        style.WindowPadding = windowPadding;
                                        style.PopupRounding = popupRounding;
                                    }
                                }
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
            ImGui.PopStyleVar();
            ImGui.End();
        }
    }
}
