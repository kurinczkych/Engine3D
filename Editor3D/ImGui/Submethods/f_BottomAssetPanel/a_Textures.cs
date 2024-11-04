using ImGuiNET;
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
        public void Textures(ref ImGuiStylePtr style, ref Vector2 imageSize)
        {
            if (ImGui.BeginTabItem("Textures"))
            {
                float padding = ImGui.GetStyle().WindowPadding.X;
                float spacing = ImGui.GetStyle().ItemSpacing.X;

                System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                int columns = (int)((availableSpace.X + spacing) / (imageSize.X + spacing));
                columns = Math.Max(1, columns);

                float itemWidth = availableSpace.X / columns - spacing;

                List<Asset> toRemove = new List<Asset>();
                List<string> folderNames = currentTextureAssetFolder.folders.Keys.ToList();
                AssetFolder? changeToFolder = null;

                int columnI = 0;
                int i = 0;

                if (currentTextureAssetFolder.name != "Textures")
                {
                    ImGui.BeginGroup();
                    ImGui.PushID("back");

                    System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                    ImGui.Image((IntPtr)engineData.textureManager.textures["ui_back.png"].TextureId, imageSize);

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    {
                        changeToFolder = currentTextureAssetFolder.parentFolder;
                    }

                    ImGui.SetCursorPos(cursorPos);

                    ImGui.InvisibleButton("##invisible", imageSize);

                    ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                    ImGui.TextWrapped("Back");
                    ImGui.PopTextWrapPos();

                    ImGui.PopID();

                    ImGui.EndGroup();


                    columnI++;
                }
                int folderCount = currentTextureAssetFolder.folders.Count;

                while (i < folderCount + currentTextureAssetFolder.assets.Count)
                {
                    if (columnI % columns != 0)
                    {
                        ImGui.SameLine();
                    }

                    if (i < currentTextureAssetFolder.folders.Count)
                    {
                        ImGui.BeginGroup();
                        ImGui.PushID(currentTextureAssetFolder.folders[folderNames[i]].name);

                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                        ImGui.Image((IntPtr)engineData.textureManager.textures["ui_folder.png"].TextureId, imageSize);

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                        {
                            changeToFolder = currentTextureAssetFolder.folders[folderNames[i]];
                        }
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup("AssetFolderContextMenu");
                        }

                        var windowPadding = style.WindowPadding;
                        var popupRounding = style.PopupRounding;
                        style.WindowPadding = new System.Numerics.Vector2(style.WindowPadding.X, style.WindowPadding.X);
                        style.PopupRounding = 2f;
                        if (ImGui.BeginPopup("AssetFolderContextMenu"))
                        {
                            if (ImGui.MenuItem("Delete folder"))
                            {
                                toRemove.AddRange(currentTextureAssetFolder.folders[folderNames[i]].assets);
                            }
                            ImGui.EndPopup();
                        }
                        style.WindowPadding = windowPadding;
                        style.PopupRounding = popupRounding;

                        ImGui.SetCursorPos(cursorPos);

                        ImGui.InvisibleButton("##invisible", imageSize);

                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                        ImGui.TextWrapped(currentTextureAssetFolder.folders[folderNames[i]].name);
                        ImGui.PopTextWrapPos();

                        ImGui.PopID();

                        ImGui.EndGroup();

                        columnI++;
                    }
                    else if (engineData.textureManager.textures.ContainsKey("ui_" + currentTextureAssetFolder.assets[i - folderCount].Name))
                    {
                        if (columnI % columns != 0)
                        {
                            ImGui.SameLine();
                        }

                        ImGui.BeginGroup();
                        ImGui.PushID(currentTextureAssetFolder.assets[i - folderCount].Name);

                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                        ImGui.Image((IntPtr)engineData.textureManager.textures["ui_" + currentTextureAssetFolder.assets[i - folderCount].Name].TextureId, imageSize);

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup("AssetContextMenu");
                        }

                        var windowPadding = style.WindowPadding;
                        var popupRounding = style.PopupRounding;
                        style.WindowPadding = new System.Numerics.Vector2(style.WindowPadding.X, style.WindowPadding.X);
                        style.PopupRounding = 2f;
                        if (ImGui.BeginPopup("AssetContextMenu"))
                        {
                            if (ImGui.MenuItem("Delete"))
                            {
                                toRemove.Add(currentTextureAssetFolder.assets[i - folderCount]);
                            }
                            ImGui.EndPopup();
                        }
                        style.WindowPadding = windowPadding;
                        style.PopupRounding = popupRounding;

                        ImGui.SetCursorPos(cursorPos);

                        ImGui.InvisibleButton("##invisible", imageSize);

                        DragDropImageSourceUI(ref engineData.textureManager, "TEXTURE_NAME", currentTextureAssetFolder.assets[i - folderCount].Name,
                                              currentTextureAssetFolder.assets[i - folderCount].Path, imageSize);

                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                        ImGui.TextWrapped(currentTextureAssetFolder.assets[i - folderCount].Name);
                        ImGui.PopTextWrapPos();

                        ImGui.PopID();

                        ImGui.EndGroup();

                        columnI++;
                    }
                    i++;
                }

                engineData.assetManager.Remove(toRemove);

                if (changeToFolder != null)
                    currentTextureAssetFolder = changeToFolder;

                ImGui.Dummy(new System.Numerics.Vector2(0.0f, 10f));

                ImGui.EndTabItem();
            }
        }
    }
}
