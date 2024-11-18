using Assimp;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                // TODO: Light type change
                //LightType lightType = light.GetLightType();
                //ImGui.Text("Type");
                //ImGui.SameLine();
                //ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 0.0f);
                //if (ImGui.BeginCombo("##lightType", lightType.ToString()))
                //{
                //    ImGui.Dummy(new System.Numerics.Vector2(0, 5));
                //    foreach (LightType newLightType in Enum.GetValues(typeof(LightType)))
                //    {
                //        if (ImGui.Selectable(newLightType.ToString()))
                //        {
                //            light.SetLightType(newLightType);
                //        }
                //    }
                //    ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                //    ImGui.EndCombo();
                //}

                if (light is DirectionalLight dl)
                {
                    if (ImGui.CollapsingHeader("Lighting", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        Color4 color = light.GetColorC4();
                        ColorPicker("Color", ref color);
                        light.SetColor(color);

                        float[] ambientVec = new float[] { light.properties.ambient.X, light.properties.ambient.Y, light.properties.ambient.Z };
                        Vector3 ambient = InputFloat3("Ambient", new string[] { "X", "Y", "Z" }, ambientVec, ref keyboardState);
                        if (light.properties.ambient.Xyz != ambient)
                            light.properties.ambient = new Vector4(ambient, 1.0f);

                        float[] diffuseVec = new float[] { light.properties.diffuse.X, light.properties.diffuse.Y, light.properties.diffuse.Z };
                        Vector3 diffuse = InputFloat3("Diffuse", new string[] { "X", "Y", "Z" }, diffuseVec, ref keyboardState);
                        if (light.properties.diffuse.Xyz != diffuse)
                            light.properties.diffuse = new Vector4(diffuse, 1.0f);

                        float[] specularVec = new float[] { light.properties.specular.X, light.properties.specular.Y, light.properties.specular.Z };
                        Vector3 specular = InputFloat3("Specular", new string[] { "X", "Y", "Z" }, specularVec, ref keyboardState);
                        if (light.properties.specular.Xyz != specular)
                            light.properties.specular = new Vector4(specular, 1.0f);

                        float[] specularPowVec = new float[] { light.properties.specularPow };
                        float specularPow = InputFloat1("SpecularPow", new string[] { "X" }, specularPowVec, ref keyboardState);
                        if (light.properties.specularPow != specularPow)
                            light.properties.specularPow = specularPow;
                    }
                    if (ImGui.CollapsingHeader("Shadow"))
                    {
                        bool recalculateFrustum = false;
                        int resizedShadowMap = -1;
                        ShadowType resizedShadowType = ShadowType.Small;

                        if (ImGui.Checkbox("##castShadows", ref light.castShadows))
                        {
                            if (light.castShadows)
                            {
                                recalculateFrustum = true;
                                light.showGizmos = true;
                                light.freezeView = true;
                            }
                            else
                            {
                                light.showGizmos = false;
                                light.freezeView = true;
                            }
                        }
                        ImGui.SameLine();
                        ImGui.Text("Cast shadows");

                        bool showFrustum = light.showGizmos;
                        if (ImGui.Checkbox("##showFrustum", ref showFrustum))
                        {
                            light.showGizmos = showFrustum;
                            if (light.showGizmos)
                                recalculateFrustum = true;
                        }
                        ImGui.SameLine();
                        ImGui.Text("Show light frustum");

                        if (ImGui.Checkbox("##freezeView", ref light.freezeView)) { }
                        ImGui.SameLine();
                        ImGui.Text("Freeze view");

                        float[] distanceFromSceneVec = new float[] { light.distanceFromScene };
                        float distanceFromScene = InputFloat1("Light Distance", new string[] { "" }, distanceFromSceneVec, ref keyboardState, titleSameLine:true);
                        if (light.distanceFromScene != distanceFromScene)
                        {
                            light.distanceFromScene = distanceFromScene;
                            recalculateFrustum = true;
                        }

                        #region SmallProjection
                        if (ImGui.CollapsingHeader("Small"))
                        {
                            float[] sizeVec = new float[] { dl.shadowSmall.size };
                            float size = InputFloat1("Shadow Map Size", new string[] { "" }, sizeVec, ref keyboardState, titleSameLine: true, hiddenTitle:"Small");
                            if (dl.shadowSmall.size != size)
                            {
                                resizedShadowMap = (int)size;
                                resizedShadowType = ShadowType.Small;
                            }

                            float[] projection1Vec = new float[] { dl.shadowSmall.projection.left, dl.shadowSmall.projection.right };
                            float[] projection1 = InputFloat2("Projection", new string[] { "L", "R" }, projection1Vec, ref keyboardState, hiddenTitle:"Small");
                            if (projection1[0] != dl.shadowSmall.projection.left)
                            {
                                dl.shadowSmall.projection.left = projection1[0];
                                recalculateFrustum = true;
                            }
                            if (projection1[1] != dl.shadowSmall.projection.right)
                            {
                                dl.shadowSmall.projection.right = projection1[1];
                                recalculateFrustum = true;
                            }

                            float[] projection2Vec = new float[] { dl.shadowSmall.projection.top, dl.shadowSmall.projection.bottom };
                            float[] projection2 = InputFloat2("", new string[] { "T", "B" }, projection2Vec, ref keyboardState, hiddenTitle: "Small");
                            if (projection2[0] != dl.shadowSmall.projection.top)
                            {
                                dl.shadowSmall.projection.top = projection2[0];
                                recalculateFrustum = true;
                            }
                            if (projection2[1] != dl.shadowSmall.projection.bottom)
                            {
                                dl.shadowSmall.projection.bottom = projection2[1];
                                recalculateFrustum = true;
                            }

                            float[] nearFarVec = new float[] { dl.shadowSmall.projection.near, dl.shadowSmall.projection.far };
                            float[] nearFar = InputFloat2("", new string[] { "Near", "Far" }, nearFarVec, ref keyboardState, hiddenTitle: "Small");
                            if (nearFar[0] != dl.shadowSmall.projection.near)
                            {
                                dl.shadowSmall.projection.near = nearFar[0];
                                recalculateFrustum = true;
                            }
                            if (nearFar[1] != dl.shadowSmall.projection.far)
                            {
                                dl.shadowSmall.projection.far = nearFar[1];
                                recalculateFrustum = true;
                            }
                        }
                        #endregion

                        #region MediumProjection
                        if (ImGui.CollapsingHeader("Medium"))
                        {
                            float[] sizeVec = new float[] { dl.shadowMedium.size };
                            float size = InputFloat1("Shadow Map Size", new string[] { "" }, sizeVec, ref keyboardState, titleSameLine: true, hiddenTitle: "Medium");
                            if (dl.shadowMedium.size != size)
                            {
                                resizedShadowMap = (int)size;
                                resizedShadowType = ShadowType.Medium;
                            }

                            float[] projection1Vec = new float[] { dl.shadowMedium.projection.left, dl.shadowMedium.projection.right };
                            float[] projection1 = InputFloat2("Projection", new string[] { "L", "R" }, projection1Vec, ref keyboardState, hiddenTitle: "Medium");
                            if (projection1[0] != dl.shadowMedium.projection.left)
                            {
                                dl.shadowMedium.projection.left = projection1[0];
                                recalculateFrustum = true;
                            }
                            if (projection1[1] != dl.shadowMedium.projection.right)
                            {
                                dl.shadowMedium.projection.right = projection1[1];
                                recalculateFrustum = true;
                            }

                            float[] projection2Vec = new float[] { dl.shadowMedium.projection.top, dl.shadowMedium.projection.bottom };
                            float[] projection2 = InputFloat2("", new string[] { "T", "B" }, projection2Vec, ref keyboardState, hiddenTitle: "Medium");
                            if (projection2[0] != dl.shadowMedium.projection.top)
                            {
                                dl.shadowMedium.projection.top = projection2[0];
                                recalculateFrustum = true;
                            }
                            if (projection2[1] != dl.shadowMedium.projection.bottom)
                            {
                                dl.shadowMedium.projection.bottom = projection2[1];
                                recalculateFrustum = true;
                            }

                            float[] nearFarVec = new float[] { dl.shadowMedium.projection.near, dl.shadowMedium.projection.far };
                            float[] nearFar = InputFloat2("", new string[] { "Near", "Far" }, nearFarVec, ref keyboardState, hiddenTitle: "Medium");
                            if (nearFar[0] != dl.shadowMedium.projection.near)
                            {
                                dl.shadowMedium.projection.near = nearFar[0];
                                recalculateFrustum = true;
                            }
                            if (nearFar[1] != dl.shadowMedium.projection.far)
                            {
                                dl.shadowMedium.projection.far = nearFar[1];
                                recalculateFrustum = true;
                            }
                        }
                        #endregion

                        #region LargeProjection
                        if (ImGui.CollapsingHeader("Large"))
                        {
                            float[] sizeVec = new float[] { dl.shadowLarge.size };
                            float size = InputFloat1("Shadow Map Size", new string[] { "" }, sizeVec, ref keyboardState, titleSameLine: true, hiddenTitle: "Large");
                            if (dl.shadowLarge.size != size)
                            {
                                resizedShadowMap = (int)size;
                                resizedShadowType = ShadowType.Large;
                            }

                            float[] projection1Vec = new float[] { dl.shadowLarge.projection.left, dl.shadowLarge.projection.right };
                            float[] projection1 = InputFloat2("Projection", new string[] { "L", "R" }, projection1Vec, ref keyboardState, hiddenTitle: "Large");
                            if (projection1[0] != dl.shadowLarge.projection.left)
                            {
                                dl.shadowLarge.projection.left = projection1[0];
                                recalculateFrustum = true;
                            }
                            if (projection1[1] != dl.shadowLarge.projection.right)
                            {
                                dl.shadowLarge.projection.right = projection1[1];
                                recalculateFrustum = true;
                            }

                            float[] projection2Vec = new float[] { dl.shadowLarge.projection.top, dl.shadowLarge.projection.bottom };
                            float[] projection2 = InputFloat2("", new string[] { "T", "B" }, projection2Vec, ref keyboardState, hiddenTitle: "Large");
                            if (projection2[0] != dl.shadowLarge.projection.top)
                            {
                                dl.shadowLarge.projection.top = projection2[0];
                                recalculateFrustum = true;
                            }
                            if (projection2[1] != dl.shadowLarge.projection.bottom)
                            {
                                dl.shadowLarge.projection.bottom = projection2[1];
                                recalculateFrustum = true;
                            }

                            float[] nearFarVec = new float[] { dl.shadowLarge.projection.near, dl.shadowLarge.projection.far };
                            float[] nearFar = InputFloat2("", new string[] { "Near", "Far" }, nearFarVec, ref keyboardState, hiddenTitle: "Large");
                            if (nearFar[0] != dl.shadowLarge.projection.near)
                            {
                                dl.shadowLarge.projection.near = nearFar[0];
                                recalculateFrustum = true;
                            }
                            if (nearFar[1] != dl.shadowLarge.projection.far)
                            {
                                dl.shadowLarge.projection.far = nearFar[1];
                                recalculateFrustum = true;
                            }
                        }
                        #endregion

                        if(resizedShadowMap != -1)
                        {
                            Engine.recreateShadowArray = true;
                        }

                        if (recalculateFrustum)
                        {
                            light.RecalculateShadows();
                            Light.SendUBOToGPU(engine.lights, engine.lightUBO);
                        }
                    }
                }
                else if (light is PointLight pl)
                {
                    if (ImGui.CollapsingHeader("Lighting"))
                    {
                        Color4 color = light.GetColorC4();
                        ColorPicker("Color", ref color);
                        light.SetColor(color);

                        float[] ambientVec = new float[] { light.properties.ambient.X, light.properties.ambient.Y, light.properties.ambient.Z };
                        Vector3 ambient = InputFloat3("Ambient", new string[] { "X", "Y", "Z" }, ambientVec, ref keyboardState);
                        if (light.properties.ambient.Xyz != ambient)
                            light.properties.ambient = new Vector4(ambient, 1.0f);

                        float[] diffuseVec = new float[] { light.properties.diffuse.X, light.properties.diffuse.Y, light.properties.diffuse.Z };
                        Vector3 diffuse = InputFloat3("Diffuse", new string[] { "X", "Y", "Z" }, diffuseVec, ref keyboardState);
                        if (light.properties.diffuse.Xyz != diffuse)
                            light.properties.diffuse = new Vector4(diffuse, 1.0f);

                        float[] specularVec = new float[] { light.properties.specular.X, light.properties.specular.Y, light.properties.specular.Z };
                        Vector3 specular = InputFloat3("Specular", new string[] { "X", "Y", "Z" }, specularVec, ref keyboardState);
                        if (light.properties.specular.Xyz != specular)
                            light.properties.specular = new Vector4(specular, 1.0f);

                        float[] specularPowVec = new float[] { light.properties.specularPow };
                        float specularPow = InputFloat1("SpecularPow", new string[] { "X" }, specularPowVec, ref keyboardState);
                        if (light.properties.specularPow != specularPow)
                            light.properties.specularPow = specularPow;


                        ImGui.Checkbox("Advanced", ref advancedLightSetting);

                        if (!advancedLightSetting)
                        {
                            float[] rangeVec = new float[] { pl.range };
                            float range = InputFloat1("Range", new string[] { "" }, specularPowVec, ref keyboardState);
                            if (pl.range != range)
                            {
                                pl.range = range;
                                float[] att = PointLight.RangeToAttenuation(range);
                                pl.properties.constant = att[0];
                                pl.properties.linear = att[1];
                                pl.properties.quadratic = att[2];
                            }
                        }
                        else
                        {
                            ImGui.Text("Constant Linear Quadratic");
                            float[] pointVec = new float[] { pl.properties.constant, pl.properties.linear, pl.properties.quadratic };
                            Vector3 point = InputFloat3("Point", new string[] { "Constant", "Linear", "Quadratic" }, pointVec, ref keyboardState, true);
                            if (pl.properties.constant != point[0])
                                pl.properties.constant = point[0];
                            if (pl.properties.linear != point[1])
                                pl.properties.linear = point[1];
                            if (pl.properties.quadratic != point[2])
                                pl.properties.quadratic = point[2];
                        }

                        if (ImGui.SliderFloat("MinBias", ref light.properties.minBias, 0.00001f, 0.01f, "%.6f"))
                        {
                            light.RecalculateShadows();
                            Light.SendUBOToGPU(engine.lights, engine.lightUBO);
                        }
                        if(ImGui.SliderFloat("MaxBias", ref light.properties.maxBias, 0.00001f, 0.01f, "%.6f"))
                        {
                            light.RecalculateShadows();
                            Light.SendUBOToGPU(engine.lights, engine.lightUBO);
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
