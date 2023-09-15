using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{

    public class Plane
    {
        public Plane() { }

        public Plane(Vector3 normal, float dist)
        {
            this.normal = normal.Normalized();
            distance = dist;
        }

        // unit vector
        public Vector3 normal = new Vector3(0.0f, 0.0f, 0.0f);

        // distance from origin to the nearest point in the plane
        public float distance = 0.0f;

        public Vector3 PointOnPlane()
        {
            return normal * distance;
        }

        public float DistanceToPoint(Vector3 point)
        {
            return Vector3.Dot(normal, point) + distance;
        }

        public float DistanceToPoint(Vec3d point)
        {
            return Vector3.Dot(normal, new Vector3(point.X, point.Y, point.Z)) + distance;
        }
    };

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

        public bool IsInside(Vec3d point)
        {
            for (int i = 0; i < 6; i++)
            {
                if (Vec3d.Dot(new Vec3d(planes[i].normal.X, planes[i].normal.Y, planes[i].normal.Z), point) + planes[i].distance < 0)
                    return false;
            }

            return true;
        }

        public bool IsTriangleInside(triangle tri)
        {
            var a = IsInside(tri.p[0]);
            var b = IsInside(tri.p[1]);
            var c = IsInside(tri.p[2]);
            return a || b || c;
        }

        public List<triangle> GetTriangles()
        {
            List<triangle> triangles = new List<triangle>();

            //foreach (Plane p in planes)
            //    triangles.AddRange(p.GetTriangles());

            Vec3d[] corners = new Vec3d[8];
            corners[0] = new Vec3d(ntl.X, ntl.Y, ntl.Z);
            corners[1] = new Vec3d(ntr.X, ntr.Y, ntr.Z);
            corners[2] = new Vec3d(nbl.X, nbl.Y, nbl.Z);
            corners[3] = new Vec3d(nbr.X, nbr.Y, nbr.Z);
            corners[4] = new Vec3d(ftl.X, ftl.Y, ftl.Z);
            corners[5] = new Vec3d(ftr.X, ftr.Y, ftr.Z);
            corners[6] = new Vec3d(fbl.X, fbl.Y, fbl.Z);
            corners[7] = new Vec3d(fbr.X, fbr.Y, fbr.Z);

            triangles.Add(new triangle(new Vec3d[] { corners[0], corners[1], corners[2] }));
            triangles.Add(new triangle(new Vec3d[] { corners[1], corners[3], corners[2] }));
            triangles.Add(new triangle(new Vec3d[] { corners[4], corners[5], corners[6] }));
            triangles.Add(new triangle(new Vec3d[] { corners[5], corners[7], corners[6] }));
            triangles.Add(new triangle(new Vec3d[] { corners[0], corners[4], corners[2] }));
            triangles.Add(new triangle(new Vec3d[] { corners[4], corners[6], corners[2] }));
            triangles.Add(new triangle(new Vec3d[] { corners[1], corners[5], corners[3] }));
            triangles.Add(new triangle(new Vec3d[] { corners[5], corners[7], corners[3] }));
            triangles.Add(new triangle(new Vec3d[] { corners[0], corners[1], corners[4] }));
            triangles.Add(new triangle(new Vec3d[] { corners[1], corners[5], corners[4] }));
            triangles.Add(new triangle(new Vec3d[] { corners[2], corners[3], corners[6] }));
            triangles.Add(new triangle(new Vec3d[] { corners[3], corners[7], corners[6] }));

            return triangles;
        }

        public Plane[] planes;

        public Vector3 nearCenter;
        public Vector3 farCenter;

        public Vector3 ntl;
        public Vector3 ntr;
        public Vector3 nbl;
        public Vector3 nbr;
        public Vector3 ftl;
        public Vector3 ftr;
        public Vector3 fbl;
        public Vector3 fbr;
    }
}
