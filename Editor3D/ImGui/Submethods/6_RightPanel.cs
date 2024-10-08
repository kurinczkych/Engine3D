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
        public void RightPanel(ref GameWindowProperty gameWindow, ref ImGuiStylePtr style, ref KeyboardState keyboardState)
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth * gameWindow.rightPanelPercent - seperatorSize,
                                                                _windowHeight - gameWindow.topPanelSize - gameWindow.bottomPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(seperatorSize + _windowWidth * (1 - gameWindow.rightPanelPercent), gameWindow.topPanelSize), ImGuiCond.Always);
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
                                    Encoding.UTF8.GetBytes(o.transformation.Position.X.ToString(), 0, o.transformation.Position.X.ToString().Length, _inputBuffers["##positionX"], 0);
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
                                    Encoding.UTF8.GetBytes(o.transformation.Position.Y.ToString(), 0, o.transformation.Position.Y.ToString().Length, _inputBuffers["##positionY"], 0);
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
                                    Encoding.UTF8.GetBytes(o.transformation.Position.Z.ToString(), 0, o.transformation.Position.Z.ToString().Length, _inputBuffers["##positionZ"], 0);
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
                                    Encoding.UTF8.GetBytes(rotation.X.ToString(), 0, rotation.X.ToString().Length, _inputBuffers["##rotationX"], 0);
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
                                    Encoding.UTF8.GetBytes(rotation.Y.ToString(), 0, rotation.Y.ToString().Length, _inputBuffers["##rotationY"], 0);
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
                                    Encoding.UTF8.GetBytes(rotation.Z.ToString(), 0, rotation.Z.ToString().Length, _inputBuffers["##rotationZ"], 0);
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
                                    Encoding.UTF8.GetBytes(o.transformation.Scale.X.ToString(), 0, o.transformation.Scale.X.ToString().Length, _inputBuffers["##scaleX"], 0);
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
                                    Encoding.UTF8.GetBytes(o.transformation.Scale.Y.ToString(), 0, o.transformation.Scale.Y.ToString().Length, _inputBuffers["##scaleY"], 0);
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
                                    Encoding.UTF8.GetBytes(o.transformation.Scale.Z.ToString(), 0, o.transformation.Scale.Z.ToString().Length, _inputBuffers["##scaleZ"], 0);
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

                                //-----------------------------------------------------------

                                ImGui.PushFont(default20);
                                CenteredText("Components");
                                ImGui.PopFont();

                                List<IComponent> toRemoveComp = new List<IComponent>();
                                foreach (IComponent c in o.components)
                                {
                                    if (c is BaseMesh baseMesh)
                                    {
                                        ImGui.Separator();
                                        ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                                        if (ImGui.TreeNode("Mesh"))
                                        {
                                            #region Mesh
                                            if (baseMesh.GetType() == typeof(Mesh))
                                            {
                                                if (ImGui.Checkbox("##useBVH", ref o.useBVH))
                                                {
                                                    if (o.useBVH)
                                                    {
                                                        o.BuildBVH();
                                                    }
                                                    else
                                                    {
                                                        baseMesh.BVHStruct = null;
                                                    }
                                                }
                                                ImGui.SameLine();
                                                ImGui.Text("Use BVH for rendering");
                                            }

                                            ImGui.Checkbox("##useShading", ref baseMesh.useShading);
                                            ImGui.SameLine();
                                            ImGui.Text("Use shading");

                                            ImGui.Separator();

                                            Encoding.UTF8.GetBytes(baseMesh.modelName, 0, baseMesh.modelName.ToString().Length, _inputBuffers["##meshPath"], 0);
                                            ImGui.InputText("##meshPath", _inputBuffers["##meshPath"], (uint)_inputBuffers["##meshPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                            {
                                                baseMesh.modelName = "";
                                                _inputBuffers["##meshPath"][0] = 0;
                                            }

                                            if (ImGui.BeginDragDropTarget())
                                            {
                                                ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("MESH_NAME");
                                                unsafe
                                                {
                                                    if (payload.NativePtr != null)
                                                    {
                                                        byte[] pathBytes = new byte[payload.DataSize];
                                                        System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                        baseMesh.modelPath = GetStringFromByte(pathBytes);
                                                        CopyDataToBuffer("##meshPath", Encoding.UTF8.GetBytes(baseMesh.modelPath));
                                                    }
                                                }
                                                ImGui.EndDragDropTarget();
                                            }
                                            #endregion

                                            ImGui.Separator();

                                            #region Textures
                                            ImGui.Text("Texture");
                                            Encoding.UTF8.GetBytes(baseMesh.textureName, 0, baseMesh.textureName.ToString().Length, _inputBuffers["##texturePath"], 0);
                                            ImGui.InputText("##texturePath", _inputBuffers["##texturePath"], (uint)_inputBuffers["##texturePath"].Length, ImGuiInputTextFlags.ReadOnly);
                                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureName != "")
                                            {
                                                baseMesh.textureName = "";
                                                ClearBuffer("##texturePath");
                                            }

                                            if (ImGui.BeginDragDropTarget())
                                            {
                                                ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                unsafe
                                                {
                                                    if (payload.NativePtr != null)
                                                    {
                                                        byte[] pathBytes = new byte[payload.DataSize];
                                                        System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                        baseMesh.textureName = GetStringFromByte(pathBytes);
                                                        CopyDataToBuffer("##texturePath", Encoding.UTF8.GetBytes(baseMesh.textureName));
                                                    }
                                                }
                                                ImGui.EndDragDropTarget();
                                            }

                                            if (ImGui.TreeNode("Custom textures"))
                                            {
                                                ImGui.Text("Normal Texture");
                                                Encoding.UTF8.GetBytes(baseMesh.textureNormalName, 0, baseMesh.textureNormalName.ToString().Length, _inputBuffers["##textureNormalPath"], 0);
                                                ImGui.InputText("##textureNormalPath", _inputBuffers["##textureNormalPath"], (uint)_inputBuffers["##textureNormalPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureNormalName != "")
                                                {
                                                    baseMesh.textureNormalName = "";
                                                    ClearBuffer("##textureNormalPath");
                                                }

                                                if (ImGui.BeginDragDropTarget())
                                                {
                                                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                    unsafe
                                                    {
                                                        if (payload.NativePtr != null)
                                                        {
                                                            byte[] pathBytes = new byte[payload.DataSize];
                                                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                            baseMesh.textureNormalName = GetStringFromByte(pathBytes);
                                                            CopyDataToBuffer("##textureNormalPath", Encoding.UTF8.GetBytes(baseMesh.textureNormalName));
                                                        }
                                                    }
                                                    ImGui.EndDragDropTarget();
                                                }
                                                ImGui.Text("Height Texture");
                                                Encoding.UTF8.GetBytes(baseMesh.textureHeightName, 0, baseMesh.textureHeightName.ToString().Length, _inputBuffers["##textureHeightPath"], 0);
                                                ImGui.InputText("##textureHeightPath", _inputBuffers["##textureHeightPath"], (uint)_inputBuffers["##textureHeightPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureHeightName != "")
                                                {
                                                    baseMesh.textureHeightName = "";
                                                    ClearBuffer("##textureHeightPath");
                                                }

                                                if (ImGui.BeginDragDropTarget())
                                                {
                                                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                    unsafe
                                                    {
                                                        if (payload.NativePtr != null)
                                                        {
                                                            byte[] pathBytes = new byte[payload.DataSize];
                                                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                            baseMesh.textureHeightName = GetStringFromByte(pathBytes);
                                                            CopyDataToBuffer("##textureHeightPath", Encoding.UTF8.GetBytes(baseMesh.textureHeightName));
                                                        }
                                                    }
                                                    ImGui.EndDragDropTarget();
                                                }
                                                ImGui.Text("AO Texture");
                                                Encoding.UTF8.GetBytes(baseMesh.textureAOName, 0, baseMesh.textureAOName.ToString().Length, _inputBuffers["##textureAOPath"], 0);
                                                ImGui.InputText("##textureAOPath", _inputBuffers["##textureAOPath"], (uint)_inputBuffers["##textureAOPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureAOName != "")
                                                {
                                                    baseMesh.textureAOName = "";
                                                    ClearBuffer("##textureAOPath");
                                                }

                                                if (ImGui.BeginDragDropTarget())
                                                {
                                                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                    unsafe
                                                    {
                                                        if (payload.NativePtr != null)
                                                        {
                                                            byte[] pathBytes = new byte[payload.DataSize];
                                                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                            baseMesh.textureAOName = GetStringFromByte(pathBytes);
                                                            CopyDataToBuffer("##textureAOPath", Encoding.UTF8.GetBytes(baseMesh.textureAOName));
                                                        }
                                                    }
                                                    ImGui.EndDragDropTarget();
                                                }
                                                ImGui.Text("Rough Texture");
                                                Encoding.UTF8.GetBytes(baseMesh.textureRoughName, 0, baseMesh.textureRoughName.ToString().Length, _inputBuffers["##textureRoughPath"], 0);
                                                ImGui.InputText("##textureRoughPath", _inputBuffers["##textureRoughPath"], (uint)_inputBuffers["##textureRoughPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureName != "")
                                                {
                                                    baseMesh.textureRoughName = "";
                                                    ClearBuffer("##textureRoughPath");
                                                }

                                                if (ImGui.BeginDragDropTarget())
                                                {
                                                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                    unsafe
                                                    {
                                                        if (payload.NativePtr != null)
                                                        {
                                                            byte[] pathBytes = new byte[payload.DataSize];
                                                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                            baseMesh.textureRoughName = GetStringFromByte(pathBytes);
                                                            CopyDataToBuffer("##textureRoughPath", Encoding.UTF8.GetBytes(baseMesh.textureRoughName));
                                                        }
                                                    }
                                                    ImGui.EndDragDropTarget();
                                                }
                                                ImGui.Text("Metal Texture");
                                                Encoding.UTF8.GetBytes(baseMesh.textureMetalName, 0, baseMesh.textureMetalName.ToString().Length, _inputBuffers["##textureMetalPath"], 0);
                                                ImGui.InputText("##textureMetalPath", _inputBuffers["##textureMetalPath"], (uint)_inputBuffers["##textureMetalPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureMetalName != "")
                                                {
                                                    baseMesh.textureMetalName = "";
                                                    ClearBuffer("##textureMetalPath");
                                                }

                                                if (ImGui.BeginDragDropTarget())
                                                {
                                                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                    unsafe
                                                    {
                                                        if (payload.NativePtr != null)
                                                        {
                                                            byte[] pathBytes = new byte[payload.DataSize];
                                                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                            baseMesh.textureMetalName = GetStringFromByte(pathBytes);
                                                            CopyDataToBuffer("##textureMetalPath", Encoding.UTF8.GetBytes(baseMesh.textureMetalName));
                                                        }
                                                    }
                                                    ImGui.EndDragDropTarget();
                                                }

                                                ImGui.TreePop();
                                            }
                                            #endregion

                                            ImGui.TreePop();
                                        }
                                    }

                                    if (c is Physics physics)
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

                                    if (c is Light light)
                                    {
                                        ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                                        if (ImGui.TreeNode("Light"))
                                        {
                                            ImGui.SameLine();
                                            var origDeleteX = RightAlignCursor(70);
                                            ImGui.PushStyleColor(ImGuiCol.Button, style.Colors[(int)ImGuiCol.FrameBg]);
                                            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                                            if (ImGui.Button("Delete", new System.Numerics.Vector2(70, 20)))
                                            {
                                                toRemoveComp.Add(c);
                                                engine.lights = new List<Light>();
                                            }
                                            ImGui.PopStyleVar();
                                            ImGui.PopStyleColor();
                                            ImGui.SetCursorPosX(origDeleteX);
                                            ImGui.Dummy(new System.Numerics.Vector2(0, 0));

                                            #region Light
                                            LightType lightType = light.GetLightType();
                                            ImGui.Text("Type");
                                            ImGui.SameLine();
                                            ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 0.0f);
                                            if (ImGui.BeginCombo("##lightType", lightType.ToString()))
                                            {
                                                ImGui.Dummy(new System.Numerics.Vector2(0, 5));
                                                foreach (LightType newLightType in Enum.GetValues(typeof(LightType)))
                                                {
                                                    if (ImGui.Selectable(newLightType.ToString()))
                                                    {
                                                        light.SetLightType(newLightType);
                                                    }
                                                }
                                                ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                                                ImGui.EndCombo();
                                            }

                                            if (lightType == LightType.DirectionalLight)
                                            {
                                                Color4 color = light.GetColorC4();
                                                ColorPicker("Color", ref color);
                                                light.SetColor(color);

                                                float[] ambientVec = new float[] { light.ambient.X, light.ambient.Y, light.ambient.Z };
                                                Vector3 ambient = InputFloat3("Ambient", new string[] { "X", "Y", "Z" }, ambientVec, ref keyboardState);
                                                if (light.ambient != ambient)
                                                    light.ambient = ambient;

                                                float[] diffuseVec = new float[] { light.diffuse.X, light.diffuse.Y, light.diffuse.Z };
                                                Vector3 diffuse = InputFloat3("Diffuse", new string[] { "X", "Y", "Z" }, diffuseVec, ref keyboardState);
                                                if (light.diffuse != diffuse)
                                                    light.diffuse = diffuse;

                                                float[] specularVec = new float[] { light.specular.X, light.specular.Y, light.specular.Z };
                                                Vector3 specular = InputFloat3("Specular", new string[] { "X", "Y", "Z" }, specularVec, ref keyboardState);
                                                if (light.specular != specular)
                                                    light.specular = specular;

                                                float[] specularPowVec = new float[] { light.specularPow };
                                                float specularPow = InputFloat1("SpecularPow", new string[] { "X" }, specularPowVec, ref keyboardState);
                                                if (light.specularPow != specularPow)
                                                    light.specularPow = specularPow;
                                            }
                                            else if (lightType == LightType.PointLight)
                                            {
                                                Color4 color = light.GetColorC4();
                                                ColorPicker("Color", ref color);
                                                light.SetColor(color);

                                                float[] ambientVec = new float[] { light.ambient.X, light.ambient.Y, light.ambient.Z };
                                                Vector3 ambient = InputFloat3("Ambient", new string[] { "X", "Y", "Z" }, ambientVec, ref keyboardState);
                                                if (light.ambient != ambient)
                                                    light.ambient = ambient;

                                                float[] diffuseVec = new float[] { light.diffuse.X, light.diffuse.Y, light.diffuse.Z };
                                                Vector3 diffuse = InputFloat3("Diffuse", new string[] { "X", "Y", "Z" }, diffuseVec, ref keyboardState);
                                                if (light.diffuse != diffuse)
                                                    light.diffuse = diffuse;

                                                float[] specularVec = new float[] { light.specular.X, light.specular.Y, light.specular.Z };
                                                Vector3 specular = InputFloat3("Specular", new string[] { "X", "Y", "Z" }, specularVec, ref keyboardState);
                                                if (light.specular != specular)
                                                    light.specular = specular;

                                                float[] specularPowVec = new float[] { light.specularPow };
                                                float specularPow = InputFloat1("SpecularPow", new string[] { "X" }, specularPowVec, ref keyboardState);
                                                if (light.specularPow != specularPow)
                                                    light.specularPow = specularPow;


                                                ImGui.Checkbox("Advanced", ref advancedLightSetting);

                                                if (!advancedLightSetting)
                                                {
                                                    float[] rangeVec = new float[] { light.range };
                                                    float range = InputFloat1("Range", new string[] { "" }, specularPowVec, ref keyboardState);
                                                    if (light.range != range)
                                                    {
                                                        light.range = range;
                                                        float[] att = Light.RangeToAttenuation(range);
                                                        light.constant = att[0];
                                                        light.linear = att[1];
                                                        light.quadratic = att[2];
                                                    }
                                                }
                                                else
                                                {
                                                    ImGui.Text("Constant Linear Quadratic");
                                                    float[] pointVec = new float[] { light.constant, light.linear, light.quadratic };
                                                    Vector3 point = InputFloat3("Point", new string[] { "Constant", "Linear", "Quadratic" }, pointVec, ref keyboardState, true);
                                                    if (light.constant != point[0])
                                                        light.constant = point[0];
                                                    if (light.linear != point[1])
                                                        light.linear = point[1];
                                                    if (light.quadratic != point[2])
                                                        light.quadratic = point[2];
                                                }
                                            }

                                            ImGui.PopStyleVar();
                                            #endregion

                                            ImGui.TreePop();
                                        }
                                    }

                                    if (c is ParticleSystem ps)
                                    {
                                        ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                                        if (ImGui.TreeNode("Particle System"))
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

                                            #region ParticleSystem

                                            float[] emitTimeSecVec = new float[] { ps.emitTimeSec };
                                            float emitTimeSec = InputFloat1("Emit Time(sec)", new string[] { "" }, emitTimeSecVec, ref keyboardState);
                                            if (ps.emitTimeSec != emitTimeSec)
                                                ps.emitTimeSec = emitTimeSec;

                                            ImGui.Separator();
                                            ImGui.Checkbox("##randomLifeTime", ref ps.randomLifeTime);
                                            ImGui.SameLine();
                                            ImGui.Text("Random lifetime");

                                            if (!ps.randomLifeTime)
                                            {
                                                float[] lifetimeVec = new float[] { ps.lifetime };
                                                float lifetime = InputFloat1("Lifetime(sec)", new string[] { "" }, lifetimeVec, ref keyboardState, titleSameLine: true);
                                                if (ps.lifetime != lifetime)
                                                    ps.lifetime = lifetime;
                                            }
                                            else
                                            {
                                                float[] lifetimeVec = new float[] { ps.xLifeTime, ps.yLifeTime };
                                                float[] lifetime = InputFloat2("Lifetime(sec)", new string[] { "From", "To" }, lifetimeVec, ref keyboardState);
                                                if (ps.xLifeTime != lifetime[0])
                                                    ps.xLifeTime = lifetime[0];
                                                if (ps.yLifeTime != lifetime[1])
                                                    ps.yLifeTime = lifetime[1];
                                            }

                                            ImGui.Separator();
                                            ImGui.Checkbox("##randomStartPos", ref ps.randomStartPos);
                                            ImGui.SameLine();
                                            ImGui.Text("Random starting position");

                                            if (!ps.randomStartPos)
                                            {
                                                float[] startPosVec = new float[] { ps.startPos.X, ps.startPos.Y, ps.startPos.Z };
                                                Vector3 startPos = InputFloat3("Starting position", new string[] { "X", "Y", "Z" }, startPosVec, ref keyboardState);
                                                if (ps.startPos != startPos)
                                                    ps.startPos = startPos;
                                            }
                                            else
                                            {
                                                float[] startPosMinVec = new float[] { ps.xStartPos.Min.X, ps.xStartPos.Min.Y, ps.xStartPos.Min.Z };
                                                Vector3 startPosMin = InputFloat3("Starting min 3D corner", new string[] { "X", "Y", "Z" }, startPosMinVec, ref keyboardState);
                                                if (ps.xStartPos.Min != startPosMin)
                                                    ps.xStartPos.Min = startPosMin;

                                                float[] startPosMaxVec = new float[] { ps.xStartPos.Max.X, ps.xStartPos.Max.Y, ps.xStartPos.Max.Z };
                                                Vector3 startPosMax = InputFloat3("Starting max 3D corner", new string[] { "X", "Y", "Z" }, startPosMaxVec, ref keyboardState);
                                                if (ps.xStartPos.Max != startPosMax)
                                                    ps.xStartPos.Max = startPosMax;
                                            }

                                            ImGui.Separator();
                                            ImGui.Checkbox("##randomStartDir", ref ps.randomDir);
                                            ImGui.SameLine();
                                            ImGui.Text("Random starting direction");

                                            if (!ps.randomDir)
                                            {
                                                float[] startDirVec = new float[] { ps.startDir.X, ps.startDir.Y, ps.startDir.Z };
                                                Vector3 startDir = InputFloat3("Starting direction", new string[] { "X", "Y", "Z" }, startDirVec, ref keyboardState);
                                                if (ps.startDir != startDir)
                                                    ps.startDir = startDir;
                                            }

                                            ImGui.Separator();
                                            ImGui.Checkbox("##randomSpeed", ref ps.randomSpeed);
                                            ImGui.SameLine();
                                            ImGui.Text("Random speed");

                                            if (!ps.randomSpeed)
                                            {
                                                float[] speedVec = new float[] { ps.startSpeed, ps.endSpeed };
                                                float[] speed = InputFloat2("", new string[] { "Start", "End" }, speedVec, ref keyboardState);
                                                if (ps.startSpeed != speed[0])
                                                    ps.startSpeed = speed[0];
                                                if (ps.endSpeed != speed[1])
                                                    ps.endSpeed = speed[1];
                                            }
                                            else
                                            {
                                                float[] speedStartVec = new float[] { ps.xStartSpeed, ps.yStartSpeed };
                                                float[] speedStart = InputFloat2("Start speed", new string[] { "From", "To" }, speedStartVec, ref keyboardState);
                                                if (ps.xStartSpeed != speedStart[0])
                                                    ps.xStartSpeed = speedStart[0];
                                                if (ps.yStartSpeed != speedStart[1])
                                                    ps.yStartSpeed = speedStart[1];

                                                float[] speedEndVec = new float[] { ps.xEndSpeed, ps.yEndSpeed };
                                                float[] speedEnd = InputFloat2("End speed", new string[] { "From", "To" }, speedEndVec, ref keyboardState);
                                                if (ps.xEndSpeed != speedEnd[0])
                                                    ps.xEndSpeed = speedEnd[0];
                                                if (ps.yEndSpeed != speedEnd[1])
                                                    ps.yEndSpeed = speedEnd[1];
                                            }

                                            ImGui.Separator();
                                            ImGui.Checkbox("##randomScale", ref ps.randomScale);
                                            ImGui.SameLine();
                                            ImGui.Text("Random scale");

                                            if (!ps.randomScale)
                                            {
                                                float[] startScaleVec = new float[] { ps.startScale.X, ps.startScale.Y, ps.startScale.Z };
                                                Vector3 startScale = InputFloat3("Starting scale", new string[] { "X", "Y", "Z" }, startScaleVec, ref keyboardState);
                                                if (ps.startScale != startScale)
                                                    ps.startScale = startScale;

                                                float[] endScaleVec = new float[] { ps.endScale.X, ps.endScale.Y, ps.endScale.Z };
                                                Vector3 endScale = InputFloat3("Ending scale", new string[] { "X", "Y", "Z" }, endScaleVec, ref keyboardState);
                                                if (ps.endScale != endScale)
                                                    ps.endScale = endScale;
                                            }
                                            else
                                            {
                                                float[] startXScaleMinVec = new float[] { ps.xStartScale.Min.X, ps.xStartScale.Min.Y, ps.xStartScale.Min.Z };
                                                Vector3 startXScaleMin = InputFloat3("Starting scale min 3D corner", new string[] { "X", "Y", "Z" }, startXScaleMinVec, ref keyboardState);
                                                if (ps.xStartScale.Min != startXScaleMin)
                                                    ps.xStartScale.Min = startXScaleMin;

                                                float[] startXScaleMaxVec = new float[] { ps.xStartScale.Max.X, ps.xStartScale.Max.Y, ps.xStartScale.Max.Z };
                                                Vector3 startPosMax = InputFloat3("Starting scale max 3D corner", new string[] { "X", "Y", "Z" }, startXScaleMaxVec, ref keyboardState);
                                                if (ps.xStartScale.Max != startPosMax)
                                                    ps.xStartScale.Max = startPosMax;

                                                float[] endXScaleMinVec = new float[] { ps.xEndScale.Min.X, ps.xStartScale.Min.Y, ps.xStartScale.Min.Z };
                                                Vector3 endXScaleMin = InputFloat3("Ending scale min 3D corner", new string[] { "X", "Y", "Z" }, endXScaleMinVec, ref keyboardState);
                                                if (ps.xStartScale.Min != endXScaleMin)
                                                    ps.xStartScale.Min = endXScaleMin;

                                                float[] endXScaleMaxVec = new float[] { ps.xStartScale.Max.X, ps.xStartScale.Max.Y, ps.xStartScale.Max.Z };
                                                Vector3 endPosMax = InputFloat3("Ending scale max 3D corner", new string[] { "X", "Y", "Z" }, endXScaleMaxVec, ref keyboardState);
                                                if (ps.xStartScale.Max != endPosMax)
                                                    ps.xStartScale.Max = endPosMax;
                                            }

                                            ImGui.Separator();
                                            ImGui.Checkbox("##randomColor", ref ps.randomColor);
                                            ImGui.SameLine();
                                            ImGui.Text("Random color");

                                            if (!ps.randomColor)
                                            {
                                                Color4 startColor = ps.startColor;
                                                ColorPicker("Start color", ref startColor);
                                                ps.startColor = startColor;

                                                Color4 endColor = ps.endColor;
                                                ColorPicker("Ending color", ref endColor);
                                                ps.endColor = endColor;
                                            }

                                            //Helper.QuaternionFromEuler
                                            ImGui.Separator();
                                            ImGui.Checkbox("##randomRotation", ref ps.randomRotation);
                                            ImGui.SameLine();
                                            ImGui.Text("Random rotation");

                                            if (!ps.randomRotation)
                                            {
                                                Vector3 startRotPS = Helper.EulerFromQuaternion(ps.startRotation);
                                                float[] startRotVec = new float[] { startRotPS.X, startRotPS.Y, startRotPS.Z };
                                                Vector3 startRot = InputFloat3("Starting rotation", new string[] { "X", "Y", "Z" }, startRotVec, ref keyboardState);
                                                OpenTK.Mathematics.Quaternion quatStartRot = Helper.QuaternionFromEuler(startRot);
                                                if (ps.startRotation != quatStartRot)
                                                    ps.startRotation = quatStartRot;

                                                Vector3 endRotPS = Helper.EulerFromQuaternion(ps.endRotation);
                                                float[] endRotVec = new float[] { endRotPS.X, endRotPS.Y, endRotPS.Z };
                                                Vector3 endRot = InputFloat3("Ending rotation", new string[] { "X", "Y", "Z" }, endRotVec, ref keyboardState);
                                                OpenTK.Mathematics.Quaternion quatEndRot = Helper.QuaternionFromEuler(endRot);
                                                if (ps.startRotation != quatEndRot)
                                                    ps.startRotation = quatEndRot;
                                            }

                                            ImGui.Dummy(new System.Numerics.Vector2(0, 25));

                                            #endregion

                                            ImGui.TreePop();
                                        }

                                        #region ParticleSystem Update Window
                                        ImGui.SetNextWindowSize(particlesWindowSize);

                                        if (particlesWindowPos == null)
                                        {
                                            particlesWindowPos = new System.Numerics.Vector2(_windowWidth * (1 - gameWindow.rightPanelPercent) - particlesWindowSize.X,
                                                                 _windowHeight * (1 - gameWindow.bottomPanelPercent) - gameWindow.bottomPanelSize - particlesWindowSize.Y);
                                        }

                                        ImGui.SetNextWindowPos(particlesWindowPos ?? new System.Numerics.Vector2());
                                        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
                                        ImGui.PushStyleColor(ImGuiCol.TitleBg, style.Colors[(int)ImGuiCol.WindowBg]);
                                        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, style.Colors[(int)ImGuiCol.WindowBg]);
                                        ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, style.Colors[(int)ImGuiCol.WindowBg]);
                                        if (ImGui.Begin("Particles", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
                                        {
                                            ImGui.Separator();
                                            var button = style.Colors[(int)ImGuiCol.Button];
                                            style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                                            if (!editorData.runParticles)
                                            {
                                                ImGui.SetCursorPosX((ImGui.GetWindowSize().X / 2.0f) - 10);
                                                if (ImGui.ImageButton("##runParticlesStart", (IntPtr)engineData.textureManager.textures["ui_play.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                                                {
                                                    editorData.runParticles = true;
                                                    engine.SetRunParticles(true);
                                                }
                                            }
                                            else
                                            {
                                                ImGui.SetCursorPosX((ImGui.GetWindowSize().X / 2.0f) - 30);
                                                if (ImGui.ImageButton("##runParticlesEnd", (IntPtr)engineData.textureManager.textures["ui_stop.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                                                {
                                                    editorData.runParticles = false;
                                                    engine.SetRunParticles(false);
                                                    engine.ResetParticles();
                                                }
                                                ImGui.SameLine();
                                                if (ImGui.ImageButton("##runParticlesPause", (IntPtr)engineData.textureManager.textures["ui_pause.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                                                {
                                                    engine.SetRunParticles(false);
                                                    editorData.runParticles = false;
                                                }
                                            }
                                            style.Colors[(int)ImGuiCol.Button] = button;

                                            ImGui.Text("Particles: " + ps.GetParticleCount());

                                        }
                                        if (ImGui.IsWindowHovered())
                                            editorData.uiHasMouse = true;
                                        ImGui.End();
                                        ImGui.PopStyleColor();
                                        ImGui.PopStyleVar();
                                        #endregion
                                    }

                                    if (c is Camera cam)
                                    {
                                        ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                                        if (ImGui.TreeNode("Camera"))
                                        {

                                            #region Camera
                                            bool commit = false;

                                            int fov = (int)engine.mainCamera.fov;
                                            ImGui.Text("Field of View");
                                            ImGui.SameLine();
                                            ImGui.SliderInt("##fieldofview", ref fov, 0, 179);
                                            if ((int)engine.mainCamera.fov != fov)
                                            {
                                                if (fov == 0)
                                                    engine.mainCamera.fov = 0.0001f;
                                                else
                                                    engine.mainCamera.fov = fov;

                                                commit = true;
                                            }


                                            float[] clippingVec = new float[] { cam.near, cam.far };
                                            float[] clipping = InputFloat2("Clipping planes", new string[] { "Near", "Far" }, clippingVec, ref keyboardState);
                                            if (cam.near != clipping[0])
                                            {
                                                if (clipping[0] <= 0)
                                                    cam.near = 0.1f;
                                                else
                                                    cam.near = clipping[0];
                                                commit = true;
                                            }
                                            if (cam.far != clipping[1])
                                            {
                                                if (clipping[1] <= 0)
                                                    cam.far = 0.1f;
                                                else
                                                    cam.far = clipping[1];
                                                commit = true;
                                            }


                                            if (commit)
                                            {
                                                engine.mainCamera.UpdateAll();
                                            }

                                            #endregion

                                            ImGui.TreePop();
                                        }
                                    }

                                    ImGui.Separator();
                                }

                                foreach (IComponent c in toRemoveComp)
                                {
                                    o.DeleteComponent(c, ref engineData.textureManager);
                                    editorData.recalculateObjects = true;
                                }

                                #region Add Component
                                float addCompOrig = CenterCursor(100);
                                if (ImGui.Button("Add Component", new System.Numerics.Vector2(100, 20)))
                                {
                                    showAddComponentWindow = true;
                                    var buttonPos = ImGui.GetItemRectMin();
                                    var buttonSize = ImGui.GetItemRectSize();
                                    ImGui.SetNextWindowPos(new System.Numerics.Vector2(seperatorSize + _windowWidth * (1 - gameWindow.rightPanelPercent), buttonPos.Y + buttonSize.Y));
                                    ImGui.SetNextWindowSize(new System.Numerics.Vector2((_windowWidth * gameWindow.rightPanelPercent - seperatorSize) - 20,
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
                                                if (component.name == "Light")
                                                {
                                                    o.components.Add(new Light(o, engine.shaderProgram.id, 0, LightType.DirectionalLight));
                                                    engine.lights = new List<Light>();
                                                }
                                                if (component.name == "ParticleSystem")
                                                {
                                                    o.components.Add(new ParticleSystem(engine.instancedMeshVao, engine.instancedMeshVbo, engine.instancedShaderProgram.id,
                                                                                        engine.windowSize, ref engine.mainCamera, ref o));

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
                                #endregion

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
