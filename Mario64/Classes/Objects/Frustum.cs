using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{

    public class Plane
    {
        public Plane() { }

        // unit vector
        public Vector3 normal = new Vector3(0.0f, 0.0f, 0.0f);

        // distance from origin to the nearest point in the plane
        public float distance = 0.0f;
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

        public bool IsTriangleInside(triangle tri)
        {
            var a = IsInside(tri.p[0]);
            var b = IsInside(tri.p[1]);
            var c = IsInside(tri.p[2]);
            return a || b || c;
        }

        public bool IsLineInside(Line line)
        {
            var a = IsInside(line.Start);
            var b = IsInside(line.End);
            return a || b;
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
