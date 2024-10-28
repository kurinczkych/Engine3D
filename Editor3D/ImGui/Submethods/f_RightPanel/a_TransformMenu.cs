using ImGuiNET;
using OpenTK.Mathematics;
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
        public void TransformMenu(ref Object o, ref KeyboardState keyboardState)
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Once);
            if (ImGui.TreeNode("Transform"))
            {
                BaseMesh? baseMesh = (BaseMesh?)o.GetComponent<BaseMesh>();
                bool commit = false;
                bool reset = false;
                ImGui.PushItemWidth(50);

                ImGui.Separator();

                #region Position
                ImGui.Text("Position");

                ImGui.Text("X");
                ImGui.SameLine();
                FillInputBuffer(o.transformation.Position.X.ToString(), "##positionX");
                commit = false;
                if (ImGui.InputText("##positionX", _inputBuffers["##positionX"], (uint)_inputBuffers["##positionX"].Length))
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer("##positionX");
                    float value = o.transformation.Position.X;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            o.transformation.Position = new Vector3(value, o.transformation.Position.Y, o.transformation.Position.Z);
                        else
                        {
                            ClearBuffer("##positionX");
                            o.transformation.Position = new Vector3(0, o.transformation.Position.Y, o.transformation.Position.Z);
                        }

                        if (baseMesh != null)
                        {
                            baseMesh.recalculate = true;
                            baseMesh.RecalculateModelMatrix(new bool[] { true, false, false });
                        }
                        if (o.GetComponent<Physics>() is Physics p)
                            p.UpdatePhysxPositionAndRotation(o.transformation);
                    }
                }
                ImGui.SameLine();

                ImGui.Text("Y");
                ImGui.SameLine();
                FillInputBuffer(o.transformation.Position.Y.ToString(), "##positionY");
                commit = false;
                if (ImGui.InputText("##positionY", _inputBuffers["##positionY"], (uint)_inputBuffers["##positionY"].Length))
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer("##positionY");
                    float value = o.transformation.Position.Y;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            o.transformation.Position = new Vector3(o.transformation.Position.X, value, o.transformation.Position.Z);
                        else
                        {
                            ClearBuffer("##positionY");
                            o.transformation.Position = new Vector3(o.transformation.Position.X, 0, o.transformation.Position.Z);
                        }

                        if (baseMesh != null)
                        {
                            baseMesh.recalculate = true;
                            baseMesh.RecalculateModelMatrix(new bool[] { true, false, false });
                        }
                        if (o.GetComponent<Physics>() is Physics p)
                            p.UpdatePhysxPositionAndRotation(o.transformation);
                    }
                }
                ImGui.SameLine();

                ImGui.Text("Z");
                ImGui.SameLine();
                FillInputBuffer(o.transformation.Position.Z.ToString(), "##positionZ");
                commit = false;
                if (ImGui.InputText("##positionZ", _inputBuffers["##positionZ"], (uint)_inputBuffers["##positionZ"].Length) && !justSelectedItem)
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer("##positionZ");
                    float value = o.transformation.Position.Z;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            o.transformation.Position = new Vector3(o.transformation.Position.X, o.transformation.Position.Y, value);
                        else
                        {
                            ClearBuffer("##positionZ");
                            o.transformation.Position = new Vector3(o.transformation.Position.X, o.transformation.Position.Y, 0);
                        }

                        if (baseMesh != null)
                        {
                            baseMesh.recalculate = true;
                            baseMesh.RecalculateModelMatrix(new bool[] { true, false, false });
                        }
                        if (o.GetComponent<Physics>() is Physics p)
                            p.UpdatePhysxPositionAndRotation(o.transformation);
                    }
                }
                #endregion

                ImGui.Separator();

                #region Rotation
                ImGui.Text("Rotation");

                Vector3 rotation = Helper.EulerFromQuaternion(o.transformation.Rotation);

                ImGui.Text("X");
                ImGui.SameLine();
                FillInputBuffer(rotation.X.ToString(), "##rotationX");
                commit = false;
                if (ImGui.InputText("##rotationX", _inputBuffers["##rotationX"], (uint)_inputBuffers["##rotationX"].Length, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer("##rotationX");
                    float value = rotation.X;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            rotation.X = value;
                        else
                        {
                            ClearBuffer("##rotationX");
                            rotation.X = 0;
                        }
                        o.transformation.Rotation = Helper.QuaternionFromEuler(rotation);

                        if (baseMesh != null)
                        {
                            baseMesh.recalculate = true;
                            baseMesh.RecalculateModelMatrix(new bool[] { false, true, false });
                        }
                        if (o.GetComponent<Physics>() is Physics p)
                            p.UpdatePhysxPositionAndRotation(o.transformation);
                    }
                }
                ImGui.SameLine();

                ImGui.Text("Y");
                ImGui.SameLine();
                FillInputBuffer(rotation.Y.ToString(), "##rotationY");
                commit = false;
                if (ImGui.InputText("##rotationY", _inputBuffers["##rotationY"], (uint)_inputBuffers["##rotationY"].Length, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer("##rotationY");
                    float value = rotation.Y;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            rotation.Y = value;
                        else
                        {
                            ClearBuffer("##rotationY");
                            rotation.Y = 0;
                        }
                        o.transformation.Rotation = Helper.QuaternionFromEuler(rotation);
                        if (baseMesh != null)
                        {
                            baseMesh.recalculate = true;
                            baseMesh.RecalculateModelMatrix(new bool[] { false, true, false });
                        }
                        if (o.GetComponent<Physics>() is Physics p)
                            p.UpdatePhysxPositionAndRotation(o.transformation);
                    }
                }
                ImGui.SameLine();

                ImGui.Text("Z");
                ImGui.SameLine();
                FillInputBuffer(rotation.Z.ToString(), "##rotationZ");
                commit = false;
                if (ImGui.InputText("##rotationZ", _inputBuffers["##rotationZ"], (uint)_inputBuffers["##rotationZ"].Length, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer("##rotationZ");
                    float value = rotation.Z;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            rotation.Z = value;
                        else
                        {
                            ClearBuffer("##rotationZ");
                            rotation.Z = 0;
                        }
                        o.transformation.Rotation = Helper.QuaternionFromEuler(rotation);
                        if (baseMesh != null)
                        {
                            baseMesh.recalculate = true;
                            baseMesh.RecalculateModelMatrix(new bool[] { false, true, false });
                        }
                        if (o.GetComponent<Physics>() is Physics p)
                            p.UpdatePhysxPositionAndRotation(o.transformation);
                    }
                }
                #endregion

                ImGui.Separator();

                #region Scale
                ImGui.Text("Scale");

                ImGui.Text("X");
                ImGui.SameLine();
                FillInputBuffer(o.transformation.Scale.X.ToString(), "##scaleX");
                commit = false;
                if (ImGui.InputText("##scaleX", _inputBuffers["##scaleX"], (uint)_inputBuffers["##scaleX"].Length))
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer("##scaleX");
                    float value = o.transformation.Scale.X;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            o.transformation.Scale.X = value;
                        else
                        {
                            ClearBuffer("##scaleX");
                            o.transformation.Scale.X = 1;
                        }

                        if (baseMesh != null)
                        {
                            baseMesh.recalculate = true;
                            baseMesh.RecalculateModelMatrix(new bool[] { false, false, true });
                        }
                        if (o.GetComponent<Physics>() is Physics p)
                            p.UpdatePhysxPositionAndRotation(o.transformation);
                    }
                }
                ImGui.SameLine();

                ImGui.Text("Y");
                ImGui.SameLine();
                FillInputBuffer(o.transformation.Scale.Y.ToString(), "##scaleY");
                commit = false;
                if (ImGui.InputText("##scaleY", _inputBuffers["##scaleY"], (uint)_inputBuffers["##scaleY"].Length))
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer("##scaleY");
                    float value = o.transformation.Scale.Y;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            o.transformation.Scale.Y = value;
                        else
                        {
                            ClearBuffer("##scaleY");
                            o.transformation.Scale.Y = 1;
                        }

                        if (baseMesh != null)
                        {
                            baseMesh.recalculate = true;
                            baseMesh.RecalculateModelMatrix(new bool[] { false, false, true });
                        }
                        if (o.GetComponent<Physics>() is Physics p)
                            p.UpdatePhysxPositionAndRotation(o.transformation);
                    }
                }
                ImGui.SameLine();

                ImGui.Text("Z");
                ImGui.SameLine();
                FillInputBuffer(o.transformation.Scale.Z.ToString(), "##scaleZ");
                commit = false;
                if (ImGui.InputText("##scaleZ", _inputBuffers["##scaleZ"], (uint)_inputBuffers["##scaleZ"].Length))
                {
                    commit = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    reset = true;
                    commit = true;
                }
                if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                    commit = true;

                if (commit)
                {
                    string valueStr = GetStringFromBuffer("##scaleZ");
                    float value = o.transformation.Scale.Z;
                    if (float.TryParse(valueStr, out value))
                    {
                        if (!reset)
                            o.transformation.Scale.Z = value;
                        else
                        {
                            ClearBuffer("##scaleZ");
                            o.transformation.Scale.Z = 1;
                        }

                        if (baseMesh != null)
                        {
                            baseMesh.recalculate = true;
                            baseMesh.RecalculateModelMatrix(new bool[] { false, false, true });
                        }
                        if (o.GetComponent<Physics>() is Physics p)
                            p.UpdatePhysxPositionAndRotation(o.transformation);
                    }
                }
                #endregion

                ImGui.Separator();

                ImGui.PopItemWidth();

                ImGui.TreePop();
            }
        }
    }
}
