using ImGuiNET;
using OpenTK.Windowing.Desktop;
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
        public void ShadowDepth()
        {
            if (editorData.selectedItem != null && editorData.selectedItem.HasComponent<Light>())
            {
                Light? light = (Light?)editorData.selectedItem.GetComponent<Light>();
                if (light == null)
                    return;

                ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 245));
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth * editorData.gameWindow.leftPanelPercent + seperatorSize, editorData.gameWindow.topPanelSize), ImGuiCond.Always);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));  // Disable padding
                                                                                                     //ImGui.SetNextWindowCollapsed(true);
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 255));
                if (ImGui.Begin("ShadowDepth", ImGuiWindowFlags.None))
                {
                    if (ImGui.BeginTabBar("ShadowMaps"))
                    {
                        if (light is DirectionalLight dl)
                        {
                            if (ImGui.BeginTabItem("Small"))
                            {
                                int gameTexture = engine.GetShadowDepthTexture(ShadowType.Small, light);
                                ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Medium"))
                            {
                                int gameTexture = engine.GetShadowDepthTexture(ShadowType.Medium, light);
                                ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Large"))
                            {
                                int gameTexture = engine.GetShadowDepthTexture(ShadowType.Large, light);
                                ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                                ImGui.EndTabItem();
                            }
                        }
                        else if (light is PointLight pl)
                        {
                            if (ImGui.BeginTabItem("Top"))
                            {
                                int gameTexture = engine.GetShadowDepthTexture(ShadowType.Top, light);
                                ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Bottom"))
                            {
                                int gameTexture = engine.GetShadowDepthTexture(ShadowType.Bottom, light);
                                ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Left"))
                            {
                                int gameTexture = engine.GetShadowDepthTexture(ShadowType.Left, light);
                                ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Right"))
                            {
                                int gameTexture = engine.GetShadowDepthTexture(ShadowType.Right, light);
                                ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Front"))
                            {
                                int gameTexture = engine.GetShadowDepthTexture(ShadowType.Front, light);
                                ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Back"))
                            {
                                int gameTexture = engine.GetShadowDepthTexture(ShadowType.Back, light);
                                ImGui.Image((IntPtr)gameTexture, new System.Numerics.Vector2(200, 200), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                                ImGui.EndTabItem();
                            }
                        }
                        ImGui.EndTabBar();
                    }
                }
                if (ImGui.IsWindowHovered())
                    editorData.uiHasMouse = true;
                ImGui.PopStyleColor();
                ImGui.PopStyleVar();
            }
        }
    }
}
