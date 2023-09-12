using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.GL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Drawing;
using System.Drawing.Imaging;

namespace Mario64
{
    public class Engine : GameWindow
    {
        class Vec2d
        {
            public Vec2d() { }

            public Vec2d(double u, double v)
            {
                this.u = u;
                this.v = v;
                this.w = 1.0f;
            }

            public double u = 0.0f;
            public double v = 0.0f;
            public double w;


            public Vec2d GetCopy()
            {
                Vec2d v2 = new Vec2d(u, v);
                v2.w = w;
                return v2;
            }
        }

        class Vec3d
        {
            public Vec3d()
            {
                W = 1.0f;
                color = Color4.White;
            }

            public Vec3d(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
                W = 1.0f;
                color = Color4.White;
            }

            public double X;
            public double Y;
            public double Z;
            public double W;

            public Vec3d GetCopy()
            {
                Vec3d v = new Vec3d(X, Y, Z);
                v.W = W;
                v.color = color;
                return v;
            }

            public static double Dot(Vec3d v1, Vec3d v2)
            {
                return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
            }

            public static Vec3d Cross(Vec3d v1, Vec3d v2)
            {
                Vec3d v = new Vec3d();
                v.X = v1.Y * v2.Z - v1.Z * v2.Y;
                v.Y = v1.Z * v2.X - v1.X * v2.Z;
                v.Z = v1.X * v2.Y - v1.Y * v2.X;
                v.W = v1.W;
                v.color = v1.color;
                return v;
            }

            public static Vec3d Normalize(Vec3d v)
            {
                double l = v.Length;
                Vec3d v2 = new Vec3d(v.X / l, v.Y / l, v.Z / l);
                v2.W = v.W;
                v2.color = v.color;
                return v;
            }

            public void Normalize()
            {
                double l = Length;
                X /= l;
                Y /= l;
                Z /= l;
            }

            #region Operator overloads
            public static Vec3d operator -(Vec3d v1, Vec3d v2)
            {
                Vec3d v3 = new Vec3d(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
                v3.W = v1.W;
                v3.color = v1.color;
                return v3;
            }
            public static Vec3d operator +(Vec3d v1, Vec3d v2)
            {
                Vec3d v3 = new Vec3d(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
                v3.W = v1.W;
                v3.color = v1.color;
                return v3;
            }
            public static Vec3d operator /(Vec3d v1, double d)
            {
                if (d == 0)
                    return v1;
                Vec3d v3 = new Vec3d(v1.X / d, v1.Y / d, v1.Z / d);
                v3.W = v1.W;
                v3.color = v1.color;
                return v3;
            }
            public static Vec3d operator *(Vec3d v1, double d)
            {
                Vec3d v3 = new Vec3d(v1.X * d, v1.Y * d, v1.Z * d);
                v3.W = v1.W;
                v3.color = v1.color;
                return v3;
            }
            #endregion

            public Color4 color;
            public double Length
            {
                get
                {
                    return Math.Sqrt(Vec3d.Dot(this, this));
                }
            }
        }

        private class triangle
        {
            public triangle()
            {
                p = new Vec3d[3];
                t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
            }

            public triangle(Vec3d[] p)
            {
                this.p = new Vec3d[p.Length];
                for (int p_ = 0; p_ < p.Length; p_++)
                {
                    this.p[p_] = p[p_].GetCopy();
                }

                t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
            }

            public triangle(Vec3d[] p, Vec2d[] t)
            {
                this.p = new Vec3d[p.Length];
                this.t = new Vec2d[t.Length];
                for (int p_ = 0; p_ < p.Length; p_++)
                {
                    this.p[p_] = p[p_].GetCopy();
                }
                for (int t_ = 0; t_ < t.Length; t_++)
                {
                    this.t[t_] = t[t_].GetCopy();
                }
            }

            public void Color(Color4 c)
            {
                foreach (Vec3d p_ in p)
                    p_.color = c;
            }

            public void Color(triangle t)
            {
                for (int p_ = 0; p_ < p.Length; p_++)
                {
                    p[p_].color = t.p[p_].color;
                }
            }

            public string GetPointsStr()
            {
                return p[0].ToString() + p[1].ToString() + p[2].ToString();
            }

            public triangle GetCopy()
            {
                triangle tri = new triangle();
                tri.p[0] = p[0].GetCopy();
                tri.p[1] = p[1].GetCopy();
                tri.p[2] = p[2].GetCopy();
                tri.t[0] = t[0].GetCopy();
                tri.t[1] = t[1].GetCopy();
                tri.t[2] = t[2].GetCopy();

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

            public Vec3d[] p;
            public Vec2d[] t;
        }

        private struct mesh
        {
            public mesh()
            {
                tris = new List<triangle>();
            }

            public void OnlyCube()
            {
                tris = new List<triangle>
                {
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(1.0f, 1.0f, 0.0f), new Vec3d(1.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 0.0f), new Vec3d(1.0f, 1.0f, 0.0f), new Vec3d(1.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 0.0f), new Vec3d(1.0f, 1.0f, 1.0f), new Vec3d(1.0f, 0.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(1.0f, 1.0f, 1.0f), new Vec3d(0.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(0.0f, 1.0f, 1.0f), new Vec3d(0.0f, 0.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 1.0f), new Vec3d(0.0f, 1.0f, 1.0f), new Vec3d(0.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 1.0f), new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(0.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(0.0f, 1.0f, 1.0f), new Vec3d(1.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(1.0f, 1.0f, 1.0f), new Vec3d(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(0.0f, 0.0f, 1.0f), new Vec3d(0.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(1.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) })
                };
            }

            public void ProcessObj(string filename)
            {
                tris = new List<triangle>();

                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(filename));

                string result;
                List<Vec3d> verts = new List<Vec3d>();

#pragma warning disable CS8600
#pragma warning disable CS8604
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (true)
                    {
                        result = reader.ReadLine();
                        if (result != null && result.Length > 0)
                        {
                            if (result[0] == 'v')
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                var a = double.Parse(vStr[0]);
                                var b = double.Parse(vStr[1]);
                                var c = double.Parse(vStr[2]);
                                Vec3d v = new Vec3d(a, b, c);
                                verts.Add(v);
                            }
                            else if (result[0] == 'f')
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                int[] f = { int.Parse(vStr[0]), int.Parse(vStr[1]), int.Parse(vStr[2]) };

                                tris.Add(new triangle(new Vec3d[] { verts[f[0] - 1], verts[f[1] - 1], verts[f[2] - 1] }));
                            }
                        }

                        if (result == null)
                            break;
                    }
                }
#pragma warning restore CS8600
#pragma warning restore CS8604
            }

            public List<triangle> tris;
        }

        private int vertexSize;
        struct Vertex
        {
            public Vector4 Position;
            public Color4 Color;
            public Vector3 Texture;
        }


        #region Matrix stuff
        Matrix4d Matrix_MakeIdentity()
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = 1.0f;
            matrix.Row1[1] = 1.0f;
            matrix.Row2[2] = 1.0f;
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4d Matrix_MakeRotationX(double fAngleRad)
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = 1.0f;
            matrix.Row1[1] = Math.Cos(fAngleRad);
            matrix.Row1[2] = Math.Sin(fAngleRad);
            matrix.Row2[1] = -Math.Sin(fAngleRad);
            matrix.Row2[2] = Math.Cos(fAngleRad);
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4d Matrix_MakeRotationY(double fAngleRad)
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = Math.Cos(fAngleRad);
            matrix.Row0[2] = Math.Sin(fAngleRad);
            matrix.Row2[0] = -Math.Sin(fAngleRad);
            matrix.Row1[1] = 1.0f;
            matrix.Row2[2] = Math.Cos(fAngleRad);
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4d Matrix_MakeRotationZ(double fAngleRad)
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = Math.Cos(fAngleRad);
            matrix.Row0[1] = Math.Sin(fAngleRad);
            matrix.Row1[0] = -Math.Sin(fAngleRad);
            matrix.Row1[1] = Math.Cos(fAngleRad);
            matrix.Row2[2] = 1.0f;
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4d Matrix_MakeTranslation(double x, double y, double z)
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = 1.0f;
            matrix.Row1[1] = 1.0f;
            matrix.Row2[2] = 1.0f;
            matrix.Row3[3] = 1.0f;
            matrix.Row3[0] = x;
            matrix.Row3[1] = y;
            matrix.Row3[2] = z;
            return matrix;
        }

        Matrix4d Matrix_MakeProjection(double fFovDegrees, double fAspectRatio, double fNear, double fFar)
        {
            double fFovRad = 1.0f / Math.Tan(fFovDegrees * 0.5f / 180.0f * 3.14159f);
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = fAspectRatio * fFovRad;
            matrix.Row1[1] = fFovRad;
            matrix.Row2[2] = fFar / (fFar - fNear);
            matrix.Row3[2] = (-fFar * fNear) / (fFar - fNear);
            matrix.Row2[3] = 1.0f;
            matrix.Row3[3] = 0.0f;
            return matrix;
        }

        Matrix4d Matrix_PointAt(Vec3d pos, Vec3d target, Vec3d up)
        {
            // Calculate new forward direction
            Vec3d newForward = target - pos;
            newForward = Vec3d.Normalize(newForward);

            // Calculate new Up direction
            Vec3d a = newForward * Vec3d.Dot(up, newForward);
            Vec3d newUp = up - a;
            newUp = Vec3d.Normalize(newUp);

            // New Right direction is easy, its just cross product
            Vec3d newRight = Vec3d.Cross(newUp, newForward);

            // Construct Dimensioning and Translation Matrix	
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = newRight.X; matrix.Row0[1] = newRight.Y; matrix.Row0[2] = newRight.Z; matrix.Row0[3] = 0.0f;
            matrix.Row1[0] = newUp.X; matrix.Row1[1] = newUp.Y; matrix.Row1[2] = newUp.Z; matrix.Row1[3] = 0.0f;
            matrix.Row2[0] = newForward.X; matrix.Row2[1] = newForward.Y; matrix.Row2[2] = newForward.Z; matrix.Row2[3] = 0.0f;
            matrix.Row3[0] = pos.X; matrix.Row3[1] = pos.Y; matrix.Row3[2] = pos.Z; matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Matrix4d Matrix_QuickInverse(Matrix4d m)
        {
            Matrix4d matrix = new Matrix4d();
            matrix.Row0[0] = m.Row0[0]; matrix.Row0[1] = m.Row1[0]; matrix.Row0[2] = m.Row2[0]; matrix.Row0[3] = 0.0f;
            matrix.Row1[0] = m.Row0[1]; matrix.Row1[1] = m.Row1[1]; matrix.Row1[2] = m.Row2[1]; matrix.Row1[3] = 0.0f;
            matrix.Row2[0] = m.Row0[2]; matrix.Row2[1] = m.Row1[2]; matrix.Row2[2] = m.Row2[2]; matrix.Row2[3] = 0.0f;
            matrix.Row3[0] = -(m.Row3[0] * matrix.Row0[0] + m.Row3[1] * matrix.Row1[0] + m.Row3[2] * matrix.Row2[0]);
            matrix.Row3[1] = -(m.Row3[0] * matrix.Row0[1] + m.Row3[1] * matrix.Row1[1] + m.Row3[2] * matrix.Row2[1]);
            matrix.Row3[2] = -(m.Row3[0] * matrix.Row0[2] + m.Row3[1] * matrix.Row1[2] + m.Row3[2] * matrix.Row2[2]);
            matrix.Row3[3] = 1.0f;
            return matrix;
        }

        Vec3d Matrix_MultiplyVector(Matrix4d m, Vec3d i)
        {
            Vec3d v = new Vec3d();
            v.X = i.X * m.Row0[0] + i.Y * m.Row1[0] + i.Z * m.Row2[0] + i.W * m.Row3[0];
            v.Y = i.X * m.Row0[1] + i.Y * m.Row1[1] + i.Z * m.Row2[1] + i.W * m.Row3[1];
            v.Z = i.X * m.Row0[2] + i.Y * m.Row1[2] + i.Z * m.Row2[2] + i.W * m.Row3[2];
            v.W = i.X * m.Row0[3] + i.Y * m.Row1[3] + i.Z * m.Row2[3] + i.W * m.Row3[3];
            v.color = i.color;
            return v;
        }
        #endregion

        #region Clipping stuff
        Vec3d Vector_IntersectPlane(Vec3d plane_p, Vec3d plane_n, Vec3d lineStart, Vec3d lineEnd, ref double t)
        {
            plane_n = Vec3d.Normalize(plane_n);
            double plane_d = -Vec3d.Dot(plane_n, plane_p);
            double ad = Vec3d.Dot(lineStart, plane_n);
            double bd = Vec3d.Dot(lineEnd, plane_n);
            t = (-plane_d - ad) / (bd - ad);
            Vec3d lineStartToEnd = lineEnd - lineStart;
            Vec3d lineToIntersect = lineStartToEnd * t;
            return lineStart + lineToIntersect;
        }

        int Triangle_ClipAgainstPlane(Vec3d plane_p, Vec3d plane_n, triangle in_tri, ref triangle out_tri1, ref triangle out_tri2)
        {
            // Make sure plane normal is indeed normal
            plane_n = Vec3d.Normalize(plane_n);

            Func<Vec3d, double> dist = (Vec3d p) =>
            {
                return (plane_n.X * p.X + plane_n.Y * p.Y + plane_n.Z * p.Z - Vec3d.Dot(plane_n, plane_p));
            };

            // Create two temporary storage arrays to classify points either side of plane
            // If distance sign is positive, point lies on "inside" of plane
            Vec3d[] inside_points = new Vec3d[3]; int nInsidePointCount = 0;
            Vec3d[] outside_points = new Vec3d[3]; int nOutsidePointCount = 0;
            Vec2d[] inside_tex = new Vec2d[3]; int nInsideTexCount = 0;
            Vec2d[] outside_tex = new Vec2d[3]; int nOutsideTexCount = 0;

            // Get signed distance of each point in triangle to plane
            double d0 = dist(in_tri.p[0]);
            double d1 = dist(in_tri.p[1]);
            double d2 = dist(in_tri.p[2]);

            if (d0 >= 0)
            {
                inside_points[nInsidePointCount++] = in_tri.p[0].GetCopy();
                inside_tex[nInsideTexCount++] = in_tri.t[0].GetCopy();
            }
            else
            {
                outside_points[nOutsidePointCount++] = in_tri.p[0].GetCopy();
                outside_tex[nOutsideTexCount++] = in_tri.t[0].GetCopy();
            }
            if (d1 >= 0)
            {
                inside_points[nInsidePointCount++] = in_tri.p[1].GetCopy();
                inside_tex[nInsideTexCount++] = in_tri.t[1].GetCopy();
            }
            else
            {
                outside_points[nOutsidePointCount++] = in_tri.p[1].GetCopy();
                outside_tex[nOutsideTexCount++] = in_tri.t[1].GetCopy();
            }
            if (d2 >= 0)
            {
                inside_points[nInsidePointCount++] = in_tri.p[2].GetCopy();
                inside_tex[nInsideTexCount++] = in_tri.t[2].GetCopy();
            }
            else
            {
                outside_points[nOutsidePointCount++] = in_tri.p[2].GetCopy();
                outside_tex[nOutsideTexCount++] = in_tri.t[2].GetCopy();
            }

            // Now classify triangle points, and break the input triangle into 
            // smaller output triangles if required. There are four possible
            // outcomes...

            if (nInsidePointCount == 0)
            {
                // All points lie on the outside of plane, so clip whole triangle
                // It ceases to exist

                return 0; // No returned triangles are valid
            }

            if (nInsidePointCount == 3)
            {
                // All points lie on the inside of plane, so do nothing
                // and allow the triangle to simply pass through
                out_tri1 = in_tri.GetCopy();

                return 1; // Just the one returned original triangle is valid
            }

            if (nInsidePointCount == 1 && nOutsidePointCount == 2)
            {
                // Triangle should be clipped. As two points lie outside
                // the plane, the triangle simply becomes a smaller triangle


                // The inside point is valid, so keep that...
                out_tri1.p[0] = inside_points[0].GetCopy();
                out_tri1.t[0] = inside_tex[0].GetCopy();

                // but the two new points are at the locations where the 
                // original sides of the triangle (lines) intersect with the plane
                double t = 0;
                out_tri1.p[1] = Vector_IntersectPlane(plane_p, plane_n, inside_points[0], outside_points[0], ref t);
                out_tri1.t[1].u = t * (outside_tex[0].u - inside_tex[0].u) + inside_tex[0].u;
                out_tri1.t[1].v = t * (outside_tex[0].v - inside_tex[0].v) + inside_tex[0].v;
                out_tri1.t[1].w = t * (outside_tex[0].w - inside_tex[0].w) + inside_tex[0].w;
                out_tri1.p[2] = Vector_IntersectPlane(plane_p, plane_n, inside_points[0], outside_points[1], ref t);
                out_tri1.t[2].u = t * (outside_tex[1].u - inside_tex[0].u) + inside_tex[0].u;
                out_tri1.t[2].v = t * (outside_tex[1].v - inside_tex[0].v) + inside_tex[0].v;
                out_tri1.t[2].w = t * (outside_tex[1].w - inside_tex[0].w) + inside_tex[0].w;

                // Copy appearance info to new triangle
                out_tri1.Color(in_tri);

                return 1; // Return the newly formed single triangle
            }

            if (nInsidePointCount == 2 && nOutsidePointCount == 1)
            {
                // Triangle should be clipped. As two points lie inside the plane,
                // the clipped triangle becomes a "quad". Fortunately, we can
                // represent a quad with two new triangles


                // The first triangle consists of the two inside points and a new
                // point determined by the location where one side of the triangle
                // intersects with the plane
                double t = 0;

                out_tri1.p[0] = inside_points[0].GetCopy();
                out_tri1.p[1] = inside_points[1].GetCopy();
                out_tri1.t[0] = inside_tex[0].GetCopy();
                out_tri1.t[1] = inside_tex[1].GetCopy();

                out_tri1.p[2] = Vector_IntersectPlane(plane_p, plane_n, inside_points[0], outside_points[0], ref t);
                out_tri1.t[2].u = t * (outside_tex[0].u - inside_tex[0].u) + inside_tex[0].u;
                out_tri1.t[2].v = t * (outside_tex[0].v - inside_tex[0].v) + inside_tex[0].v;
                out_tri1.t[2].w = t * (outside_tex[0].w - inside_tex[0].w) + inside_tex[0].w;

                // The second triangle is composed of one of he inside points, a
                // new point determined by the intersection of the other side of the 
                // triangle and the plane, and the newly created point above
                out_tri2.p[0] = inside_points[1].GetCopy();
                out_tri2.p[1] = out_tri1.p[2].GetCopy();
                out_tri2.t[0] = inside_tex[1].GetCopy();
                out_tri2.t[1] = out_tri1.t[2].GetCopy();
                out_tri2.p[2] = Vector_IntersectPlane(plane_p, plane_n, inside_points[1], outside_points[0], ref t);
                out_tri2.t[2].u = t * (outside_tex[0].u - inside_tex[1].u) + inside_tex[1].u;
                out_tri2.t[2].v = t * (outside_tex[0].v - inside_tex[1].v) + inside_tex[1].v;
                out_tri2.t[2].w = t * (outside_tex[0].w - inside_tex[1].w) + inside_tex[1].w;

                // Copy appearance info to new triangles
                //out_tri1.Color(new Color4(0.0f, 1.0f, 0.0f, 1.0f));
                //out_tri2.Color(new Color4(0.0f, 0.0f, 1.0f, 1.0f));
                out_tri1.Color(in_tri);
                out_tri2.Color(in_tri);

                return 2; // Return two newly formed triangles which form a quad
            }

            return 0;
        }
        #endregion

        #region Wireframe drawing
        private void DrawPixel(double x, double y, Color4 color, bool scissorTest = true)
        {
            if (scissorTest)
                GL.Enable(EnableCap.ScissorTest);

            GL.Scissor((int)x, (int)y, 1, 1);
            GL.ClearColor(color);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            if (scissorTest)
                GL.Disable(EnableCap.ScissorTest);
        }
        private void DrawLine(double x1, double y1, double x2, double y2, Color4 color)
        {
            double x, y, dx, dy, dx1, dy1, px, py, xe, ye, i;
            dx = x2 - x1; dy = y2 - y1;
            dx1 = Math.Abs(dx); dy1 = Math.Abs(dy);
            px = 2 * dy1 - dx1; py = 2 * dx1 - dy1;
            if (dy1 <= dx1)
            {
                if (dx >= 0)
                { x = x1; y = y1; xe = x2; }
                else
                { x = x2; y = y2; xe = x1; }

                //DrawPixel(x, y, c, col);
                DrawPixel(x, y, color);

                for (i = 0; x < xe; i++)
                {
                    x = x + 1;
                    if (px < 0)
                        px = px + 2 * dy1;
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0)) y = y + 1; else y = y - 1;
                        px = px + 2 * (dy1 - dx1);
                    }
                    DrawPixel(x, y, color);
                }
            }
            else
            {
                if (dy >= 0)
                { x = x1; y = y1; ye = y2; }
                else
                { x = x2; y = y2; ye = y1; }

                DrawPixel(x, y, color);

                for (i = 0; y < ye; i++)
                {
                    y = y + 1;
                    if (py <= 0)
                        py = py + 2 * dx1;
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0)) x = x + 1; else x = x - 1;
                        py = py + 2 * (dx1 - dy1);
                    }
                    DrawPixel(x, y, color);
                }
            }
        }
        private void DrawTriangle(triangle tri, Color4 color)
        {
            int x1 = (int)tri.p[0].X;
            int y1 = (int)tri.p[0].Y;
            int x2 = (int)tri.p[1].X;
            int y2 = (int)tri.p[1].Y;
            int x3 = (int)tri.p[2].X;
            int y3 = (int)tri.p[2].Y;

            DrawLine(x1, y1, x2, y2, color);
            DrawLine(x2, y2, x3, y3, color);
            DrawLine(x3, y3, x1, y1, color);
        }
        #endregion

        // OPENGL
        private int vao;
        private int shaderProgram;
        private int vbo;
        private List<Vertex> vertices;
        private List<triangle> trisToRaster = new List<triangle>();

        // Program variables
        private Random rnd = new Random((int)DateTime.Now.Ticks);
        private int screenWidth;
        private int screenHeight;
        private int frameCount;
        private double totalTime;

        // Engine variables
        private mesh meshCube;
        private Matrix4d matProj;
        private Matrix4d matRotZ, matRotX, matTrans, matWorld;
        private double theta = 0f;

        private Vec3d camera = new Vec3d();
        private Vec3d lookDir = new Vec3d();
        private double yaw;

        public Engine(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            screenWidth = width;
            screenHeight = height;
            this.CenterWindow(new Vector2i(screenWidth, screenHeight));
            vertices = new List<Vertex>();
        }

        private void DrawFps(double deltaTime)
        {
            frameCount += 1;
            totalTime += deltaTime;

            double fps = (double)frameCount / totalTime;
            Title = "Mario 64    |    FPS: " + Math.Round(fps, 4).ToString();

            if (frameCount > 1000)
            {
                frameCount = 0;
                totalTime = 0;
            }
        }

        private void CalculateTriangles()
        {
            matRotZ = Matrix_MakeRotationZ(theta * 0.5f);
            matRotX = Matrix_MakeRotationX(theta);

            matTrans = Matrix_MakeTranslation(0.0f, 0.0f, 8.0f);

            matWorld = new Matrix4d();
            matWorld = Matrix_MakeIdentity();
            matWorld = Matrix4d.Mult(matRotZ, matRotX);
            matWorld = Matrix4d.Mult(matWorld, matTrans);

            Vec3d up = new Vec3d(0.0f, 1.0f, 0.0f);
            Vec3d target = new Vec3d(0.0f, 0.0f, 1.0f);
            Matrix4d matCameraRot = Matrix_MakeRotationY(yaw);

            var cameraP = Matrix_MultiplyVector(matCameraRot, target);
            lookDir = cameraP.GetCopy();
            target = camera + lookDir;

            List<triangle> trisToClip = new List<triangle>();

            Matrix4d matCamera = Matrix_PointAt(camera, target, up);
            Matrix4d matView = Matrix_QuickInverse(matCamera);

            foreach (triangle tri in meshCube.tris)
            {
                triangle triTransormed = new triangle();
                // Offset to screen
                triTransormed.p[0] = Matrix_MultiplyVector(matWorld, tri.p[0]);
                triTransormed.p[1] = Matrix_MultiplyVector(matWorld, tri.p[1]);
                triTransormed.p[2] = Matrix_MultiplyVector(matWorld, tri.p[2]);
                triTransormed.t[0] = tri.t[0].GetCopy();
                triTransormed.t[1] = tri.t[1].GetCopy();
                triTransormed.t[2] = tri.t[2].GetCopy();

                // Normal calculation
                Vec3d normal, line1, line2;
                line1 = triTransormed.p[1] - triTransormed.p[0];
                line2 = triTransormed.p[2] - triTransormed.p[0];

                normal = Vec3d.Cross(line1, line2);
                normal.Normalize();

                Vec3d cameraRay = triTransormed.p[0] - camera;
                // If not visible, continue
                if (Vec3d.Dot(normal, cameraRay) >= 0)
                    continue;

                Vec3d lightDir = new Vec3d(0.0f, 0.0f, -1.0f);
                lightDir.Normalize();
                double dp = Math.Max(0.1f, Vec3d.Dot(normal, lightDir));
                triTransormed.Color(new Color4((float)dp, (float)dp, (float)dp, 1.0f));

                // Convert world space to view space
                triangle triViewed = new triangle();
                triViewed.p[0] = Matrix_MultiplyVector(matView, triTransormed.p[0]);
                triViewed.p[1] = Matrix_MultiplyVector(matView, triTransormed.p[1]);
                triViewed.p[2] = Matrix_MultiplyVector(matView, triTransormed.p[2]);
                triViewed.t[0] = triTransormed.t[0].GetCopy();
                triViewed.t[1] = triTransormed.t[1].GetCopy();
                triViewed.t[2] = triTransormed.t[2].GetCopy();
                triViewed.Color(triTransormed);

                int nClippedTriangles = 0;
                triangle[] clipped = new triangle[2] { new triangle(), new triangle() };
                nClippedTriangles = Triangle_ClipAgainstPlane(new Vec3d(0.0f, 0.0f, 0.1f), new Vec3d(0.0f, 0.0f, 1.0f), triViewed, ref clipped[0], ref clipped[1]);

                for (int n = 0; n < nClippedTriangles; n++)
                {
                    triangle triProjected = new triangle();

                    // Project triangles from 3D to 2D
                    triProjected.p[0] = Matrix_MultiplyVector(matProj, clipped[n].p[0]);
                    triProjected.p[1] = Matrix_MultiplyVector(matProj, clipped[n].p[1]);
                    triProjected.p[2] = Matrix_MultiplyVector(matProj, clipped[n].p[2]);
                    triProjected.t[0] = clipped[n].t[0].GetCopy();
                    triProjected.t[1] = clipped[n].t[1].GetCopy();
                    triProjected.t[2] = clipped[n].t[2].GetCopy();
                    triProjected.Color(clipped[n]);

                    //triProjected.t[0].u = triProjected.t[0].u / triProjected.p[0].Z;
                    //triProjected.t[1].u = triProjected.t[1].u / triProjected.p[1].Z;
                    //triProjected.t[2].u = triProjected.t[2].u / triProjected.p[2].Z;
                                                                              
                    //triProjected.t[0].v = triProjected.t[0].v / triProjected.p[0].Z;
                    //triProjected.t[1].v = triProjected.t[1].v / triProjected.p[1].Z;
                    //triProjected.t[2].v = triProjected.t[2].v / triProjected.p[2].Z;

                    //triProjected.t[0].w = 1.0f / triProjected.p[0].W;
                    //triProjected.t[1].w = 1.0f / triProjected.p[1].W;
                    //triProjected.t[2].w = 1.0f / triProjected.p[2].W;

                    triProjected.p[0] = triProjected.p[0] / triProjected.p[0].W;
                    triProjected.p[1] = triProjected.p[1] / triProjected.p[1].W;
                    triProjected.p[2] = triProjected.p[2] / triProjected.p[2].W;

                    // Scale into view
                    Vec3d offsetView = new Vec3d(1.0f, 1.0f, 0.0f);
                    triProjected.p[0] += offsetView;
                    triProjected.p[1] += offsetView;
                    triProjected.p[2] += offsetView;

                    triProjected.p[0].X *= 0.5f * screenWidth;
                    triProjected.p[0].Y *= 0.5f * screenHeight;
                    triProjected.p[1].X *= 0.5f * screenWidth;
                    triProjected.p[1].Y *= 0.5f * screenHeight;
                    triProjected.p[2].X *= 0.5f * screenWidth;
                    triProjected.p[2].Y *= 0.5f * screenHeight;

                    //triProjected.ClipPoints(screenWidth, screenHeight);

                    trisToClip.Add(triProjected);
                }
            }

            // Sort triangles back to front
            trisToClip.Sort((a, b) => a.CompareTo(b));

            trisToRaster = new List<triangle>();

            foreach (triangle triToClip in trisToClip)
            {
                // Clip triangles against all four screen edges, this could yield
                // a bunch of triangles, so create a queue that we traverse to 
                //  ensure we only test new triangles generated against planes
                triangle[] clippedNew = new triangle[2] { new triangle(), new triangle() };

                Queue<triangle> list = new Queue<triangle>();
                list.Enqueue(triToClip);

                int nNewTriangles = 1;

                for (int p_ = 0; p_ < 4; p_++)
                {
                    int nTrisToAdd = 0;
                    while (nNewTriangles > 0)
                    {
                        // Take triangle from front of queue
                        triangle test = list.Dequeue();
                        nNewTriangles--;

                        // Clip it against a plane. We only need to test each 
                        // subsequent plane, against subsequent new triangles
                        // as all triangles after a plane clip are guaranteed
                        // to lie on the inside of the plane. I like how this
                        // comment is almost completely and utterly justified

                        if (p_ == 0)
                            nTrisToAdd = Triangle_ClipAgainstPlane(new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(0.0f, 1.0f, 0.0f), test, ref clippedNew[0], ref clippedNew[1]);

                        if (p_ == 1)
                            nTrisToAdd = Triangle_ClipAgainstPlane(new Vec3d(0.0f, (float)screenHeight - 1, 0.0f), new Vec3d(0.0f, -1.0f, 0.0f), test, ref clippedNew[0], ref clippedNew[1]);

                        if (p_ == 2)
                            nTrisToAdd = Triangle_ClipAgainstPlane(new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(1.0f, 0.0f, 0.0f), test, ref clippedNew[0], ref clippedNew[1]);

                        if (p_ == 3)
                            nTrisToAdd = Triangle_ClipAgainstPlane(new Vec3d((float)screenWidth - 1, 0.0f, 0.0f), new Vec3d(-1.0f, 0.0f, 0.0f), test, ref clippedNew[0], ref clippedNew[1]);

                        // Clipping may yield a variable number of triangles, so
                        // add these new ones to the back of the queue for subsequent
                        // clipping against next planes

                        for (int w = 0; w < nTrisToAdd; w++)
                            list.Enqueue(clippedNew[w].GetCopy());
                    }
                    nNewTriangles = list.Count();
                }
                ;
                foreach (triangle listTriangle in list)
                    trisToRaster.Add(listTriangle);
            }
        }


        public double ConvertRange(double originalStart, double originalEnd, double newStart, double newEnd, double value)
        {
            double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (double)(newStart + ((value - originalStart) * scale));
        }

        Vertex ConvertToNDC(Vec3d screenPos, Color4 color, Vec2d tex)
        {
            //float x = (float)ConvertRange(0, screenWidth, -1, 1, screenPos[0]);
            //float y = (float)ConvertRange(0, screenHeight, -1, 1, screenPos[1]);

            float x = (2.0f * (float)screenPos.X / screenWidth) - 1.0f;
            float y = (2.0f * (float)screenPos.Y / screenHeight) - 1.0f;

            //return new Vertex() { Position = new Vector3(x, y, 1.0f), Color = color, Texture = new Vector3((float)tex.u, (float)tex.v, (float)tex.w) };
            return new Vertex() { Position = new Vector4(x, y, 1.0f, (float)screenPos.W), Color = color, Texture = new Vector3((float)tex.u, (float)tex.v, (float)tex.w) };
        }

        void ConvertListToNDC(List<triangle> tris)
        {
            vertices = new List<Vertex>();
            foreach (var tri in tris)
            {
                //Color4 c = new Color4((float)rnd.Next(0, 100) / 100f, (float)rnd.Next(0, 100) / 100f, (float)rnd.Next(0, 100) / 100f, 1.0f);
                Color4 c = tri.p[0].color;
                //vertices.Add(ConvertToNDC(tri.p[0], tri.color));
                //vertices.Add(ConvertToNDC(tri.p[1], tri.color));
                //vertices.Add(ConvertToNDC(tri.p[2], tri.color));
                vertices.Add(ConvertToNDC(tri.p[0], c, tri.t[0]));
                vertices.Add(ConvertToNDC(tri.p[1], c, tri.t[1]));
                vertices.Add(ConvertToNDC(tri.p[2], c, tri.t[2]));
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.ClearColor(Color4.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            DrawFps(args.Time);

            GL.UseProgram(shaderProgram); // bind vao
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, vertexSize, 4 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, 8 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            GL.DisableVertexAttribArray(0);

            //foreach (triangle tri in trisToRaster)
            //{
            //    DrawTriangle(tri, Color4.Red);
            //}

            Context.SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);


            if (IsKeyDown(Keys.Space))
            {
                camera.Y += 8.0f * args.Time;
            }
            if (IsKeyDown(Keys.LeftShift))
            {
                camera.Y -= 8.0f * args.Time;
            }

            Vec3d forward = lookDir * (4.0f * args.Time);
            Vec3d leftLookDir = new Vec3d(-lookDir.Z, 0.0f, lookDir.X);
            Vec3d left = leftLookDir * (8.0f * args.Time);  //8.0f <- speed
            if (IsKeyDown(Keys.W))
            {
                camera += forward;
            }
            if (IsKeyDown(Keys.S))
            {
                camera -= forward;
            }
            if (IsKeyDown(Keys.A))
            {
                camera = camera + left;
            }
            if (IsKeyDown(Keys.D))
            {
                camera = camera - left;
            }

            if (IsKeyDown(Keys.Left))
            {
                yaw += 2.0f * args.Time;
            }
            if (IsKeyDown(Keys.Right))
            {
                yaw -= 2.0f * args.Time;
            }

            CalculateTriangles();
            ConvertListToNDC(trisToRaster);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * vertexSize, vertices.ToArray(), BufferUsageHint.StaticDraw);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            vertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex));

            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            // OPENGL init
            vao = GL.GenVertexArray();

            // generate a buffer
            GL.GenBuffers(1, out vbo);

            // Texture -----------------------------------------------
            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            // Load the image (using System.Drawing or another library)
            Stream stream = GetResourceStreamByNameEnd("bmp_24.bmp");
            if (stream != null)
            {
                using (stream)
                {
                    Bitmap bitmap = new Bitmap(stream);
                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                    bitmap.UnlockBits(data);

                    // Texture settings
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                }
            }
            else
            {
                ;
            }
            // Texture --------------------------------------------

            // bind the vao
            GL.BindVertexArray(vao);
            // point slot (0) of the VAO to the currently bound VBO (vbo)
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.EnableVertexAttribArray(0);


            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, vertexSize, 4 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, 8 * sizeof(float));
            GL.EnableVertexAttribArray(2);


            // create the shader program
            shaderProgram = GL.CreateProgram();

            // create the vertex shader
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            // add the source code from "Default.vert" in the Shaders file
            GL.ShaderSource(vertexShader, LoadShaderSource("Default.vert"));
            // Compile the Shader
            GL.CompileShader(vertexShader);

            // Same as vertex shader
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, LoadShaderSource("Default.frag"));
            GL.CompileShader(fragmentShader);

            // Attach the shaders to the shader program
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);

            // Link the program to OpenGL
            GL.LinkProgram(shaderProgram);

            int textureLocation = GL.GetUniformLocation(shaderProgram, "textureSampler");
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.Uniform1(textureLocation, 0);  // 0 corresponds to TextureUnit.Texture0

            // delete the shaders
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // Projection matrix and mesh loading
            meshCube.OnlyCube();
            //meshCube.ProcessObj("cube.obj");

            //Projection matrix
            double near = 0.1f;
            double far = 1000.0f;
            double fov = 90.0f;
            double aspectRatio = (double)screenHeight / (double)screenWidth;
            double fovRad = 1.0f / Math.Tan(fov * 0.5f / 180.0f * Math.PI);

            matProj = Matrix_MakeProjection(fov, aspectRatio, near, far);
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo);
            GL.DeleteProgram(shaderProgram);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            screenWidth = e.Width;
            screenHeight = e.Height;
        }

        public string LoadShaderSource(string filePath)
        {
            string shaderSource = "";

            try
            {
                using (StreamReader reader = new StreamReader("../../../Shaders/" + filePath))
                {
                    shaderSource = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load shader source file: " + e.Message);
            }

            return shaderSource;
        }

        private Stream GetResourceStreamByNameEnd(string nameEnd)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (resourceName.EndsWith(nameEnd, StringComparison.OrdinalIgnoreCase))
                {
                    return assembly.GetManifestResourceStream(resourceName);
                }
            }
            return null; // or throw an exception if the resource is not found
        }
    }
}
