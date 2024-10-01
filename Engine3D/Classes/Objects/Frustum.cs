using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Frustum
    {

        public Plane[] planes;

        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 nearCenter;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 farCenter;

        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 ntl;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 ntr;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 nbl;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 nbr;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 ftl;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 ftr;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 fbl;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 fbr;

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
    }
}
