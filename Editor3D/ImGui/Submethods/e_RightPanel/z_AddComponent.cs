using Engine3D;
using ImGuiNET;
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
        public void AddComponent(ref Object o)
        {
            float addCompOrig = CenterCursor(100);
            if (ImGui.Button("Add Component", new System.Numerics.Vector2(100, 20)))
            {
                showAddComponentWindow = true;
                var buttonPos = ImGui.GetItemRectMin();
                var buttonSize = ImGui.GetItemRectSize();
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(seperatorSize + _windowWidth * (1 - editorData.gameWindow.rightPanelPercent), buttonPos.Y + buttonSize.Y));
                ImGui.SetNextWindowSize(new System.Numerics.Vector2((_windowWidth * editorData.gameWindow.rightPanelPercent - seperatorSize) - 20,
                                                                    200));
            }
            ImGui.SetCursorPosX(addCompOrig);

            if (showAddComponentWindow)
            {
                System.Numerics.Vector2 componentWindowPos = ImGui.GetWindowPos();
                System.Numerics.Vector2 componentWindowSize = ImGui.GetWindowSize();
                if (ImGui.Begin("Component Selection", ref showAddComponentWindow, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
                {
                    componentWindowPos = ImGui.GetWindowPos();
                    componentWindowSize = ImGui.GetWindowSize();

                    ImGui.BeginChild("ComponentList", new System.Numerics.Vector2(0, 150), true);
                    ImGui.InputText("Search", ref searchQueryAddComponent, 256);

                    HashSet<string> alreadyGotBase = new HashSet<string>();
                    foreach (IComponent c in o.components)
                    {
                        Type cType = c.GetType();
                        if (cType.BaseType != null && cType.BaseType.Name == "BaseMesh")
                        {
                            alreadyGotBase.Add("BaseMesh");
                        }
                        if (cType.BaseType != null && cType.BaseType.Name == "Light")
                        {
                            alreadyGotBase.Add("Light");
                        }
                    }
                    HashSet<string> alreadyGotClass = new HashSet<string>();
                    foreach (IComponent c in o.components)
                    {
                        var cType = c.GetType();
                        alreadyGotClass.Add(cType.Name);
                    }

                    filteredComponents = availableComponents
                        .Where(component => component.name.ToLower().Contains(searchQueryAddComponent.ToLower()) &&
                                            !alreadyGotBase.Contains(component.baseClass) &&
                                            !alreadyGotClass.Contains(component.name))
                        .ToList();

                    foreach (var component in filteredComponents)
                    {
                        if (ImGui.Button(component.name))
                        {
                            if (component.name == "Physics")
                            {
                                if (o.GetComponent<BaseMesh>() == null)
                                {
                                    Engine.consoleManager.AddLog("Can't add physics to an object that doesn't have a mesh!", LogType.Warning);
                                    showAddComponentWindow = false;
                                    break;
                                }

                                object[] args = new object[]
                                {
                                                        editorData.physx
                                };
                                object? comp = Activator.CreateInstance(component.type, args);
                                if (comp == null)
                                    continue;

                                o.components.Add((IComponent)comp);
                            }
                            if (component.name == "PointLight")
                            {
                                o.components.Add(new PointLight(o, 0, engine.wireVao, engine.wireVbo, engine.onlyPosShaderProgram.programId, engine.windowSize, ref engine.mainCamera_));
                                engine.lights = new List<Light>();
                            }
                            if (component.name == "DirectionalLight")
                            {
                                o.components.Add(new DirectionalLight(o, 0, engine.wireVao, engine.wireVbo, engine.onlyPosShaderProgram.programId, engine.windowSize, ref engine.mainCamera_));
                                engine.lights = new List<Light>();
                            }
                            if (component.name == "ParticleSystem")
                            {
                                o.components.Add(new ParticleSystem(engine.instancedMeshVao, engine.instancedMeshVbo, engine.instancedShaderProgram.programId,
                                                                    engine.windowSize, ref engine.mainCamera_, ref o));

                                engine.particleSystems = new List<ParticleSystem>();
                            }


                            showAddComponentWindow = false;
                        }
                    }
                    ImGui.EndChild();



                    ImGui.End();
                }

                System.Numerics.Vector2 mousePos = ImGui.GetMousePos();
                bool isMouseOutsideWindow = mousePos.X < componentWindowPos.X || mousePos.X > componentWindowPos.X + componentWindowSize.X ||
                                            mousePos.Y < componentWindowPos.Y || mousePos.Y > componentWindowPos.Y + componentWindowSize.Y;

                if (ImGui.IsMouseClicked(0) && isMouseOutsideWindow)
                {
                    showAddComponentWindow = false;
                }
            }
        }
    }
}
