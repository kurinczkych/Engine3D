using ImGuiNET;
using MagicPhysX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void PhysicsComponent(IComponent c, ref ImGuiStylePtr style, ref List<IComponent> toRemoveComp, ref Physics physics, ref Object o)
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Once);
            if (ImGui.TreeNode("Physics"))
            {
                ImGui.SameLine();
                var origDeleteX = RightAlignCursor(70);
                ImGui.PushStyleColor(ImGuiCol.Button, style.Colors[(int)ImGuiCol.FrameBg]);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                if (ImGui.Button("Delete", new System.Numerics.Vector2(70, 20)))
                {
                    toRemoveComp.Add(c);
                }
                ImGui.PopStyleVar();
                ImGui.PopStyleColor();
                ImGui.SetCursorPosX(origDeleteX);
                ImGui.Dummy(new System.Numerics.Vector2(0, 0));

                #region Physx
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5.0f);
                var buttonColor = style.Colors[(int)ImGuiCol.Button];
                style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f);

                if (o.GetComponent<Physics>() is Physics p && p.HasCollider)
                {
                    System.Numerics.Vector2 buttonSize = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 40);
                    if (ImGui.Button("Remove\n" + p.colliderStaticType + " " + p.colliderType + " Collider", buttonSize))
                    {
                        p.RemoveCollider();
                    }
                }
                else
                {
                    BaseMesh? baseMeshPhysics = (BaseMesh?)o.GetComponent<BaseMesh>();

                    System.Numerics.Vector2 buttonSize = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 20);
                    string[] options = { "Static", "Dynamic" };
                    for (int i = 0; i < options.Length; i++)
                    {
                        if (ImGui.RadioButton(options[i], physics.selectedColliderOption == i))
                        {
                            physics.selectedColliderOption = i;
                        }

                        if (i != options.Length - 1)
                            ImGui.SameLine();
                    }

                    if (physics.selectedColliderOption == 0)
                    {
                        if (ImGui.Button("Add Triangle Mesh Collider", buttonSize))
                        {
                            if (baseMeshPhysics == null)
                                throw new Exception("For a triangle mesh collider the object must have a mesh!");
                            physics.AddTriangleMeshCollider(o.transformation, baseMeshPhysics);
                        }
                        if (ImGui.Button("Add Cube Collider", buttonSize))
                        {
                            physics.AddCubeCollider(o.transformation, true);
                        }
                        if (ImGui.Button("Add Sphere Collider", buttonSize))
                        {
                            physics.AddSphereCollider(o.transformation, true);
                        }
                        if (ImGui.Button("Add Capsule Collider", buttonSize))
                        {
                            physics.AddCapsuleCollider(o.transformation, true);
                        }
                    }
                    else if (physics.selectedColliderOption == 1)
                    {
                        if (ImGui.Button("Add Cube Collider", buttonSize))
                        {
                            physics.AddCubeCollider(o.transformation, false);
                        }
                        if (ImGui.Button("Add Sphere Collider", buttonSize))
                        {
                            physics.AddSphereCollider(o.transformation, false);
                        }
                        if (ImGui.Button("Add Capsule Collider", buttonSize))
                        {
                            physics.AddCapsuleCollider(o.transformation, false);
                        }
                    }
                }
                style.Colors[(int)ImGuiCol.Button] = buttonColor;
                ImGui.PopStyleVar();
                #endregion

                ImGui.TreePop();
            }
        }
    }
}
