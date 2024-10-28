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
        public void ObjectManagingMenu()
        {
            if (ImGui.BeginPopupContextWindow("objectManagingMenu", ImGuiPopupFlags.MouseButtonRight))
            {
                if (ImGui.MenuItem("Empty Object"))
                {
                    engine.AddObject(ObjectType.Empty);
                    shouldOpenTreeNodeMeshes = true;
                    editorData.recalculateObjects = true;
                }
                if (ImGui.BeginMenu("3D Object"))
                {
                    if (ImGui.MenuItem("Cube"))
                    {
                        engine.AddObject(ObjectType.Cube);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                    }
                    if (ImGui.MenuItem("Sphere"))
                    {
                        engine.AddObject(ObjectType.Sphere);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                    }
                    if (ImGui.MenuItem("Capsule"))
                    {
                        engine.AddObject(ObjectType.Capsule);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                    }
                    if (ImGui.MenuItem("Plane"))
                    {
                        engine.AddObject(ObjectType.Plane);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                    }
                    if (ImGui.MenuItem("Mesh"))
                    {
                        engine.AddObject(ObjectType.TriangleMesh);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.MenuItem("Particle system"))
                {
                    engine.AddParticleSystem();
                    shouldOpenTreeNodeMeshes = true;
                    editorData.recalculateObjects = true;
                }
                if (ImGui.MenuItem("Audio emitter"))
                {
                    engine.AddObject(ObjectType.AudioEmitter);
                    shouldOpenTreeNodeMeshes = true;
                    editorData.recalculateObjects = true;
                }
                if (ImGui.BeginMenu("Lighting"))
                {
                    if (ImGui.MenuItem("Point Light"))
                    {
                        engine.AddLight(Light.LightType.PointLight);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                    }
                    if (ImGui.MenuItem("Directional Light"))
                    {
                        engine.AddLight(Light.LightType.DirectionalLight);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndPopup();
            }
        }
    }
}
