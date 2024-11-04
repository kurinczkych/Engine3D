using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void AssetStore(ref KeyboardState keyboardState, ref MouseState mouseState, ref Vector2 imageSize)
        {
            if (ImGui.BeginTabItem("Asset store"))
            {
                if (editorData.assetStoreManager.IsThereInternetConnection)
                {
                    Encoding.UTF8.GetBytes(editorData.assetStoreManager.currentKeyword, 0, editorData.assetStoreManager.currentKeyword.Length, _inputBuffers["##assetSearch"], 0);

                    if (currentBottomPanelTab != "Asset store")
                    {
                        currentBottomPanelTab = "Asset store";
                        ImGui.SetKeyboardFocusHere();
                    }

                    bool commit = false;
                    if (ImGui.InputText("##assetSearch", _inputBuffers["##assetSearch"], (uint)_inputBuffers["##assetSearch"].Length, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        commit = true;
                    }
                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                        commit = true;
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        ImGui.SetNextWindowFocus();
                        editorData.assetStoreManager.currentKeyword = "";
                        _inputBuffers["##assetSearch"][0] = 0;
                    }

                    if (commit)
                    {
                        editorData.assetStoreManager.currentKeyword = GetStringFromBuffer("##assetSearch");
                        editorData.assetStoreManager.currentPageNumber = 0;
                        editorData.assetStoreManager.GetOpenGameArtOrg();
                    }

                    ImGui.Separator();

                    float padding = ImGui.GetStyle().WindowPadding.X;
                    float spacing = ImGui.GetStyle().ItemSpacing.X;

                    System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                    int columns = (int)((availableSpace.X + spacing) / (imageSize.X + spacing));
                    columns = Math.Max(1, columns);
                    float itemWidth = availableSpace.X / columns - spacing;

                    for (int i = 0; i < editorData.assetStoreManager.assets.Count; i++)
                    {
                        if (engineData.textureManager.textures.ContainsKey("ui_" + editorData.assetStoreManager.assets[i].Path))
                        {
                            if (i % columns != 0)
                            {
                                ImGui.SameLine();
                            }

                            ImGui.BeginGroup();
                            ImGui.PushID(editorData.assetStoreManager.assets[i].Path);

                            ImGui.Image((IntPtr)engineData.textureManager.textures["ui_" + editorData.assetStoreManager.assets[i].Path].TextureId, imageSize);

                            var cursorPos = ImGui.GetCursorPos();

                            if (ImGui.IsItemHovered())
                            {
                                // Get the current window's draw list
                                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                                // Retrieve the bounding box of the last item (the image)
                                System.Numerics.Vector2 min = ImGui.GetItemRectMin();
                                System.Numerics.Vector2 max = ImGui.GetItemRectMax();

                                // Define the overlay color (gray transparent)
                                System.Numerics.Vector4 overlayColor = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 0.3f); // RGBA

                                // Draw the overlay
                                drawList.AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(overlayColor));

                                // Define the button position and size
                                System.Numerics.Vector2 buttonSize = new System.Numerics.Vector2(100, 20);
                                System.Numerics.Vector2 buttonPos = new System.Numerics.Vector2(
                                    (min.X + max.X) / 2 - buttonSize.X / 2,
                                    (min.Y + max.Y) / 2 - buttonSize.Y / 2
                                );

                                // Draw the button
                                ImGui.SetCursorScreenPos(buttonPos);
                                if (ImGui.Button("Download", buttonSize))
                                {
                                }

                                if (ImGui.IsItemHovered() && mouseState.IsButtonReleased(MouseButton.Left))
                                {
                                    editorData.assetStoreManager.DownloadAssetFull(editorData.assetStoreManager.assets[i]);
                                }
                            }

                            ImGui.SetCursorPos(cursorPos);
                            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                            ImGui.TextWrapped(editorData.assetStoreManager.assets[i].Name);
                            ImGui.PopTextWrapPos();

                            ImGui.PopID();

                            ImGui.EndGroup();
                        }
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0.0f, 10f));

                    ImGui.PopStyleVar();
                }
                else
                {
                    string noInternet = "There is no internet connection.";

                    System.Numerics.Vector2 textSize = ImGui.CalcTextSize(noInternet);

                    System.Numerics.Vector2 windowSize = ImGui.GetWindowSize();

                    System.Numerics.Vector2 textPos = new System.Numerics.Vector2(
                        (windowSize.X - textSize.X) * 0.5f, // X centered
                        (windowSize.Y - textSize.Y) * 0.5f  // Y centered (if you want it exactly in the middle)
                    );

                    ImGui.SetCursorPos(textPos);

                    ImGui.Text(noInternet);
                }

                ImGui.EndTabItem();
            }
        }
    }
}
