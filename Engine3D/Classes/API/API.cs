using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class Engine : GameWindow
    {
        public void AddRenderMethod(RenderDelegate renderMethod)
        {
            renderMethods.Add(renderMethod);
        }

        public void AddUpdateMethod(UpdateDelegate updateMethod)
        {
            updateMethods.Add(updateMethod);
        }

        public void AddUnloadMethod(UnloadDelegate unloadMethod)
        {
            unloadMethods.Add(unloadMethod);
        }

        public void AddOnLoadMethod(OnLoadDelegate onLoadMethod)
        {
            onLoadMethods.Add(onLoadMethod);
        }

        public void AddCharInputMethod(CharInputDelegate charInputMethod)
        {
            charInputMethods.Add(charInputMethod);
        }

        public void AddMouseWheelInputMethod(MouseWheelInputDelegate mouseWheelInputMethod)
        {
            mouseWheelInputMethods.Add(mouseWheelInputMethod);
        }

        public void SubscribeToResizeEvent(WindowResizedDelegate res)
        {
            windowResized = res;
        }

        public void SubscribeToObjectSelectedEvent(ObjectSelectedDelegate o)
        {
            objectSelected = o;
        }

        public MouseCursor GetCursor()
        {
            return Cursor;
        }

        public void SetCursor(MouseCursor cursor)
        {
            Cursor = cursor;
        }

        public CursorState GetCursorState()
        {
            return CursorState;
        }

        public void SetCursorState(CursorState cursorState)
        {
            CursorState = cursorState;
        }

        public KeyboardState GetKeyboardState()
        {
            return KeyboardState;
        }

        public MouseState GetMouseState()
        {
            return MouseState;
        }

        public GizmoManager GetGizmoManager()
        {
            return gizmoManager;
        }

        public void SetGizmoWindow(Vector2 size, Vector2 pos)
        {
            if(gizmoWindowPos != pos)
                gizmoWindowPos = pos;
            if(gizmoWindowSize != size)
                gizmoWindowSize = size;
        }

        public void SetUIHasMouse(bool hasMouse)
        {
            UIHasMouse = hasMouse;
        }

        public void SetSelectedObject(Object? o)
        {
            selectedObject = o;
        }

        public void SetGizmoInstIndex(int index)
        {
            gizmoManager.instIndex = index;
        }

        public void SetGameWindow(GameWindowProperty gameWindowProperty)
        {
            this.gameWindowProperty = gameWindowProperty;
        }

        public void ResizedEditorWindow(Vector2 gameWindowSize, Vector2 gameWindowPos)
        {
            gameWindowProperty.gameWindowSize = gameWindowSize;
            gameWindowProperty.gameWindowPos = gameWindowPos;

            if (mainCamera != null)
                mainCamera.SetScreenSize(windowSize, gameWindowSize, gameWindowPos);

            ResizeFramebuffer(gameWindowSize);
        }

        public void SetRunParticles(bool value)
        {
            runParticles = value;
        }

        public void ResetParticles()
        {
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.RemoveAllParticles();
            }
        }

        public int GetFps()
        {
            return fps.fps;
        }

        public string GetFpsString()
        {
            return fps.GetFpsString();
        }

        public List<Object> GetObjects()
        {
            return objects;
        }

        private void AddObjectAndCalculate(Object o)
        {
            objects.Add(o);
            _meshObjects.Add(o);
            o.transformation.Position = mainCamera.GetPosition() + mainCamera.front * 5;
            BaseMesh? mesh = (BaseMesh?)o.GetComponent<BaseMesh>();
            if (mesh != null)
                mesh.RecalculateModelMatrix(new bool[] { true, false, false });
        }

        public void AddObject(ObjectType type)
        {
            if (type == ObjectType.Cube)
            {
                Object o = new Object(type);
                o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "cube", BaseMesh.GetUnitCube(), windowSize, ref mainCamera, ref o));
                AddObjectAndCalculate(o);
            }
            else if (type == ObjectType.Sphere)
            {
                Object o = new Object(type);
                o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "sphere", BaseMesh.GetUnitSphere(), windowSize, ref mainCamera, ref o));
                AddObjectAndCalculate(o);
            }
            else if (type == ObjectType.Capsule)
            {
                Object o = new Object(type);
                o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "capsule", BaseMesh.GetUnitCapsule(), windowSize, ref mainCamera, ref o));
                AddObjectAndCalculate(o);
            }
            else if (type == ObjectType.Plane)
            {
                Object o = new Object(type);
                o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "plane", BaseMesh.GetUnitFace(), windowSize, ref mainCamera, ref o));
                AddObjectAndCalculate(o);
            }
            else if (type == ObjectType.TriangleMesh)
            {
                Object o = new Object(type);
                o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, "mesh", new ModelData(), windowSize, ref mainCamera, ref o));
                AddObjectAndCalculate(o);
            }
            else if (type == ObjectType.Empty)
            {
                Object o = new Object(type);
                o.transformation.Position = mainCamera.GetPosition() + mainCamera.front * 5;
                objects.Add(o);
            }
        }

        public void AddMeshObject(string meshName)
        {
            Object o = new Object(ObjectType.TriangleMesh);
            o.AddMesh(new Mesh(meshVao, meshVbo, shaderProgram.id, Path.GetFileName(meshName), windowSize, ref mainCamera, ref o));
            AddObjectAndCalculate(o);
        }

        public void AddLight(Light.LightType lightType)
        {
            objects.Add(new Object(ObjectType.Empty) { name = "Light" });
            objects[objects.Count - 1].components.Add(new Light(objects[objects.Count - 1], shaderProgram.id, 0, lightType));
            objects[objects.Count - 1].transformation.Position = mainCamera.GetPosition() + mainCamera.front * 5;

            lights = new List<Light>();
        }

        public void AddParticleSystem()
        {
            Object o = new Object(ObjectType.Empty) { name = "ParticleSystem" };
            o.transformation.Position = mainCamera.GetPosition() + mainCamera.front * 5;
            o.components.Add(new ParticleSystem(instancedMeshVao, instancedMeshVbo, instancedShaderProgram.id, windowSize, ref mainCamera, ref o));
            objects.Add(o);

            particleSystems = new List<ParticleSystem>();
        }

        public void RemoveObject(Object o)
        {
            if (o.GetComponent<BaseMesh>() is BaseMesh mesh)
            {
                if (mesh.GetType() == typeof(Mesh))
                    _meshObjects.Remove(o);
                else if (mesh.GetType() == typeof(InstancedMesh))
                    _instObjects.Remove(o);

                textureManager.DeleteTexture(mesh.textureName);
            }

            lights = new List<Light>();
            particleSystems = new List<ParticleSystem>();
            o.Delete(ref textureManager);
            objects.Remove(o);
        }


        public void AddText(Object obj, string tag)
        {
            BaseMesh? textMesh = (BaseMesh?)obj.GetComponent<BaseMesh>();
            if (textMesh == null)
                throw new Exception("Can only add Text, if the object has a TextMesh");


            if (obj.GetObjectType() != ObjectType.TextMesh && textMesh.GetType() != typeof(TextMesh))
                throw new Exception("Can only add Text, if its ObjectType.TextMesh");

            haveText = true;
            objects.Add(obj);
            texts.Add(tag, (TextMesh)textMesh);
            objects.Sort();
        }

        public void SetGameState(GameState state)
        {
            gameState = state;
        }

        public AssetManager GetAssetManager()
        {
            return assetManager;
        }

        public TextureManager GetTextureManager()
        {
            return textureManager;
        }

        private void RenderInfiniteFloor()
        {
            GL.Disable(EnableCap.CullFace);
            infiniteFloorShader.Use();

            Vector3 cameraPos = mainCamera.GetPosition();

            Matrix4 projectionMatrix = mainCamera.projectionMatrix;
            Matrix4 viewMatrix = mainCamera.viewMatrix;
            Matrix4 scaleMatrix = Matrix4.CreateScale(10000, 1, 10000);
            Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion(new OpenTK.Mathematics.Quaternion(0, 0, 0));
            Matrix4 translationMatrix = Matrix4.CreateTranslation(0, 0, 0);

            Matrix4 modelMatrix = scaleMatrix * rotationMatrix * translationMatrix;

            GL.UniformMatrix4(GL.GetUniformLocation(infiniteFloorShader.id, "modelMatrix"), true, ref modelMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(infiniteFloorShader.id, "viewMatrix"), true, ref viewMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(infiniteFloorShader.id, "projectionMatrix"), true, ref projectionMatrix);
            GL.Uniform3(GL.GetUniformLocation(infiniteFloorShader.id, "cameraPos"), ref cameraPos);
            GL.Uniform3(GL.GetUniformLocation(infiniteFloorShader.id, "bgColor"), backgroundColor.R, backgroundColor.G, backgroundColor.B);
            GL.Uniform3(GL.GetUniformLocation(infiniteFloorShader.id, "lineColor"), gridColor.R, gridColor.G, gridColor.B);

            infiniteFloorVao.Bind();
            List<float> gridVertices = new List<float>
            {
                // Position data (X, Y, Z)
                -1.0f, 0.0f, -1.0f,  // Bottom-left
                 1.0f, 0.0f, -1.0f,  // Bottom-right
                 1.0f, 0.0f,  1.0f,  // Top-right
                -1.0f, 0.0f,  1.0f   // Top-left
            };
            infiniteFloorVbo.Buffer(gridVertices);
            GL.DrawArrays(OpenTK.Graphics.OpenGL4.PrimitiveType.TriangleFan, 0, 4);

            infiniteFloorVao.Unbind();
            GL.Enable(EnableCap.CullFace);
        }
    }
}
