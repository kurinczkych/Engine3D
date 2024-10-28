using ImGuiNET;
using OpenTK.Mathematics;
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
        public void ManipulationGizmosMenu(ref ImGuiStylePtr style)
        {
            if (editorData.selectedItem != null && editorData.gameRunning == GameState.Stopped)
            {
                bool isInst = false;
                float yHeight = 174.5f - 34.5f;

                if (editorData.selectedItem is Object o && o.GetComponent<BaseMesh>() is BaseMesh bm && bm.GetType() == typeof(InstancedMesh))
                {
                    isInst = true;
                    yHeight = 174.5f;
                }

                var button = style.Colors[(int)ImGuiCol.Button];
                style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 0.6f);
                var windowRounding = style.WindowRounding;
                style.WindowRounding = 0;
                var frameBorderSize = style.FrameBorderSize;
                var framePadding = style.FramePadding;
                style.FrameBorderSize = 0;
                style.FramePadding = new System.Numerics.Vector2(2.5f, 2.5f);

                editorData.gizmoWindowSize = new Vector2(22.5f, yHeight);
                editorData.gizmoWindowPos = new Vector2(editorData.gameWindow.gameWindowPos.X + seperatorSize, editorData.gameWindow.topPanelSize);
                engine.SetGizmoWindow(editorData.gizmoWindowSize, editorData.gizmoWindowPos);

                ImGui.SetNextWindowSize(new System.Numerics.Vector2(editorData.gizmoWindowSize.X, editorData.gizmoWindowSize.Y));
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(editorData.gizmoWindowPos.X, editorData.gizmoWindowPos.Y), ImGuiCond.Always);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

                var origButton = style.Colors[(int)ImGuiCol.Button];
                var a = style.WindowPadding;
                if (ImGui.Begin("GizmosMenu", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar))
                {
                    var availableWidth = ImGui.GetContentRegionAvail().X;
                    System.Numerics.Vector2 imageSize = new System.Numerics.Vector2(32 - 5, 32 - 5);
                    var startXPos = (availableWidth - imageSize.X) * 0.5f;

                    #region Move gizmo button
                    ImGui.SetCursorPosX(0);
                    if (engineData.gizmoManager.gizmoType == GizmoType.Move)
                    {
                        style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);
                    }
                    if (ImGui.ImageButton("##gizmo1", (IntPtr)engineData.textureManager.textures["ui_gizmo_move.png"].TextureId, imageSize))
                    {
                        if (engineData.gizmoManager.gizmoType != GizmoType.Move)
                            engineData.gizmoManager.gizmoType = GizmoType.Move;
                    }
                    if (engineData.gizmoManager.gizmoType == GizmoType.Move)
                    {
                        style.Colors[(int)ImGuiCol.Button] = origButton;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(5, 5));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5);
                        ImGui.BeginTooltip();
                        ImGui.Text("Move tool");
                        ImGui.EndTooltip();
                        ImGui.PopStyleVar(2);
                    }
                    #endregion

                    #region Local/global move button
                    ImGui.SetCursorPosX(0);
                    if (engineData.gizmoManager.AbsoluteMoving)
                    {
                        if (ImGui.ImageButton("##gizmoRelativeMove", (IntPtr)engineData.textureManager.textures["ui_absolute.png"].TextureId, imageSize))
                        {
                            engineData.gizmoManager.AbsoluteMoving = false;
                        }
                    }
                    else
                    {
                        if (ImGui.ImageButton("##gizmoAbsoluteMove", (IntPtr)engineData.textureManager.textures["ui_relative.png"].TextureId, imageSize))
                        {
                            engineData.gizmoManager.AbsoluteMoving = true;
                        }
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(5, 5));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5);
                        ImGui.BeginTooltip();
                        if (engineData.gizmoManager.AbsoluteMoving)
                            ImGui.Text("Set to relative moving");
                        else
                            ImGui.Text("Set to absolute moving");
                        ImGui.EndTooltip();
                        ImGui.PopStyleVar(2);
                    }
                    #endregion

                    #region Rotate gizmo button
                    ImGui.SetCursorPosX(0);
                    if (engineData.gizmoManager.gizmoType == GizmoType.Rotate)
                    {
                        style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);
                    }
                    if (ImGui.ImageButton("##gizmo2", (IntPtr)engineData.textureManager.textures["ui_gizmo_rotate.png"].TextureId, imageSize))
                    {
                        if (engineData.gizmoManager.gizmoType != GizmoType.Rotate)
                            engineData.gizmoManager.gizmoType = GizmoType.Rotate;
                    }
                    if (engineData.gizmoManager.gizmoType == GizmoType.Rotate)
                    {
                        style.Colors[(int)ImGuiCol.Button] = origButton;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(5, 5));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5);
                        ImGui.BeginTooltip();
                        ImGui.Text("Rotate tool");
                        ImGui.EndTooltip();
                        ImGui.PopStyleVar(2);
                    }
                    #endregion

                    #region Scale gizmo button
                    ImGui.SetCursorPosX(0);
                    if (engineData.gizmoManager.gizmoType == GizmoType.Scale)
                    {
                        style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);
                    }
                    if (ImGui.ImageButton("##gizmo3", (IntPtr)engineData.textureManager.textures["ui_gizmo_scale.png"].TextureId, imageSize))
                    {
                        if (engineData.gizmoManager.gizmoType != GizmoType.Scale)
                            engineData.gizmoManager.gizmoType = GizmoType.Scale;
                    }
                    if (engineData.gizmoManager.gizmoType == GizmoType.Scale)
                    {
                        style.Colors[(int)ImGuiCol.Button] = origButton;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(5, 5));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5);
                        ImGui.BeginTooltip();
                        ImGui.Text("Scale tool");
                        ImGui.EndTooltip();
                        ImGui.PopStyleVar(2);
                    }
                    #endregion

                    #region Per instance button
                    if (isInst)
                    {
                        var button2 = style.Colors[(int)ImGuiCol.Button];
                        if (engineData.gizmoManager.PerInstanceMove)
                            style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);

                        ImGui.SetCursorPosX(0);
                        if (ImGui.ImageButton("##gizmo4", (IntPtr)engineData.textureManager.textures["ui_missing.png"].TextureId, imageSize))
                        {
                            engineData.gizmoManager.PerInstanceMove = !engineData.gizmoManager.PerInstanceMove;
                        }
                        if (engineData.gizmoManager.PerInstanceMove)
                            style.Colors[(int)ImGuiCol.Button] = button2;

                        if (ImGui.IsItemHovered())
                        {
                            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(5, 5));
                            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5);
                            ImGui.BeginTooltip();
                            ImGui.Text("Per instance manipulate");
                            ImGui.EndTooltip();
                            ImGui.PopStyleVar(2);
                        }
                    }
                    #endregion
                }
                if (ImGui.IsWindowHovered())
                    editorData.uiHasMouse = true;
                ImGui.PopStyleVar(2);
                style.WindowRounding = windowRounding;
                style.Colors[(int)ImGuiCol.Button] = button;
                style.FrameBorderSize = frameBorderSize;
                style.FramePadding = framePadding;
            }
        }
    }
}
