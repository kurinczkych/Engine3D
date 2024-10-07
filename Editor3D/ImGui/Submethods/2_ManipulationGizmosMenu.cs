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
        public void ManipulationGizmosMenu(ref GameWindowProperty gameWindow, ref ImGuiStylePtr style)
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
                style.WindowRounding = 0;
                var frameBorderSize = style.FrameBorderSize;
                var framePadding = style.FramePadding;
                style.FrameBorderSize = 0;
                style.FramePadding = new System.Numerics.Vector2(2.5f, 2.5f);

                editorData.gizmoWindowSize = new Vector2(22.5f, yHeight);
                editorData.gizmoWindowPos = new Vector2(gameWindow.gameWindowPos.X + seperatorSize, gameWindow.topPanelSize);

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
                    if (editorData.gizmoManager.gizmoType == GizmoType.Move)
                    {
                        style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);
                    }
                    if (ImGui.ImageButton("##gizmo1", (IntPtr)Engine.textureManager.textures["ui_gizmo_move.png"].TextureId, imageSize))
                    {
                        if (editorData.gizmoManager.gizmoType != GizmoType.Move)
                            editorData.gizmoManager.gizmoType = GizmoType.Move;
                    }
                    if (editorData.gizmoManager.gizmoType == GizmoType.Move)
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
                    if (editorData.gizmoManager.AbsoluteMoving)
                    {
                        if (ImGui.ImageButton("##gizmoRelativeMove", (IntPtr)Engine.textureManager.textures["ui_absolute.png"].TextureId, imageSize))
                        {
                            editorData.gizmoManager.AbsoluteMoving = false;
                        }
                    }
                    else
                    {
                        if (ImGui.ImageButton("##gizmoAbsoluteMove", (IntPtr)Engine.textureManager.textures["ui_relative.png"].TextureId, imageSize))
                        {
                            editorData.gizmoManager.AbsoluteMoving = true;
                        }
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(5, 5));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5);
                        ImGui.BeginTooltip();
                        if (editorData.gizmoManager.AbsoluteMoving)
                            ImGui.Text("Set to relative moving");
                        else
                            ImGui.Text("Set to absolute moving");
                        ImGui.EndTooltip();
                        ImGui.PopStyleVar(2);
                    }
                    #endregion

                    #region Rotate gizmo button
                    ImGui.SetCursorPosX(0);
                    if (editorData.gizmoManager.gizmoType == GizmoType.Rotate)
                    {
                        style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);
                    }
                    if (ImGui.ImageButton("##gizmo2", (IntPtr)Engine.textureManager.textures["ui_gizmo_rotate.png"].TextureId, imageSize))
                    {
                        if (editorData.gizmoManager.gizmoType != GizmoType.Rotate)
                            editorData.gizmoManager.gizmoType = GizmoType.Rotate;
                    }
                    if (editorData.gizmoManager.gizmoType == GizmoType.Rotate)
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
                    if (editorData.gizmoManager.gizmoType == GizmoType.Scale)
                    {
                        style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);
                    }
                    if (ImGui.ImageButton("##gizmo3", (IntPtr)Engine.textureManager.textures["ui_gizmo_scale.png"].TextureId, imageSize))
                    {
                        if (editorData.gizmoManager.gizmoType != GizmoType.Scale)
                            editorData.gizmoManager.gizmoType = GizmoType.Scale;
                    }
                    if (editorData.gizmoManager.gizmoType == GizmoType.Scale)
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
                        if (editorData.gizmoManager.PerInstanceMove)
                            style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);

                        ImGui.SetCursorPosX(0);
                        if (ImGui.ImageButton("##gizmo4", (IntPtr)Engine.textureManager.textures["ui_missing.png"].TextureId, imageSize))
                        {
                            editorData.gizmoManager.PerInstanceMove = !editorData.gizmoManager.PerInstanceMove;
                        }
                        if (editorData.gizmoManager.PerInstanceMove)
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
                ImGui.PopStyleVar(2);
                style.WindowRounding = 5f;
                style.Colors[(int)ImGuiCol.Button] = button;
                style.FrameBorderSize = frameBorderSize;
                style.FramePadding = framePadding;
            }
        }
    }
}
