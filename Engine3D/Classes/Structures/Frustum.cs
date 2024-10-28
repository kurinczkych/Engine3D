using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Engine3D
{
    public class Frustum
    {
        public Frustum()
        {
            planes = new Plane[6];
            for (int i = 0; i < 6; i++)
            {
                planes[i] = new Plane();
            }
        }

        public bool IsInside(Vector3 point)
        {
            for (int i = 0; i < 6; i++)
            {
                if (Vector3.Dot(planes[i].normal, point) + planes[i].distance < 0)
                    return false;
            }
            return true;
        }

        public bool IsAABBInside(AABB box)
        {
            foreach (var plane in planes)
            {
                int outCount = 0;  // Counter to track how many corners are outside the current plane

                // Check each corner of the AABB against the current plane
                for (float x = 0; x <= 1; x++)
                {
                    for (float y = 0; y <= 1; y++)
                    {
                        for (float z = 0; z <= 1; z++)
                        {
                            Vector3 corner = new Vector3(
                                x > 0.5f ? box.Max.X : box.Min.X,
                                y > 0.5f ? box.Max.Y : box.Min.Y,
                                z > 0.5f ? box.Max.Z : box.Min.Z
                            );

                            if (Vector3.Dot(plane.normal, corner) + plane.distance < 0)
                            {
                                outCount++;
                            }
                        }
                    }
                }

                // If all 8 corners are outside of the current plane, the AABB is outside the frustum
                if (outCount == 8) return false;
            }

            // If we didn't exit early, then the AABB is inside (or intersecting) the frustum
            return true;
        }

        public bool IsTriangleInside(triangle tri)
        {
            for (int i = 0; i < 3; i++)
            {
                if (IsInside(tri.v[i].p))
                    return true;
            }
            return false;
        }

        public bool IsLineInside(Line line)
        {
            var a = IsInside(line.Start);
            var b = IsInside(line.End);
            return a || b;
        }

        public List<float> GetData()
        {
            List<float> data = new List<float>();
            for (int i = 0;i < 6;i++)
            {
                data.Add(planes[i].normal.X);
                data.Add(planes[i].normal.Y);
                data.Add(planes[i].normal.Z);
                data.Add(planes[i].distance);
            }
            return data;
        }

        public List<triangle> GetTriangles()
        {
            List<triangle> triangles = new List<triangle>();

            //foreach (Plane p in planes)
            //    triangles.AddRange(p.GetTriangles());

            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(ntl.X, ntl.Y, ntl.Z);
            corners[1] = new Vector3(ntr.X, ntr.Y, ntr.Z);
            corners[2] = new Vector3(nbl.X, nbl.Y, nbl.Z);
            corners[3] = new Vector3(nbr.X, nbr.Y, nbr.Z);
            corners[4] = new Vector3(ftl.X, ftl.Y, ftl.Z);
            corners[5] = new Vector3(ftr.X, ftr.Y, ftr.Z);
            corners[6] = new Vector3(fbl.X, fbl.Y, fbl.Z);
            corners[7] = new Vector3(fbr.X, fbr.Y, fbr.Z);

            triangles.Add(new triangle(new Vector3[] { corners[0], corners[1], corners[2] }));
            triangles.Add(new triangle(new Vector3[] { corners[1], corners[3], corners[2] }));
            triangles.Add(new triangle(new Vector3[] { corners[4], corners[5], corners[6] }));
            triangles.Add(new triangle(new Vector3[] { corners[5], corners[7], corners[6] }));
            triangles.Add(new triangle(new Vector3[] { corners[0], corners[4], corners[2] }));
            triangles.Add(new triangle(new Vector3[] { corners[4], corners[6], corners[2] }));
            triangles.Add(new triangle(new Vector3[] { corners[1], corners[5], corners[3] }));
            triangles.Add(new triangle(new Vector3[] { corners[5], corners[7], corners[3] }));
            triangles.Add(new triangle(new Vector3[] { corners[0], corners[1], corners[4] }));
            triangles.Add(new triangle(new Vector3[] { corners[1], corners[5], corners[4] }));
            triangles.Add(new triangle(new Vector3[] { corners[2], corners[3], corners[6] }));
            triangles.Add(new triangle(new Vector3[] { corners[3], corners[7], corners[6] }));

            return triangles;
        }

        public void CalcCorners(float width, float height, float near, float far, float fov)
        {
            float AR = height / width;

            float tanHalfFOV = (float)Math.Tan(MathHelper.DegreesToRadians(fov / 2.0f));

            float NearZ = near;
            float NearX = NearZ * tanHalfFOV;
            float NearY = NearZ * tanHalfFOV * AR;

            ntl = new Vector4(-NearX, NearY, NearZ, 1.0f);
            nbl = new Vector4(-NearX, -NearY, NearZ, 1.0f);
            ntr = new Vector4(NearX, NearY, NearZ, 1.0f);
            nbr = new Vector4(NearX, -NearY, NearZ, 1.0f);

            float FarZ = far;
            float FarX = FarZ * tanHalfFOV;
            float FarY = FarZ * tanHalfFOV * AR;

            ftl = new Vector4(-FarX, FarY, FarZ, 1.0f);
            fbl = new Vector4(-FarX, -FarY, FarZ, 1.0f);
            ftr = new Vector4(FarX, FarY, FarZ, 1.0f);
            fbr = new Vector4(FarX, -FarY, FarZ, 1.0f);
        }

        public void Transform(Matrix4 m)
        {
            ntl = m * ntl;
            nbl = m * nbl;
            ntr = m * ntr;
            nbr = m * nbr;

            ftl = m * ftl;
            fbl = m * fbl;
            ftr = m * ftr;
            fbr = m * fbr;
        }

        public AABB CalcAABB()
        {
            AABB aabb = new AABB();
            aabb.Enclose(ntl);
            aabb.Enclose(nbl);
            aabb.Enclose(ntr);
            aabb.Enclose(nbr);
            
            aabb.Enclose(ftl);
            aabb.Enclose(fbl);
            aabb.Enclose(ftr);
            aabb.Enclose(fbr);
            return aabb;
        }

        public Frustum GetCopy()
        {
            Frustum f = new Frustum();
            f.ntl = ntl;
            f.nbl = nbl;
            f.ntr = ntr;
            f.nbr = nbr;

            f.ftl = ftl;
            f.fbl = fbl;
            f.ftr = ftr;
            f.fbr = fbr;
            return f;
        }

        public Plane[] planes;

        public Vector3 nearCenter = Vector3.Zero;
        public Vector3 farCenter = Vector3.Zero;

        public Vector4 ntl = Vector4.Zero;
        public Vector4 ntr = Vector4.Zero;
        public Vector4 nbl = Vector4.Zero;
        public Vector4 nbr = Vector4.Zero;
        public Vector4 ftl = Vector4.Zero;
        public Vector4 ftr = Vector4.Zero;
        public Vector4 fbl = Vector4.Zero;
        public Vector4 fbr = Vector4.Zero;
    }
}
