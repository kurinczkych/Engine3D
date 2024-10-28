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
                else if (lightType == LightType.PointLight)
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

                ImGui.PopStyleVar();
                #endregion

                ImGui.TreePop();
            }
        }
    }
}
