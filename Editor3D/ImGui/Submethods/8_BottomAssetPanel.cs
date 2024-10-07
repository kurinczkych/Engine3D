using ImGuiNET;
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
        public void BottomAssetPanel(ref GameWindowProperty gameWindow, ref ImGuiStylePtr style, ref KeyboardState keyboardState, ref MouseState mouseState)
        {
            style.WindowRounding = 0f;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, _windowHeight * gameWindow.bottomPanelPercent - seperatorSize - 1));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight * (1 - gameWindow.bottomPanelPercent) + seperatorSize - gameWindow.bottomPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("BottomAssetPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if (ImGui.BeginTabBar("MyTabs"))
                {
                    float imageWidth = 100;
                    float imageHeight = 100;
                    System.Numerics.Vector2 imageSize = new System.Numerics.Vector2(imageWidth, imageHeight);

                    if (ImGui.BeginTabItem("Project"))
                    {
                        if (currentBottomPanelTab != "Project")
                            currentBottomPanelTab = "Project";

                        if (ImGui.BeginTabBar("Assets"))
                        {
                            if (ImGui.BeginTabItem("Textures"))
                            {
                                float padding = ImGui.GetStyle().WindowPadding.X;
                                float spacing = ImGui.GetStyle().ItemSpacing.X;

                                System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                                int columns = (int)((availableSpace.X + spacing) / (imageWidth + spacing));
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
                                    ImGui.Image((IntPtr)Engine.textureManager.textures["ui_back.png"].TextureId, imageSize);

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
                                        ImGui.Image((IntPtr)Engine.textureManager.textures["ui_folder.png"].TextureId, imageSize);

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
                                    else if (Engine.textureManager.textures.ContainsKey("ui_" + currentTextureAssetFolder.assets[i - folderCount].Name))
                                    {
                                        if (columnI % columns != 0)
                                        {
                                            ImGui.SameLine();
                                        }

                                        ImGui.BeginGroup();
                                        ImGui.PushID(currentTextureAssetFolder.assets[i - folderCount].Name);

                                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                        ImGui.Image((IntPtr)Engine.textureManager.textures["ui_" + currentTextureAssetFolder.assets[i - folderCount].Name].TextureId, imageSize);

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

                                        DragDropImageSourceUI(ref Engine.textureManager, "TEXTURE_NAME", currentTextureAssetFolder.assets[i - folderCount].Name,
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

                                editorData.assetManager.Remove(toRemove);

                                if (changeToFolder != null)
                                    currentTextureAssetFolder = changeToFolder;

                                ImGui.Dummy(new System.Numerics.Vector2(0.0f, 10f));

                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Models"))
                            {
                                float padding = ImGui.GetStyle().WindowPadding.X;
                                float spacing = ImGui.GetStyle().ItemSpacing.X;

                                System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                                int columns = (int)((availableSpace.X + spacing) / (imageWidth + spacing));
                                columns = Math.Max(1, columns);

                                float itemWidth = availableSpace.X / columns - spacing;

                                List<Asset> toRemove = new List<Asset>();
                                List<string> folderNames = currentModelAssetFolder.folders.Keys.ToList();
                                AssetFolder? changeToFolder = null;

                                int columnI = 0;
                                int i = 0;

                                // Back button
                                if (currentModelAssetFolder.name != "Models")
                                {
                                    ImGui.BeginGroup();
                                    ImGui.PushID("back");

                                    System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                    ImGui.Image((IntPtr)Engine.textureManager.textures["ui_back.png"].TextureId, imageSize);

                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                    {
                                        changeToFolder = currentModelAssetFolder.parentFolder;
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

                                int folderCount = currentModelAssetFolder.folders.Count;
                                while (i < folderCount + currentModelAssetFolder.assets.Count)
                                {
                                    if (columnI % columns != 0)
                                    {
                                        ImGui.SameLine();
                                    }

                                    if (i < currentModelAssetFolder.folders.Count)
                                    {
                                        ImGui.BeginGroup();
                                        ImGui.PushID(currentModelAssetFolder.folders[folderNames[i]].name);

                                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                        ImGui.Image((IntPtr)Engine.textureManager.textures["ui_folder.png"].TextureId, imageSize);

                                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                        {
                                            changeToFolder = currentModelAssetFolder.folders[folderNames[i]];
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
                                                toRemove.AddRange(currentModelAssetFolder.folders[folderNames[i]].assets);
                                            }
                                            ImGui.EndPopup();
                                        }
                                        style.WindowPadding = windowPadding;
                                        style.PopupRounding = popupRounding;

                                        ImGui.SetCursorPos(cursorPos);

                                        ImGui.InvisibleButton("##invisible", imageSize);

                                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                                        ImGui.TextWrapped(currentModelAssetFolder.folders[folderNames[i]].name);
                                        ImGui.PopTextWrapPos();

                                        ImGui.PopID();

                                        ImGui.EndGroup();

                                        columnI++;
                                    }
                                    else if (currentTextureAssetFolder.assets.Count > i - folderCount)
                                    {
                                        if (columnI % columns != 0)
                                        {
                                            ImGui.SameLine();
                                        }

                                        ImGui.BeginGroup();
                                        ImGui.PushID(currentTextureAssetFolder.assets[i - folderCount].Name);

                                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();

                                        if (Engine.textureManager.textures.ContainsKey("ui_" + currentModelAssetFolder.assets[i - folderCount].Name))
                                            ImGui.Image((IntPtr)Engine.textureManager.textures["ui_" + currentModelAssetFolder.assets[i - folderCount].Name].TextureId, imageSize);
                                        else
                                            ImGui.Image((IntPtr)Engine.textureManager.textures["ui_missing.png"].TextureId, imageSize);

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
                                                toRemove.Add(currentModelAssetFolder.assets[i - folderCount]);
                                            }
                                            ImGui.EndPopup();
                                        }
                                        style.WindowPadding = windowPadding;
                                        style.PopupRounding = popupRounding;

                                        ImGui.SetCursorPos(cursorPos);

                                        ImGui.InvisibleButton("##invisible", imageSize);

                                        DragDropImageSourceUI(ref Engine.textureManager, "MESH_NAME", currentModelAssetFolder.assets[i - folderCount].Name,
                                                              currentModelAssetFolder.assets[i - folderCount].Path, imageSize);

                                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                                        ImGui.TextWrapped(currentModelAssetFolder.assets[i - folderCount].Name);
                                        ImGui.PopTextWrapPos();

                                        ImGui.PopID();

                                        ImGui.EndGroup();

                                        columnI++;
                                    }
                                    i++;
                                }

                                editorData.assetManager.Remove(toRemove);

                                if (changeToFolder != null)
                                    currentModelAssetFolder = changeToFolder;

                                ImGui.Dummy(new System.Numerics.Vector2(0.0f, 10f));

                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Audio"))
                            {
                                float padding = ImGui.GetStyle().WindowPadding.X;
                                float spacing = ImGui.GetStyle().ItemSpacing.X;

                                System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                                int columns = (int)((availableSpace.X + spacing) / (imageWidth + spacing));
                                columns = Math.Max(1, columns);

                                float itemWidth = availableSpace.X / columns - spacing;

                                List<Asset> toRemove = new List<Asset>();
                                List<string> folderNames = currentAudioAssetFolder.folders.Keys.ToList();
                                AssetFolder? changeToFolder = null;

                                int columnI = 0;
                                int i = 0;

                                // Back button
                                if (currentAudioAssetFolder.name != "Audio")
                                {
                                    ImGui.BeginGroup();
                                    ImGui.PushID("back");

                                    System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                    ImGui.Image((IntPtr)Engine.textureManager.textures["ui_back.png"].TextureId, imageSize);

                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                    {
                                        changeToFolder = currentAudioAssetFolder.parentFolder;
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

                                int folderCount = currentAudioAssetFolder.folders.Count;
                                while (i < folderCount + currentAudioAssetFolder.assets.Count)
                                {
                                    if (columnI % columns != 0)
                                    {
                                        ImGui.SameLine();
                                    }

                                    if (i < currentAudioAssetFolder.folders.Count)
                                    {
                                        ImGui.BeginGroup();
                                        ImGui.PushID(currentAudioAssetFolder.folders[folderNames[i]].name);

                                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                        ImGui.Image((IntPtr)Engine.textureManager.textures["ui_folder.png"].TextureId, imageSize);

                                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                        {
                                            changeToFolder = currentAudioAssetFolder.folders[folderNames[i]];
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
                                                toRemove.AddRange(currentAudioAssetFolder.folders[folderNames[i]].assets);
                                            }
                                            ImGui.EndPopup();
                                        }
                                        style.WindowPadding = windowPadding;
                                        style.PopupRounding = popupRounding;

                                        ImGui.SetCursorPos(cursorPos);

                                        ImGui.InvisibleButton("##invisible", imageSize);

                                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                                        ImGui.TextWrapped(currentAudioAssetFolder.folders[folderNames[i]].name);
                                        ImGui.PopTextWrapPos();

                                        ImGui.PopID();

                                        ImGui.EndGroup();

                                        columnI++;
                                    }
                                    else
                                    {
                                        if (columnI % columns != 0)
                                        {
                                            ImGui.SameLine();
                                        }

                                        ImGui.BeginGroup();
                                        ImGui.PushID(currentAudioAssetFolder.assets[i - folderCount].Name);

                                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();

                                        if (Engine.textureManager.textures.ContainsKey("ui_" + currentAudioAssetFolder.assets[i - folderCount].Name))
                                            ImGui.Image((IntPtr)Engine.textureManager.textures["ui_" + currentAudioAssetFolder.assets[i - folderCount].Name].TextureId, imageSize);
                                        else
                                            ImGui.Image((IntPtr)Engine.textureManager.textures["ui_missing.png"].TextureId, imageSize);

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
                                                toRemove.Add(currentAudioAssetFolder.assets[i - folderCount]);
                                            }
                                            ImGui.EndPopup();
                                        }
                                        style.WindowPadding = windowPadding;
                                        style.PopupRounding = popupRounding;

                                        ImGui.SetCursorPos(cursorPos);

                                        ImGui.InvisibleButton("##invisible", imageSize);

                                        DragDropImageSourceUI(ref Engine.textureManager, "AUDIO_NAME", currentAudioAssetFolder.assets[i - folderCount].Name,
                                                              currentAudioAssetFolder.assets[i - folderCount].Path, imageSize);

                                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                                        ImGui.TextWrapped(currentAudioAssetFolder.assets[i - folderCount].Name);
                                        ImGui.PopTextWrapPos();

                                        ImGui.PopID();

                                        ImGui.EndGroup();

                                        columnI++;
                                    }
                                    i++;
                                }

                                editorData.assetManager.Remove(toRemove);

                                if (changeToFolder != null)
                                    currentAudioAssetFolder = changeToFolder;

                                ImGui.Dummy(new System.Numerics.Vector2(0.0f, 10f));

                                ImGui.EndTabItem();
                            }

                            ImGui.EndTabBar();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Console"))
                    {
                        if (currentBottomPanelTab != "Console")
                            currentBottomPanelTab = "Console";

                        ImGui.PushFont(default18);
                        foreach (Log log in Engine.consoleManager.Logs)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, Engine.consoleManager.LogColors[log.logType]);
                            ImGui.TextWrapped(log.message);
                            ImGui.PopStyleColor();
                        }
                        ImGui.PopFont();
                        ImGui.SetScrollHereY(1.0f);

                        ImGui.SetCursorPosY(ImGui.GetWindowSize().Y - gameWindow.bottomPanelSize - 4);
                        ImGui.Separator();
                        ImGui.SetNextItemWidth(200);
                        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 0);
                        if (ImGui.BeginCombo("##showConsoleTypeDropdown", showConsoleTypeList[showConsoleTypeListIndex]))
                        {
                            ImGui.Dummy(new System.Numerics.Vector2(0, 5));
                            for (int i = 0; i < showConsoleTypeList.Length; i++)
                            {
                                bool isSelected = (i == showConsoleTypeListIndex);

                                if (ImGui.Selectable(showConsoleTypeList[i], isSelected))
                                {
                                    showConsoleTypeListIndex = i;
                                    Engine.consoleManager.showConsoleType = (ShowConsoleType)Enum.Parse(typeof(ShowConsoleType), showConsoleTypeList[showConsoleTypeListIndex]);
                                }

                                if (isSelected)
                                {
                                    ImGui.SetItemDefaultFocus();
                                }
                            }
                            ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                            ImGui.EndCombo();
                        }
                        ImGui.PopStyleVar();

                        ImGui.Dummy(new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 20));

                        ImGui.EndTabItem();
                    }

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
                            int columns = (int)((availableSpace.X + spacing) / (imageWidth + spacing));
                            columns = Math.Max(1, columns);
                            float itemWidth = availableSpace.X / columns - spacing;

                            for (int i = 0; i < editorData.assetStoreManager.assets.Count; i++)
                            {
                                if (Engine.textureManager.textures.ContainsKey("ui_" + editorData.assetStoreManager.assets[i].Path))
                                {
                                    if (i % columns != 0)
                                    {
                                        ImGui.SameLine();
                                    }

                                    ImGui.BeginGroup();
                                    ImGui.PushID(editorData.assetStoreManager.assets[i].Path);

                                    ImGui.Image((IntPtr)Engine.textureManager.textures["ui_" + editorData.assetStoreManager.assets[i].Path].TextureId, imageSize);

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

                    if (Engine.consoleManager.justAdded)
                    {
                        ImGui.SetTabItemClosed("Project");
                        ImGui.SetTabItemClosed("Asset store");

                        Engine.consoleManager.justAdded = false;
                    }

                    ImGui.EndTabBar();
                }
            }
            ImGui.PopStyleVar();
            ImGui.End();
        }
    }
}
