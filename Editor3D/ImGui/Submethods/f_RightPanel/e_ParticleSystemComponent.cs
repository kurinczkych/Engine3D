using ImGuiNET;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void ParticleSystemComponent(ref List<IComponent> toRemoveComp, ref ImGuiStylePtr style, ref ParticleSystem ps, IComponent c)
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Once);
            if (ImGui.TreeNode("Particle System"))
            {
                ImGui.SameLine();
                var origDeleteX = RightAlignCursor(70);
                ImGui.PushStyleColor(ImGuiCol.Button, style.Colors[(int)ImGuiCol.FrameBg]);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                if (ImGui.Button("Delete", new System.Numerics.Vector2(70, 20)))
                {
                    toRemoveComp.Add(c);
                }
                ImGui.PopStyleVar();
                ImGui.PopStyleColor();
                ImGui.SetCursorPosX(origDeleteX);
                ImGui.Dummy(new System.Numerics.Vector2(0, 0));

                #region ParticleSystem

                float[] emitTimeSecVec = new float[] { ps.emitTimeSec };
                float emitTimeSec = InputFloat1("Emit Time(sec)", new string[] { "" }, emitTimeSecVec, ref keyboardState);
                if (ps.emitTimeSec != emitTimeSec)
                    ps.emitTimeSec = emitTimeSec;

                ImGui.Separator();
                ImGui.Checkbox("##randomLifeTime", ref ps.randomLifeTime);
                ImGui.SameLine();
                ImGui.Text("Random lifetime");

                if (!ps.randomLifeTime)
                {
                    float[] lifetimeVec = new float[] { ps.lifetime };
                    float lifetime = InputFloat1("Lifetime(sec)", new string[] { "" }, lifetimeVec, ref keyboardState, titleSameLine: true);
                    if (ps.lifetime != lifetime)
                        ps.lifetime = lifetime;
                }
                else
                {
                    float[] lifetimeVec = new float[] { ps.xLifeTime, ps.yLifeTime };
                    float[] lifetime = InputFloat2("Lifetime(sec)", new string[] { "From", "To" }, lifetimeVec, ref keyboardState);
                    if (ps.xLifeTime != lifetime[0])
                        ps.xLifeTime = lifetime[0];
                    if (ps.yLifeTime != lifetime[1])
                        ps.yLifeTime = lifetime[1];
                }

                ImGui.Separator();
                ImGui.Checkbox("##randomStartPos", ref ps.randomStartPos);
                ImGui.SameLine();
                ImGui.Text("Random starting position");

                if (!ps.randomStartPos)
                {
                    float[] startPosVec = new float[] { ps.startPos.X, ps.startPos.Y, ps.startPos.Z };
                    Vector3 startPos = InputFloat3("Starting position", new string[] { "X", "Y", "Z" }, startPosVec, ref keyboardState);
                    if (ps.startPos != startPos)
                        ps.startPos = startPos;
                }
                else
                {
                    float[] startPosMinVec = new float[] { ps.xStartPos.Min.X, ps.xStartPos.Min.Y, ps.xStartPos.Min.Z };
                    Vector3 startPosMin = InputFloat3("Starting min 3D corner", new string[] { "X", "Y", "Z" }, startPosMinVec, ref keyboardState);
                    if (ps.xStartPos.Min != startPosMin)
                        ps.xStartPos.Min = startPosMin;

                    float[] startPosMaxVec = new float[] { ps.xStartPos.Max.X, ps.xStartPos.Max.Y, ps.xStartPos.Max.Z };
                    Vector3 startPosMax = InputFloat3("Starting max 3D corner", new string[] { "X", "Y", "Z" }, startPosMaxVec, ref keyboardState);
                    if (ps.xStartPos.Max != startPosMax)
                        ps.xStartPos.Max = startPosMax;
                }

                ImGui.Separator();
                ImGui.Checkbox("##randomStartDir", ref ps.randomDir);
                ImGui.SameLine();
                ImGui.Text("Random starting direction");

                if (!ps.randomDir)
                {
                    float[] startDirVec = new float[] { ps.startDir.X, ps.startDir.Y, ps.startDir.Z };
                    Vector3 startDir = InputFloat3("Starting direction", new string[] { "X", "Y", "Z" }, startDirVec, ref keyboardState);
                    if (ps.startDir != startDir)
                        ps.startDir = startDir;
                }

                ImGui.Separator();
                ImGui.Checkbox("##randomSpeed", ref ps.randomSpeed);
                ImGui.SameLine();
                ImGui.Text("Random speed");

                if (!ps.randomSpeed)
                {
                    float[] speedVec = new float[] { ps.startSpeed, ps.endSpeed };
                    float[] speed = InputFloat2("", new string[] { "Start", "End" }, speedVec, ref keyboardState);
                    if (ps.startSpeed != speed[0])
                        ps.startSpeed = speed[0];
                    if (ps.endSpeed != speed[1])
                        ps.endSpeed = speed[1];
                }
                else
                {
                    float[] speedStartVec = new float[] { ps.xStartSpeed, ps.yStartSpeed };
                    float[] speedStart = InputFloat2("Start speed", new string[] { "From", "To" }, speedStartVec, ref keyboardState);
                    if (ps.xStartSpeed != speedStart[0])
                        ps.xStartSpeed = speedStart[0];
                    if (ps.yStartSpeed != speedStart[1])
                        ps.yStartSpeed = speedStart[1];

                    float[] speedEndVec = new float[] { ps.xEndSpeed, ps.yEndSpeed };
                    float[] speedEnd = InputFloat2("End speed", new string[] { "From", "To" }, speedEndVec, ref keyboardState);
                    if (ps.xEndSpeed != speedEnd[0])
                        ps.xEndSpeed = speedEnd[0];
                    if (ps.yEndSpeed != speedEnd[1])
                        ps.yEndSpeed = speedEnd[1];
                }

                ImGui.Separator();
                ImGui.Checkbox("##randomScale", ref ps.randomScale);
                ImGui.SameLine();
                ImGui.Text("Random scale");

                if (!ps.randomScale)
                {
                    float[] startScaleVec = new float[] { ps.startScale.X, ps.startScale.Y, ps.startScale.Z };
                    Vector3 startScale = InputFloat3("Starting scale", new string[] { "X", "Y", "Z" }, startScaleVec, ref keyboardState);
                    if (ps.startScale != startScale)
                        ps.startScale = startScale;

                    float[] endScaleVec = new float[] { ps.endScale.X, ps.endScale.Y, ps.endScale.Z };
                    Vector3 endScale = InputFloat3("Ending scale", new string[] { "X", "Y", "Z" }, endScaleVec, ref keyboardState);
                    if (ps.endScale != endScale)
                        ps.endScale = endScale;
                }
                else
                {
                    float[] startXScaleMinVec = new float[] { ps.xStartScale.Min.X, ps.xStartScale.Min.Y, ps.xStartScale.Min.Z };
                    Vector3 startXScaleMin = InputFloat3("Starting scale min 3D corner", new string[] { "X", "Y", "Z" }, startXScaleMinVec, ref keyboardState);
                    if (ps.xStartScale.Min != startXScaleMin)
                        ps.xStartScale.Min = startXScaleMin;

                    float[] startXScaleMaxVec = new float[] { ps.xStartScale.Max.X, ps.xStartScale.Max.Y, ps.xStartScale.Max.Z };
                    Vector3 startPosMax = InputFloat3("Starting scale max 3D corner", new string[] { "X", "Y", "Z" }, startXScaleMaxVec, ref keyboardState);
                    if (ps.xStartScale.Max != startPosMax)
                        ps.xStartScale.Max = startPosMax;

                    float[] endXScaleMinVec = new float[] { ps.xEndScale.Min.X, ps.xStartScale.Min.Y, ps.xStartScale.Min.Z };
                    Vector3 endXScaleMin = InputFloat3("Ending scale min 3D corner", new string[] { "X", "Y", "Z" }, endXScaleMinVec, ref keyboardState);
                    if (ps.xStartScale.Min != endXScaleMin)
                        ps.xStartScale.Min = endXScaleMin;

                    float[] endXScaleMaxVec = new float[] { ps.xStartScale.Max.X, ps.xStartScale.Max.Y, ps.xStartScale.Max.Z };
                    Vector3 endPosMax = InputFloat3("Ending scale max 3D corner", new string[] { "X", "Y", "Z" }, endXScaleMaxVec, ref keyboardState);
                    if (ps.xStartScale.Max != endPosMax)
                        ps.xStartScale.Max = endPosMax;
                }

                ImGui.Separator();
                ImGui.Checkbox("##randomColor", ref ps.randomColor);
                ImGui.SameLine();
                ImGui.Text("Random color");

                if (!ps.randomColor)
                {
                    Color4 startColor = ps.startColor;
                    ColorPicker("Start color", ref startColor);
                    ps.startColor = startColor;

                    Color4 endColor = ps.endColor;
                    ColorPicker("Ending color", ref endColor);
                    ps.endColor = endColor;
                }

                //Helper.QuaternionFromEuler
                ImGui.Separator();
                ImGui.Checkbox("##randomRotation", ref ps.randomRotation);
                ImGui.SameLine();
                ImGui.Text("Random rotation");

                if (!ps.randomRotation)
                {
                    Vector3 startRotPS = Helper.EulerFromQuaternion(ps.startRotation);
                    float[] startRotVec = new float[] { startRotPS.X, startRotPS.Y, startRotPS.Z };
                    Vector3 startRot = InputFloat3("Starting rotation", new string[] { "X", "Y", "Z" }, startRotVec, ref keyboardState);
                    OpenTK.Mathematics.Quaternion quatStartRot = Helper.QuaternionFromEuler(startRot);
                    if (ps.startRotation != quatStartRot)
                        ps.startRotation = quatStartRot;

                    Vector3 endRotPS = Helper.EulerFromQuaternion(ps.endRotation);
                    float[] endRotVec = new float[] { endRotPS.X, endRotPS.Y, endRotPS.Z };
                    Vector3 endRot = InputFloat3("Ending rotation", new string[] { "X", "Y", "Z" }, endRotVec, ref keyboardState);
                    OpenTK.Mathematics.Quaternion quatEndRot = Helper.QuaternionFromEuler(endRot);
                    if (ps.startRotation != quatEndRot)
                        ps.startRotation = quatEndRot;
                }

                ImGui.Dummy(new System.Numerics.Vector2(0, 25));

                #endregion

                ImGui.TreePop();
            }

            #region ParticleSystem Update Window
            ImGui.SetNextWindowSize(particlesWindowSize);

            if (particlesWindowPos == null)
            {
                particlesWindowPos = new System.Numerics.Vector2(_windowWidth * (1 - editorData.gameWindow.rightPanelPercent) - particlesWindowSize.X,
                                     _windowHeight * (1 - editorData.gameWindow.bottomPanelPercent) - editorData.gameWindow.bottomPanelSize - particlesWindowSize.Y);
            }

            ImGui.SetNextWindowPos(particlesWindowPos ?? new System.Numerics.Vector2());
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGui.PushStyleColor(ImGuiCol.TitleBg, style.Colors[(int)ImGuiCol.WindowBg]);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, style.Colors[(int)ImGuiCol.WindowBg]);
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, style.Colors[(int)ImGuiCol.WindowBg]);
            if (ImGui.Begin("Particles", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
            {
                ImGui.Separator();
                var button = style.Colors[(int)ImGuiCol.Button];
                style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                if (!editorData.runParticles)
                {
                    ImGui.SetCursorPosX((ImGui.GetWindowSize().X / 2.0f) - 10);
                    if (ImGui.ImageButton("##runParticlesStart", (IntPtr)engineData.textureManager.textures["ui_play.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.runParticles = true;
                        engine.SetRunParticles(true);
                    }
                }
                else
                {
                    ImGui.SetCursorPosX((ImGui.GetWindowSize().X / 2.0f) - 30);
                    if (ImGui.ImageButton("##runParticlesEnd", (IntPtr)engineData.textureManager.textures["ui_stop.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.runParticles = false;
                        engine.SetRunParticles(false);
                        engine.ResetParticles();
                    }
                    ImGui.SameLine();
                    if (ImGui.ImageButton("##runParticlesPause", (IntPtr)engineData.textureManager.textures["ui_pause.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        engine.SetRunParticles(false);
                        editorData.runParticles = false;
                    }
                }
                style.Colors[(int)ImGuiCol.Button] = button;

                ImGui.Text("Particles: " + ps.GetParticleCount());

            }
            if (ImGui.IsWindowHovered())
                editorData.uiHasMouse = true;
            ImGui.End();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            #endregion
        }
    }
}
