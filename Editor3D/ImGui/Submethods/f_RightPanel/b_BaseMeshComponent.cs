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
        public void BaseMeshComponent(ref Object o, ref BaseMesh baseMesh)
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
    }
}
