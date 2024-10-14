using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using Assimp.Configs;
using Assimp.Unmanaged;
using FontStashSharp;
using HtmlAgilityPack;
using OpenTK.Mathematics;
using static OpenTK.Graphics.OpenGL.GL;

namespace Engine3D
{

    public class AssimpManager
    {
        private AssimpContext context;
        public Dictionary<string, AnimationClip> animations = new Dictionary<string, AnimationClip>();

        public AssimpManager()
        {
            context = new AssimpContext();
        }

        public void ProcessAnimation(string relativeAnimationPath)
        {
            string filePath = Environment.CurrentDirectory + "\\Assets\\" + FileType.Animations.ToString() + "\\" + relativeAnimationPath;
            if (!File.Exists(filePath))
            {
                Engine.consoleManager.AddLog("File '" + relativeAnimationPath + "' not found!", LogType.Warning);
                return;
            }

            var scene = context.ImportFile("Assets\\" + FileType.Animations.ToString() + "\\" + relativeAnimationPath);

            throw new NotImplementedException();

            //if (scene.AnimationCount > 0)
            //{
            //    foreach(var anim in scene.Animations)
            //    {
            //        AnimationClip animClip = new AnimationClip();
            //        animClip.DurationInTicks = anim.DurationInTicks;
            //        animClip.TicksPerSecond = anim.TicksPerSecond;
            //        animClip.Name = anim.Name;

            //        foreach (var node in anim.NodeAnimationChannels)
            //        {
            //            AnimationPose animPose = new AnimationPose();

            //            for (int i = 0; i < node.PositionKeyCount; i++)
            //                animPose.AddTranslationKey(AssimpVector3(node.PositionKeys[i].Value), node.PositionKeys[i].Time);
            //            for (int i = 0; i < node.RotationKeyCount; i++)
            //                animPose.AddRotationKey(AssimpQuaternion(node.RotationKeys[i].Value), node.RotationKeys[i].Time);
            //            for (int i = 0; i < node.ScalingKeyCount; i++)
            //                animPose.AddScalingKey(AssimpVector3(node.ScalingKeys[i].Value), node.ScalingKeys[i].Time);

            //            animClip.AddAnimationPose(node.NodeName, animPose);
            //        }

            //        if (!animations.ContainsKey(anim.Name))
            //            animations.Add(anim.Name, animClip);
            //    }
            //}

        }

        private void AddAnimation(Assimp.Animation anim)
        {
            throw new NotImplementedException();
            //AnimationClip animClip = new AnimationClip();
            //animClip.DurationInTicks = anim.DurationInTicks;
            //animClip.TicksPerSecond = anim.TicksPerSecond;
            //animClip.Name = anim.Name;

            //foreach (var node in anim.NodeAnimationChannels)
            //{
            //    AnimationPose animPose = new AnimationPose();

            //    for (int i = 0; i < node.PositionKeyCount; i++)
            //        animPose.AddTranslationKey(AssimpVector3(node.PositionKeys[i].Value), node.PositionKeys[i].Time);
            //    for (int i = 0; i < node.RotationKeyCount; i++)
            //        animPose.AddRotationKey(AssimpQuaternion(node.RotationKeys[i].Value), node.RotationKeys[i].Time);
            //    for (int i = 0; i < node.ScalingKeyCount; i++)
            //        animPose.AddScalingKey(AssimpVector3(node.ScalingKeys[i].Value), node.ScalingKeys[i].Time);

            //    animClip.AddAnimationPose(node.NodeName, animPose);
                
            //}

            //if(!animations.ContainsKey(anim.Name))
            //    animations.Add(anim.Name, animClip);
        }

        public ModelData? ProcessModel(string relativeModelPath, float cr = 1, float cg = 1, float cb = 1, float ca = 1)
        {
            ModelData modelData = new ModelData();

            Color4 color = new Color4(cr, cg, cb, ca);

            string filePath = Environment.CurrentDirectory + "\\Assets\\" + FileType.Models.ToString() + "\\" + relativeModelPath;
            if (!File.Exists(filePath))
            {
                Engine.consoleManager.AddLog("File '" + relativeModelPath + "' not found!", LogType.Warning);
                return null;
            }

            var scene = context.ImportFile("Assets\\" + FileType.Models.ToString() + "\\" + relativeModelPath, 
                /*PostProcessSteps.LimitBoneWeights |*/ PostProcessSteps.Triangulate | PostProcessSteps.JoinIdenticalVertices);

            foreach (var anim in scene.Animations)
                AddAnimation(anim);

            modelData.meshes = scene.Meshes.Select(x => new MeshData(x)).ToList();
            modelData.materials = new List<Material>(scene.Materials);

            return modelData;

        }

        
        public static Matrix4 AssimpMatrix4(Assimp.Matrix4x4 m)
        {
            Matrix4 to = new Matrix4();
            //for (int x = 0; x < 4; x++)
            //{
            //    for (int y = 0; y < 4; y++)
            //    {
            //        to[x, y] = m[x, y];
            //    }
            //}
            //matrix4.Transpose();

            to[0, 0] = m.A1; to[0, 1] = m.B1; to[0, 2] = m.C1; to[0, 3] = m.D1;
            to[1, 0] = m.A2; to[1, 1] = m.B2; to[1, 2] = m.C2; to[1, 3] = m.D2;
            to[2, 0] = m.A3; to[2, 1] = m.B3; to[2, 2] = m.C3; to[2, 3] = m.D3;
            to[3, 0] = m.A4; to[3, 1] = m.B4; to[3, 2] = m.C4; to[3, 3] = m.D4;
            to.Transpose();
            return to;
        }
        public static OpenTK.Mathematics.Quaternion AssimpQuaternion(Assimp.Quaternion quat)
        {
            return new OpenTK.Mathematics.Quaternion(quat.X, quat.Y, quat.Z, quat.W);
        }
    }
}
