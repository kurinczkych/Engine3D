using OpenTK.Mathematics;
using OpenTK.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public class triangle
    {
        public Vector3[] p;
        public Vector3[] n;
        public Color4[] c;
        public Vec2d[] t;
        public bool gotPointNormals;

        public triangle()
        {
            gotPointNormals = false;
            p = new Vector3[3];
            n = new Vector3[3];
            c = new Color4[3];
            t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
        }

        public triangle(Vector3[] p)
        {
            gotPointNormals = false;
            this.p = new Vector3[p.Length];
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.p[p_] = p[p_];
            }
            n = new Vector3[] { new Vector3(), new Vector3(), new Vector3() };
            c = new Color4[p.Length];
            for (int c_ = 0; c_ < p.Length; c_++)
            {
                c[c_] = Color4.White;
            }

            t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
        }

        public triangle(Vector3[] p, Vec2d[] t)
        {
            gotPointNormals = false;
            this.p = new Vector3[p.Length];
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.p[p_] = p[p_];
            }
            n = new Vector3[] { new Vector3(), new Vector3(), new Vector3() };
            c = new Color4[p.Length];
            for (int c_ = 0; c_ < p.Length; c_++)
            {
                c[c_] = Color4.White;
            }
            this.t = new Vec2d[t.Length];
            for (int t_ = 0; t_ < t.Length; t_++)
            {
                this.t[t_] = t[t_].GetCopy();
            }
        }

        public triangle(Vector3[] p, Vector3[] n, Vec2d[] t)
        {
            gotPointNormals = true;
            this.p = new Vector3[p.Length];
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.p[p_] = p[p_];
            }
            this.n = new Vector3[] { new Vector3(), new Vector3(), new Vector3() };
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.n[p_] = n[p_];
            }
            c = new Color4[p.Length];
            for (int c_ = 0; c_ < p.Length; c_++)
            {
                c[c_] = Color4.White;
            }
            this.t = new Vec2d[t.Length];
            for (int t_ = 0; t_ < t.Length; t_++)
            {
                this.t[t_] = t[t_].GetCopy();
            }
        }

        public Vector3 GetMiddle()
        {
            return (p[0] + p[1] + p[2]) / 3;
        }

        public void TransformPosition(Vector3 transform)
        {
            p[0] += transform;
            p[1] += transform;
            p[2] += transform;
        }

        public bool RayIntersects(Vector3 rayOrigin, Vector3 rayDir, out Vector3 intersection)
        {
            intersection = new Vector3();

            Vector3 e1 = p[1] - p[0];
            Vector3 e2 = p[2] - p[0];
            Vector3 h = Vector3.Cross(rayDir, e2);
            float a = Vector3.Dot(e1, h);

            if (a > -float.Epsilon && a < float.Epsilon)
                return false;

            float f = 1.0f / a;
            Vector3 s = rayOrigin - p[0];
            float u = f * Vector3.Dot(s, h);

            if (u < 0.0f || u > 1.0f)
                return false;

            Vector3 q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(rayDir, q);

            if (v < 0.0f || u + v > 1.0f)
                return false;

            float t = f * Vector3.Dot(e2, q);

            if (t > float.Epsilon)
            {
                intersection = rayOrigin + rayDir * t;
                return true;
            }

            return false;
        }

        public Vector3 GetPointAboveXZ(Vector3 point)
        {
            Vector3 rayDir = new Vector3(0, -1, 0);

            if (RayIntersects(point, rayDir, out Vector3 intersection))
            {
                return intersection;
            }
            return Vector3.NegativeInfinity;
        }

        public void Color(Color4 c)
        {
            for (int i = 0; i < this.c.Length; i++)
            {
                this.c[i] = c;
            }
        }

        public void Color(triangle t)
        {
            for (int i = 0; i < this.c.Length; i++)
            {
                this.c[i] = t.c[i];
            }
        }

        public string GetPointsStr()
        {
            return p[0].ToString() + p[1].ToString() + p[2].ToString();
        }

        public Vector3 ComputeTriangleNormal()
        {
            Vector3 normal, line1, line2;
            line1 = p[1] - p[0];
            line2 = p[2] - p[0];

            normal = Vector3.Cross(line1, line2);
            normal.Normalize();
            return normal;
        }

        public float GetAngleToNormal(Vector3 dir)
        {
            Vector3 n = ComputeTriangleNormal();
            float dotProduct = Vector3.Dot(n, dir);
            float magnitudeA = n.Length;
            float magnitudeB = dir.Length;

            float angle = MathHelper.RadiansToDegrees((float)Math.Acos(dotProduct / (magnitudeA * magnitudeB)));

            if (angle <= 180)
            {
                return 180 - angle;
            }
            else
            {
                return angle - 180;
            }
        }

        public triangle GetCopy()
        {
            triangle tri = new triangle();
            tri.p[0] = p[0];
            tri.p[1] = p[1];
            tri.p[2] = p[2];
            tri.n[0] = n[0];
            tri.n[1] = n[1];
            tri.n[2] = n[2];
            tri.t[0] = t[0].GetCopy();
            tri.t[1] = t[1].GetCopy();
            tri.t[2] = t[2].GetCopy();
            tri.c[0] = c[0];
            tri.c[1] = c[1];
            tri.c[2] = c[2];

            return tri;
        }

        public int CompareTo(triangle tri)
        {
            double z1 = (p[0].Z + p[1].Z + p[2].Z) / 3.0f;
            double z2 = (tri.p[0].Z + tri.p[1].Z + tri.p[2].Z) / 3.0f;

            if (z1 < z2)
                return 1;
            else if (z1 > z2)
                return -1;
            else
                return 0;
        }

        public bool IsPointInTriangle(Vector3 p, out float distance)
        {
            Vector3 a = this.p[0];
            Vector3 b = this.p[1];
            Vector3 c = this.p[2];

            Vector3 normal = Vector3.Cross(b - a, c - a).Normalized();
            distance = Vector3.Dot(normal, p - a);

            Vector3 u = b - a;
            Vector3 v = c - a;
            Vector3 w = p - a;

            float uu = Vector3.Dot(u, u);
            float uv = Vector3.Dot(u, v);
            float vv = Vector3.Dot(v, v);
            float wu = Vector3.Dot(w, u);
            float wv = Vector3.Dot(w, v);
            float denom = 1.0f / (uv * uv - uu * vv);

            float s = (uv * wv - vv * wu) * denom;
            float t = (uv * wu - uu * wv) * denom;

            return s >= 0 && t >= 0 && (s + t) <= 1;
        }

        public bool IsLineInTriangle(Line line)
        {
            Vector3 normal = Vector3.Cross(p[1] - p[0], p[2] - p[0]);

            // Find intersection of line and triangle's plane
            float dotNumerator = Vector3.Dot(p[0] - line.Start, normal);
            float dotDenominator = Vector3.Dot(line.End - line.Start, normal);

            // Check if line and plane are parallel
            if (Math.Abs(dotDenominator) < 0.000001f)
            {
                return false;  // They are parallel so they don't intersect!
            }

            float t = dotNumerator / dotDenominator;
            Vector3 intersectionPoint = line.Start + t * (line.End - line.Start);

            // Check if the intersection point lies within the triangle
            return PointInTriangle(intersectionPoint, p[0], p[1], p[2]);
        }

        private bool PointInTriangle(Vector3 pt, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Vector3 d1 = pt - v1;
            Vector3 d2 = pt - v2;
            Vector3 d3 = pt - v3;

            float area = Vector3.Dot(Vector3.Cross(v1 - v2, v1 - v3).Normalized(), Vector3.Cross(v2 - v3, v2 - v1).Normalized());
            float area1 = Vector3.Dot(Vector3.Cross(d1, d2).Normalized(), Vector3.Cross(d1, d3).Normalized());

            return (Math.Abs(area - area1) < 0.000001f);
        }

        public bool IsRectInTriangle(List<Vector3> rectangleVertices)
        {
            List<Vector3> potentialSeparatingAxes = GetPotentialSeparatingAxes(rectangleVertices);

            foreach (Vector3 axis in potentialSeparatingAxes)
            {
                if (!IsOverlapping(Projection(rectangleVertices, axis), Projection(p.ToList(), axis)))
                    return false; // Separating axis found
            }

            return true;  // All projections overlap
        }

        private List<Vector3> GetPotentialSeparatingAxes(List<Vector3> rectangleVertices)
        {
            List<Vector3> axes = new List<Vector3>();

            // Triangle normal
            Vector3 triangleNormal = Vector3.Cross(p[1] - p[0], p[2] - p[0]);
            axes.Add(triangleNormal.Normalized());

            // Rectangle normals (parallelogram)
            Vector3 edge1 = rectangleVertices[1] - rectangleVertices[0];
            Vector3 edge2 = rectangleVertices[2] - rectangleVertices[0];
            axes.Add(Vector3.Cross(edge1, edge2).Normalized());

            // Cross products of edges
            Vector3 triangleEdge1 = p[1] - p[0];
            Vector3 triangleEdge2 = p[2] - p[1];
            Vector3 triangleEdge3 = p[0] - p[2];
            Vector3[] triangleEdges = { triangleEdge1, triangleEdge2, triangleEdge3 };

            for (int i = 0; i < triangleEdges.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Vector3 rectangleEdge = rectangleVertices[(j + 1) % 4] - rectangleVertices[j];
                    Vector3 axis = Vector3.Cross(triangleEdges[i], rectangleEdge).Normalized();

                    if (axis.LengthSquared > 0.0001f)  // Avoid nearly-zero vectors
                        axes.Add(axis);
                }
            }

            return axes;
        }

        private (float, float) Projection(List<Vector3> vertices, Vector3 axis)
        {
            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (Vector3 vertex in vertices)
            {
                float dot = Vector3.Dot(vertex, axis);
                if (dot < min) min = dot;
                if (dot > max) max = dot;
            }

            return (min, max);
        }

        private bool IsOverlapping((float, float) a, (float, float) b)
        {
            return a.Item1 <= b.Item2 && b.Item1 <= a.Item2;
        }
    }
}
