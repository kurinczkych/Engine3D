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
        public void CameraComponent(ref Camera cam)
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
    }
}
