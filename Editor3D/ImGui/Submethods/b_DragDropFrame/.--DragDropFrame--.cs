using ImGuiNET;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void DragDropFrame()
        {
            float gwBorder = 5;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(editorData.gameWindow.gameWindowSize.X, editorData.gameWindow.gameWindowSize.Y / 2));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(editorData.gameWindow.gameWindowPos.X + seperatorSize, editorData.gameWindow.topPanelSize + editorData.gameWindow.gameWindowSize.Y / 2), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

            if (ImGui.Begin("DragDropFrame", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar |
                                               ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground/* | ImGuiWindowFlags.NoInputs*/))
            {
                ImGui.SetCursorPos(new System.Numerics.Vector2(gwBorder, gwBorder));
                ImGui.InvisibleButton("##invDragDropFrame", ImGui.GetContentRegionAvail() - new System.Numerics.Vector2(gwBorder * 2, gwBorder));
                if (ImGui.BeginDragDropTarget())
                {
                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("MESH_NAME");
                    unsafe
                    {
                        if (payload.NativePtr != null)
                        {
                            byte[] pathBytes = new byte[payload.DataSize];
                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);

                            engine.AddMeshObject(GetStringFromByte(pathBytes));
                            shouldOpenTreeNodeMeshes = true;
                            editorData.recalculateObjects = true;
                        }
                    }
                    ImGui.EndDragDropTarget();
                }

                ImGui.End();
            }

            ImGui.PopStyleVar(2);
        }
    }
}
