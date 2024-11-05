using Assimp;
using ImGuiNET;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Engine3D.Light;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void LightComponent(ref List<IComponent> toRemoveComp, ref ImGuiStylePtr style, IComponent c, ref Light light)
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Once);
            if (ImGui.TreeNode("Light"))
            {
                ImGui.SameLine();
                var origDeleteX = RightAlignCursor(70);
                ImGui.PushStyleColor(ImGuiCol.Button, style.Colors[(int)ImGuiCol.FrameBg]);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                if (ImGui.Button("Delete", new System.Numerics.Vector2(70, 20)))
                {
                    toRemoveComp.Add(c);
                    engine.lights = new List<Light>();
                }
                ImGui.PopStyleVar();
                ImGui.PopStyleColor();
                ImGui.SetCursorPosX(origDeleteX);
                ImGui.Dummy(new System.Numerics.Vector2(0, 0));

                #region Light
                LightType lightType = light.GetLightType();
                ImGui.Text("Type");
                ImGui.SameLine();
                ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 0.0f);
                if (ImGui.BeginCombo("##lightType", lightType.ToString()))
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0, 5));
                    foreach (LightType newLightType in Enum.GetValues(typeof(LightType)))
                    {
                        if (ImGui.Selectable(newLightType.ToString()))
                        {
                            light.SetLightType(newLightType);
                        }
                    }
                    ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                    ImGui.EndCombo();
                }

                if (lightType == LightType.DirectionalLight)
                {
                    if (ImGui.CollapsingHeader("Lighting", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        Color4 color = light.GetColorC4();
                        ColorPicker("Color", ref color);
                        light.SetColor(color);

                        float[] ambientVec = new float[] { light.ambient.X, light.ambient.Y, light.ambient.Z };
                        Vector3 ambient = InputFloat3("Ambient", new string[] { "X", "Y", "Z" }, ambientVec, ref keyboardState);
                        if (light.ambient != ambient)
                            light.ambient = ambient;

                        float[] diffuseVec = new float[] { light.diffuse.X, light.diffuse.Y, light.diffuse.Z };
                        Vector3 diffuse = InputFloat3("Diffuse", new string[] { "X", "Y", "Z" }, diffuseVec, ref keyboardState);
                        if (light.diffuse != diffuse)
                            light.diffuse = diffuse;

                        float[] specularVec = new float[] { light.specular.X, light.specular.Y, light.specular.Z };
                        Vector3 specular = InputFloat3("Specular", new string[] { "X", "Y", "Z" }, specularVec, ref keyboardState);
                        if (light.specular != specular)
                            light.specular = specular;

                        float[] specularPowVec = new float[] { light.specularPow };
                        float specularPow = InputFloat1("SpecularPow", new string[] { "X" }, specularPowVec, ref keyboardState);
                        if (light.specularPow != specularPow)
                            light.specularPow = specularPow;
                    }
                    if (ImGui.CollapsingHeader("Shadow"))
                    {
                        bool recalculateFrustum = false;

                        bool showFrustum = light.showGizmos;
                        if (ImGui.Checkbox("##showFrustum", ref showFrustum))
                        {
                            light.showGizmos = showFrustum;
                            if (light.showGizmos)
                                recalculateFrustum = true;
                        }
                        ImGui.SameLine();
                        ImGui.Text("Show light frustum");

                        float[] distanceFromSceneVec = new float[] { light.distanceFromScene };
                        float distanceFromScene = InputFloat1("Light Distance", new string[] { "" }, distanceFromSceneVec, ref keyboardState, titleSameLine:true);
                        if (light.distanceFromScene != distanceFromScene)
                        {
                            light.distanceFromScene = distanceFromScene;
                            recalculateFrustum = true;
                        }

                        float[] targetVec = new float[] { light.target.X, light.target.Y, light.target.Z };
                        Vector3 target = InputFloat3("Scene Target", new string[] { "X", "Y", "Z" }, targetVec, ref keyboardState);
                        if (light.target != target)
                        {
                            light.target = target;
                            recalculateFrustum = true;
                        }

                        #region SmallProjection
                        if (ImGui.CollapsingHeader("Small"))
                        {
                            float[] projection1Vec = new float[] { light.shadowSmall.projection.left, light.shadowSmall.projection.right };
                            float[] projection1 = InputFloat2("Projection", new string[] { "L", "R" }, projection1Vec, ref keyboardState, hiddenTitle:"Small");
                            if (projection1[0] != light.shadowSmall.projection.left)
                            {
                                light.shadowSmall.projection.left = projection1[0];
                                recalculateFrustum = true;
                            }
                            if (projection1[1] != light.shadowSmall.projection.right)
                            {
                                light.shadowSmall.projection.right = projection1[1];
                                recalculateFrustum = true;
                            }

                            float[] projection2Vec = new float[] { light.shadowSmall.projection.top, light.shadowSmall.projection.bottom };
                            float[] projection2 = InputFloat2("", new string[] { "T", "B" }, projection2Vec, ref keyboardState, hiddenTitle: "Small");
                            if (projection2[0] != light.shadowSmall.projection.top)
                            {
                                light.shadowSmall.projection.top = projection2[0];
                                recalculateFrustum = true;
                            }
                            if (projection2[1] != light.shadowSmall.projection.bottom)
                            {
                                light.shadowSmall.projection.bottom = projection2[1];
                                recalculateFrustum = true;
                            }

                            float[] nearFarVec = new float[] { light.shadowSmall.projection.near, light.shadowSmall.projection.far };
                            float[] nearFar = InputFloat2("", new string[] { "Near", "Far" }, nearFarVec, ref keyboardState, hiddenTitle: "Small");
                            if (nearFar[0] != light.shadowSmall.projection.near)
                            {
                                light.shadowSmall.projection.near = nearFar[0];
                                recalculateFrustum = true;
                            }
                            if (nearFar[1] != light.shadowSmall.projection.far)
                            {
                                light.shadowSmall.projection.far = nearFar[1];
                                recalculateFrustum = true;
                            }
                        }
                        #endregion

                        #region MediumProjection
                        if (ImGui.CollapsingHeader("Medium"))
                        {
                            float[] projection1Vec = new float[] { light.shadowMedium.projection.left, light.shadowMedium.projection.right };
                            float[] projection1 = InputFloat2("Projection", new string[] { "L", "R" }, projection1Vec, ref keyboardState, hiddenTitle: "Medium");
                            if (projection1[0] != light.shadowMedium.projection.left)
                            {
                                light.shadowMedium.projection.left = projection1[0];
                                recalculateFrustum = true;
                            }
                            if (projection1[1] != light.shadowMedium.projection.right)
                            {
                                light.shadowMedium.projection.right = projection1[1];
                                recalculateFrustum = true;
                            }

                            float[] projection2Vec = new float[] { light.shadowMedium.projection.top, light.shadowMedium.projection.bottom };
                            float[] projection2 = InputFloat2("", new string[] { "T", "B" }, projection2Vec, ref keyboardState, hiddenTitle: "Medium");
                            if (projection2[0] != light.shadowMedium.projection.top)
                            {
                                light.shadowMedium.projection.top = projection2[0];
                                recalculateFrustum = true;
                            }
                            if (projection2[1] != light.shadowMedium.projection.bottom)
                            {
                                light.shadowMedium.projection.bottom = projection2[1];
                                recalculateFrustum = true;
                            }

                            float[] nearFarVec = new float[] { light.shadowMedium.projection.near, light.shadowMedium.projection.far };
                            float[] nearFar = InputFloat2("", new string[] { "Near", "Far" }, nearFarVec, ref keyboardState, hiddenTitle: "Medium");
                            if (nearFar[0] != light.shadowMedium.projection.near)
                            {
                                light.shadowMedium.projection.near = nearFar[0];
                                recalculateFrustum = true;
                            }
                            if (nearFar[1] != light.shadowMedium.projection.far)
                            {
                                light.shadowMedium.projection.far = nearFar[1];
                                recalculateFrustum = true;
                            }
                        }
                        #endregion

                        #region LargeProjection
                        if (ImGui.CollapsingHeader("Large"))
                        {
                            float[] projection1Vec = new float[] { light.shadowLarge.projection.left, light.shadowLarge.projection.right };
                            float[] projection1 = InputFloat2("Projection", new string[] { "L", "R" }, projection1Vec, ref keyboardState, hiddenTitle: "Large");
                            if (projection1[0] != light.shadowLarge.projection.left)
                            {
                                light.shadowLarge.projection.left = projection1[0];
                                recalculateFrustum = true;
                            }
                            if (projection1[1] != light.shadowLarge.projection.right)
                            {
                                light.shadowLarge.projection.right = projection1[1];
                                recalculateFrustum = true;
                            }

                            float[] projection2Vec = new float[] { light.shadowLarge.projection.top, light.shadowLarge.projection.bottom };
                            float[] projection2 = InputFloat2("", new string[] { "T", "B" }, projection2Vec, ref keyboardState, hiddenTitle: "Large");
                            if (projection2[0] != light.shadowLarge.projection.top)
                            {
                                light.shadowLarge.projection.top = projection2[0];
                                recalculateFrustum = true;
                            }
                            if (projection2[1] != light.shadowLarge.projection.bottom)
                            {
                                light.shadowLarge.projection.bottom = projection2[1];
                                recalculateFrustum = true;
                            }

                            float[] nearFarVec = new float[] { light.shadowLarge.projection.near, light.shadowLarge.projection.far };
                            float[] nearFar = InputFloat2("", new string[] { "Near", "Far" }, nearFarVec, ref keyboardState, hiddenTitle: "Large");
                            if (nearFar[0] != light.shadowLarge.projection.near)
                            {
                                light.shadowLarge.projection.near = nearFar[0];
                                recalculateFrustum = true;
                            }
                            if (nearFar[1] != light.shadowLarge.projection.far)
                            {
                                light.shadowLarge.projection.far = nearFar[1];
                                recalculateFrustum = true;
                            }
                        }
                        #endregion

                        if (recalculateFrustum)
                        {
                            light.RecalculateGizmos();
                        }
                    }
                }
                else if (lightType == LightType.PointLight)
                {
                    if (ImGui.CollapsingHeader("Lighting"))
                    {
                        Color4 color = light.GetColorC4();
                        ColorPicker("Color", ref color);
                        light.SetColor(color);

                        float[] ambientVec = new float[] { light.ambient.X, light.ambient.Y, light.ambient.Z };
                        Vector3 ambient = InputFloat3("Ambient", new string[] { "X", "Y", "Z" }, ambientVec, ref keyboardState);
                        if (light.ambient != ambient)
                            light.ambient = ambient;

                        float[] diffuseVec = new float[] { light.diffuse.X, light.diffuse.Y, light.diffuse.Z };
                        Vector3 diffuse = InputFloat3("Diffuse", new string[] { "X", "Y", "Z" }, diffuseVec, ref keyboardState);
                        if (light.diffuse != diffuse)
                            light.diffuse = diffuse;

                        float[] specularVec = new float[] { light.specular.X, light.specular.Y, light.specular.Z };
                        Vector3 specular = InputFloat3("Specular", new string[] { "X", "Y", "Z" }, specularVec, ref keyboardState);
                        if (light.specular != specular)
                            light.specular = specular;

                        float[] specularPowVec = new float[] { light.specularPow };
                        float specularPow = InputFloat1("SpecularPow", new string[] { "X" }, specularPowVec, ref keyboardState);
                        if (light.specularPow != specularPow)
                            light.specularPow = specularPow;


                        ImGui.Checkbox("Advanced", ref advancedLightSetting);

                        if (!advancedLightSetting)
                        {
                            float[] rangeVec = new float[] { light.range };
                            float range = InputFloat1("Range", new string[] { "" }, specularPowVec, ref keyboardState);
                            if (light.range != range)
                            {
                                light.range = range;
                                float[] att = Light.RangeToAttenuation(range);
                                light.constant = att[0];
                                light.linear = att[1];
                                light.quadratic = att[2];
                            }
                        }
                        else
                        {
                            ImGui.Text("Constant Linear Quadratic");
                            float[] pointVec = new float[] { light.constant, light.linear, light.quadratic };
                            Vector3 point = InputFloat3("Point", new string[] { "Constant", "Linear", "Quadratic" }, pointVec, ref keyboardState, true);
                            if (light.constant != point[0])
                                light.constant = point[0];
                            if (light.linear != point[1])
                                light.linear = point[1];
                            if (light.quadratic != point[2])
                                light.quadratic = point[2];
                        }
                    }
                }

                ImGui.PopStyleVar();
                #endregion

                ImGui.TreePop();
            }
        }
    }
}
