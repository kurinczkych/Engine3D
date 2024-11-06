using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static System.Net.WebRequestMethods;

namespace Engine3D
{
    public class Camera : IComponent
    {
        public Vector2 screenSize { get; private set; }
        public Vector2 gameScreenSize { get; private set; }
        public Vector2 gameScreenPos { get; private set; }
        public float near;
        public float far;
        public float fov;
        public float aspectRatio;

        public Vector3 up { get; private set; } = Vector3.UnitY;
        public Vector3 front { get; private set; } = -Vector3.UnitZ;
        public Vector3 frontClamped { get; private set; } = -Vector3.UnitZ;
        public Vector3 right { get; private set; } = Vector3.UnitX;

        private float yaw;
        private float pitch = 0f;

        public Matrix4 viewMatrix;
        public Matrix4 projectionMatrix;
        public Matrix4 projectionMatrixBigger;
        public Matrix4 projectionMatrixOrtho;
        public Frustum frustum;

        [JsonIgnore]
        public Object parentObject;

        public Camera()
        {
            
        }

        public Camera(Vector2 screenSize, Object parentObject)
        {
            this.screenSize = screenSize;
            this.parentObject = parentObject;

            near = 0.1f;
            far = 1000.0f;
            fov = 60.0f;
            aspectRatio = gameScreenSize.X / gameScreenSize.Y;

            UpdateAll();
        }

        #region Setters and getters
        public Vector3 GetPosition()
        {
            return parentObject.transformation.Position;
        }
        public void SetPosition(Vector3 position)
        {
            if (this.parentObject.transformation.Position != position)
            {
                this.parentObject.transformation.Position = position;
                UpdateVectors();
            }
        }

        public float GetYaw()
        {
            return yaw;
        }
        public void SetYaw(float yaw)
        {
            if (this.yaw != yaw)
            {
                this.yaw = yaw;
                if (this.yaw > 360)
                    this.yaw = 0;
                if (this.yaw < 0)
                    this.yaw = 360;
                UpdateVectors();
            }
        }

        public float GetPitch()
        {
            return pitch;
        }
        public void SetPitch(float pitch)
        {
            if (this.pitch != pitch)
            {
                this.pitch = pitch;
                UpdateVectors();
            }
        }

        public void SetScreenSize(Vector2 size, Vector2 gameSize, Vector2 gamePos)
        {
            screenSize = size;
            gameScreenSize = gameSize;
            gameScreenPos = gamePos;

            aspectRatio = gameScreenSize.X / gameScreenSize.Y;

            viewMatrix = GetViewMatrix();
            projectionMatrix = GetProjectionMatrix();
            projectionMatrixBigger = GetProjectionMatrixBigger(1.3f);
            projectionMatrixOrtho = GetProjectionMatrixOrtho();
        }
        #endregion

        public void UpdateAll()
        {
            UpdateVectors();
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position + front, up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), aspectRatio, near, far);
        }

        public Matrix4 GetProjectionMatrixBigger(float fovMult = 1.1f)
        {
            float fovBig = fov * fovMult;
            if (fovBig > 179)
                fovBig = 179;
            return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fovBig), aspectRatio, near, far);
        }

        public Matrix4 GetProjectionMatrixOrtho()
        {
            float l = -25.0f;
            float r =  25.0f;
            float t =  25.0f/* / aspectRatio*/;
            float b = -25.0f/* / aspectRatio*/;
            float n = near; // 0.1
            float f = far; // 1000

            Matrix4 m = Matrix4.CreateOrthographic(r - l, t - b, n, f);

            return m;
        }

        public static Matrix4 GetProjectionMatrixOrthoShadow(Vector3 minLightSpace, Vector3 maxLightSpace)
        {

            // NEW CHATGPT APPROACH FOR TIGHT FIT
            // Calculate orthographic bounds from transformed min/max light-space coordinates
            float left = minLightSpace.X;
            float right = maxLightSpace.X;
            float bottom = minLightSpace.Y;
            float top = maxLightSpace.Y;
            float near = -maxLightSpace.Z; // Light's near plane (Z inverted)
            float far = -minLightSpace.Z;  // Light's far plane (Z inverted)

            // Return the orthographic projection matrix
            return Matrix4.CreateOrthographicOffCenter(left, right, bottom, top, near, far);

            // MY APPROACH ------------------------------------------ Working but not tight light view proj
            //float l = -25.0f;
            //float r = 25.0f;
            //float t = 25.0f/* / aspectRatio*/;
            //float b = -25.0f/* / aspectRatio*/;
            //float n = 5; // 0.1
            //float f = 1000; // 1000

            //Matrix4 m = Matrix4.CreateOrthographic(r - l, t - b, n, f);

            //return m;

            // OGLDEV APPROACH ------------------------------------------ NOT WORKING
            //Frustum f = new Frustum();
            //f.CalcCorners(gameScreenSize.X, gameScreenSize.Y, near, far, fov);

            //Matrix4 inverseCamView = viewMatrix.Inverted();
            //f.Transform(inverseCamView);

            //Frustum viewFrustumInWorldSpace = f.GetCopy();

            //Matrix4 lightView = ShadowMapFBO.GetLightViewMatrix(new Vector3(0, -1, 0));
            //f.Transform(lightView);

            //AABB aabb = f.CalcAABB();

            //Vector3 bottomLeft = new Vector3(aabb.Min.X, aabb.Min.Y, aabb.Min.Z);
            //Vector3 topRight = new Vector3(aabb.Max.X, aabb.Max.Y, aabb.Min.Z);
            //Vector4 lightPosWorld4d = new Vector4((bottomLeft + topRight) / 2.0f, 1.0f);

            //Matrix4 lightViewInv = lightView.Inverted();
            //lightPosWorld4d = lightViewInv * lightPosWorld4d;
            //Vector3 lightPosWorld = new Vector3(lightPosWorld4d.X, lightPosWorld4d.Y, lightPosWorld4d.Z);

            //Matrix4 lightView2 = ShadowMapFBO.GetLightViewMatrix(new Vector3(0, -1, 0), lightPosWorld);
            //viewFrustumInWorldSpace.Transform(lightView2);

            //AABB final_aabb = viewFrustumInWorldSpace.CalcAABB();
            //Matrix4 lightProjection = Matrix4.CreateOrthographicOffCenter(final_aabb.Min.X, final_aabb.Max.X, final_aabb.Min.Y, final_aabb.Max.Y, final_aabb.Min.Z, final_aabb.Max.Z);
            //return lightProjection;


            // CHATGPT APPROACH ------------------------------------------ Flickering
            //Vector3 lightDirection = new Vector3(0, -1, 0);

            //Vector3[] frustumCorners = GetFrustumCornersWorldSpace();

            //// Step 2: Transform the frustum corners into light space
            //Matrix4 lightView = ShadowMapFBO.GetLightViewMatrix(lightDirection);
            //Vector3 min = new Vector3(float.MaxValue);
            //Vector3 max = new Vector3(float.MinValue);

            //for (int i = 0; i < 8; i++)
            //{
            //    Vector4 transformedCorner = new Vector4(frustumCorners[i], 1.0f);

            //    // Multiply with the light's view matrix to get light-space coordinates
            //    transformedCorner = lightView * transformedCorner;

            //    // Update the min/max bounds for the transformed coordinates
            //    min = Vector3.ComponentMin(min, transformedCorner.Xyz);
            //    max = Vector3.ComponentMax(max, transformedCorner.Xyz);
            //}

            //// Step 3: Create the orthographic projection matrix that tightly fits the frustum bounds
            //float l = min.X;
            //float r = max.X;
            //float b = min.Y;
            //float t = max.Y;

            //// Ensure near (n) and far (f) are positive
            //float n = Math.Min(min.Z, max.Z);  // Near is the closest (smallest) Z value
            //float f = Math.Max(min.Z, max.Z);

            //Matrix4 lightProjection = Matrix4.CreateOrthographicOffCenter(l, r, b, t, n, f);

            //return lightProjection;
        }

        private Vector3[] GetFrustumCornersWorldSpace()
        {
            Vector3[] frustumCorners = new Vector3[8];

            float halfHeightNear = (float)Math.Tan(MathHelper.DegreesToRadians(fov / 2.0f)) * near;
            float halfWidthNear = halfHeightNear * aspectRatio;
            float halfHeightFar = (float)Math.Tan(MathHelper.DegreesToRadians(fov / 2.0f)) * far;
            float halfWidthFar = halfHeightFar * aspectRatio;

            // Near plane corners
            Vector3 nearCenter = parentObject.transformation.Position + front * near;
            frustumCorners[0] = nearCenter + (up * halfHeightNear) - (right * halfWidthNear);  // Top-Left
            frustumCorners[1] = nearCenter + (up * halfHeightNear) + (right * halfWidthNear);  // Top-Right
            frustumCorners[2] = nearCenter - (up * halfHeightNear) - (right * halfWidthNear);  // Bottom-Left
            frustumCorners[3] = nearCenter - (up * halfHeightNear) + (right * halfWidthNear);  // Bottom-Right

            // Far plane corners
            Vector3 farCenter = parentObject.transformation.Position + front * far;
            frustumCorners[4] = farCenter + (up * halfHeightFar) - (right * halfWidthFar);  // Top-Left
            frustumCorners[5] = farCenter + (up * halfHeightFar) + (right * halfWidthFar);  // Top-Right
            frustumCorners[6] = farCenter - (up * halfHeightFar) - (right * halfWidthFar);  // Bottom-Left
            frustumCorners[7] = farCenter - (up * halfHeightFar) + (right * halfWidthFar);  // Bottom-Right

            return frustumCorners;
        }

        public Vector3 GetCameraRay(Vector2 screenPoint)
        {
            Vector2 relativeScreenPoint = new Vector2(
                screenPoint.X - gameScreenPos.X,
                screenPoint.Y - (screenSize.Y - gameScreenPos.Y - gameScreenSize.Y)
            );

            Vector2 ndc = new Vector2(
                (2.0f * relativeScreenPoint.X) / gameScreenSize.X - 1.0f,
                1.0f - (2.0f * relativeScreenPoint.Y) / gameScreenSize.Y
            );

            // Convert to clip space coordinates
            Vector4 clipCoords = new Vector4(ndc.X, ndc.Y, -1f, 1f);

            // Convert to eye space
            Vector4 eyeCoords = Vector4.TransformRow(clipCoords, Matrix4.Invert(projectionMatrix));
            eyeCoords = new Vector4(eyeCoords.X, eyeCoords.Y, -1f, 0f);

            // Convert to world space
            Vector3 worldRay = Vector4.TransformRow(eyeCoords, Matrix4.Invert(viewMatrix)).Xyz;

            // Normalize the ray direction
            worldRay.Normalize();

            return worldRay;
        }

        public bool IsTriangleClose(triangle tri)
        {
            float dist = float.PositiveInfinity;
            foreach (var vertex in tri.v)
            {
                float ddist = (parentObject.transformation.Position - vertex.p).Length;
                if (ddist < dist)
                    dist = ddist;
            }

            return dist < 15.0f;

        }

        public bool IsPointClose(Vector3 p)
        {
            float dist = (parentObject.transformation.Position - p).Length;

            return dist < 15.0f;

        }

        public bool IsAnyTriangleClose(List<triangle> tris)
        {
            float dist = float.PositiveInfinity;
            foreach(triangle triangle in tris)
            {
                foreach (var vertex in triangle.v)
                {
                    float ddist = (parentObject.transformation.Position - vertex.p).Length;
                    if (ddist < dist)
                        dist = ddist;
                }
            }

            return dist < 15.0f;

        }

        public bool IsAABBClose(AABB aabb)
        {
            Vector3[] corners = aabb.GetCorners();
            float dist = float.PositiveInfinity;
            foreach (var corner in corners)
            {
                float ddist = (parentObject.transformation.Position - corner).Length;
                if(ddist < dist) 
                    dist = ddist;
            }

            return dist < 15.0f;

        }

        public bool IsLineClose(Line line)
        {
            float dist1 = (parentObject.transformation.Position - line.Start).Length;
            float dist2 = (parentObject.transformation.Position - line.End).Length;
            float dist = float.PositiveInfinity;
            if (dist1 < dist) dist = dist1;
            if (dist2 < dist) dist = dist2;

            return dist < 15.0f;

        }

        private Frustum GetFrustum()
        {
            Frustum frustum = new Frustum();
            Matrix4 m = viewMatrix;
            m = m * projectionMatrixBigger;

            //right
            frustum.planes[0].normal.X = m.Row0[3] - m.Row0[0];
            frustum.planes[0].normal.Y = m.Row1[3] - m.Row1[0];
            frustum.planes[0].normal.Z = m.Row2[3] - m.Row2[0];
            frustum.planes[0].distance = m.Row3[3] - m.Row3[0];
                   
            //left
            frustum.planes[1].normal.X = m.Row0[3] + m.Row0[0];
            frustum.planes[1].normal.Y = m.Row1[3] + m.Row1[0];
            frustum.planes[1].normal.Z = m.Row2[3] + m.Row2[0];
            frustum.planes[1].distance = m.Row3[3] + m.Row3[0];
                
            //down
            frustum.planes[2].normal.X = m.Row0[3] + m.Row0[1];
            frustum.planes[2].normal.Y = m.Row1[3] + m.Row1[1];
            frustum.planes[2].normal.Z = m.Row2[3] + m.Row2[1];
            frustum.planes[2].distance = m.Row3[3] + m.Row3[1];
                
            //up
            frustum.planes[3].normal.X = m.Row0[3] - m.Row0[1];
            frustum.planes[3].normal.Y = m.Row1[3] - m.Row1[1];
            frustum.planes[3].normal.Z = m.Row2[3] - m.Row2[1];
            frustum.planes[3].distance = m.Row3[3] - m.Row3[1];
              
            //far
            frustum.planes[4].normal.X = m.Row0[3] - m.Row0[2];
            frustum.planes[4].normal.Y = m.Row1[3] - m.Row1[2];
            frustum.planes[4].normal.Z = m.Row2[3] - m.Row2[2];
            frustum.planes[4].distance = m.Row3[3] - m.Row3[2];
                     
            //near
            frustum.planes[5].normal.X = m.Row0[3] + m.Row0[2];
            frustum.planes[5].normal.Y = m.Row1[3] + m.Row1[2];
            frustum.planes[5].normal.Z = m.Row2[3] + m.Row2[2];
            frustum.planes[5].distance = m.Row3[3] + m.Row3[2];

            for (int i = 0; i < 6; i++)
            {
                float magnitude = frustum.planes[i].normal.Length;
                frustum.planes[i].normal /= magnitude;
                frustum.planes[i].distance /= magnitude;
            }

            return frustum;
        }

        public void UpdatePositionToGround(List<triangle> groundTriangles)
        {
            float offsetHeight = 4f;

            foreach (var triangle in groundTriangles)
            {
                // Check if character is above this triangle.
                if (triangle.IsPointInTriangle(parentObject.transformation.Position, out float distanceToTriangle))
                {
                    // Compute the triangle's normal.
                    Vector3 normal = Vector3.Cross(triangle.v[1].p - triangle.v[0].p, triangle.v[2].p - triangle.v[0].p).Normalized();

                    // Adjust the character's position based on the triangle's normal and the computed distance.
                    parentObject.transformation.Position -= normal * (distanceToTriangle - offsetHeight);

                    break; // Exit once the correct triangle is found.
                }
            }
        }

        public void UpdateVectors()
        {
            if (pitch > 89f)
                pitch = 89f;

            if (pitch < -89f)
                pitch = -89f;

            Vector3 front_ = new Vector3()
            {
                X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw)),
                Y = MathF.Sin(MathHelper.DegreesToRadians(pitch)),
                Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw)),
            };
            front = front_;

            frontClamped = new Vector3(front.X, 0, front.Z);

            front.Normalize();
            frontClamped.Normalize();

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));

            viewMatrix = GetViewMatrix();
            projectionMatrix = GetProjectionMatrix();
            projectionMatrixBigger = GetProjectionMatrixBigger(1.3f);
            projectionMatrixOrtho = GetProjectionMatrixOrtho();
            frustum = GetFrustum(); 
        }
    }
}
