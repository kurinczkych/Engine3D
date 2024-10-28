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
        public void ObjectShowing(ref ImGuiStylePtr style, Vector2 windowPadding, float popupRounding)
        {
            if (engineData.objects.Count > 0)
            {
                if (shouldOpenTreeNodeMeshes)
                {
                    ImGui.SetNextItemOpen(true, ImGuiCond.Once); // Open the tree node once.
                    shouldOpenTreeNodeMeshes = false; // Reset the flag so it doesn't open again automatically.
                }

                if (engineData.objects.Count > 0 && ImGui.TreeNode("Objects"))
                {
                    ImGui.Separator();
                    for (int i = 0; i < engineData.objects.Count; i++)
                    {
                        Object ro = engineData.objects[i];
                        if (!ro.interactableInEditor)
                            continue;

                        if (ImGui.Selectable(ro.displayName, (editorData.selectedItem != null && editorData.selectedItem.id == ro.id)))
                        {
                            SelectItem(ro, editorData);
                        }
                        if (ImGui.IsItemHovered())
                            editorData.anyObjectHovered = ro.id;

                        if (isObjectHovered != -1 && isObjectHovered == ro.id)
                        {
                            style.WindowPadding = new System.Numerics.Vector2(style.WindowPadding.X, style.WindowPadding.X);
                            style.PopupRounding = 2f;
                            if (ImGui.BeginPopupContextWindow("objectManagingMenu", ImGuiPopupFlags.MouseButtonRight))
                            {
                                editorData.anyObjectHovered = ro.id;
                                if (ImGui.MenuItem("Delete"))
                                {
                                    engine.RemoveObject(ro);
                                    editorData.recalculateObjects = true;
                                    SelectItem(null, editorData);
                                }

                                ImGui.EndPopup();
                            }
                            style.WindowPadding = windowPadding;
                            style.PopupRounding = popupRounding;
                        }
                    }
                }
            }
        }
    }
}
