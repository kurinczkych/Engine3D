using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class QueryPool
    {
        private Stack<int> availableQueries = new Stack<int>();
        private int InitialPoolSize = 1000;  // Adjust based on your needs

        public QueryPool(int poolSize)
        {
            InitialPoolSize = poolSize;
            for (int i = 0; i < InitialPoolSize; i++)
            {
                int query;
                GL.GenQueries(1, out query);
                availableQueries.Push(query);
            }
        }

        public int GetQuery()
        {
            if (availableQueries.Count == 0)
            {
                // Either generate more queries or handle the "out of queries" situation.
                // Here we're just generating a new one.
                int query;
                GL.GenQueries(1, out query);
                return query;
            }

            return availableQueries.Pop();
        }

        public void ReturnQuery(int query)
        {
            availableQueries.Push(query);
        }

        public void DeleteQueries()
        {
            int[] queries = availableQueries.ToArray();
            if (queries.Length > 0)
            {
                GL.DeleteQueries(queries.Length, queries);
            }
            availableQueries.Clear();
        }
    }

    public static class OcclusionCulling
    {
        private static void RenderAABB(AABB bounds, VBO aabbVbo, VAO aabbVao, Shader shader, Camera camera)
        {

            List<float> vertices = bounds.GetTriangleVertices();

            aabbVao.Bind();

            aabbVbo.Buffer(vertices);

            // Draw the AABB using the indices
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count / 3);
        }

        private static void ManageVisibility(BVHNode node, bool value)
        {
            if (node.visibility.Count == BVHNode.VisCount)
            {
                node.visibility.Insert(0, value);
                node.visibility.RemoveAt(node.visibility.Count - 1);
            }
            else
            {
                node.visibility.Insert(0, value);
            }
        }

        public static void PerformOcclusionQueriesForBVH(BVH node, VBO aabbVbo, VAO aabbVao, Shader shader, Camera camera, 
                                                         ref QueryPool queryPool, ref Dictionary<int, BVHNode> pendingQueries, bool first)
        {
            Frustum frustum = camera.frustum;

            Matrix4 modelMatrix = Matrix4.Identity;
            Matrix4 projectionMatrix = camera.projectionMatrix;
            Matrix4 viewMatrix = camera.viewMatrix;

            GL.UniformMatrix4(node.uniformLocations["modelMatrix"], true, ref modelMatrix);
            GL.UniformMatrix4(node.uniformLocations["viewMatrix"], true, ref viewMatrix);
            GL.UniformMatrix4(node.uniformLocations["projectionMatrix"], true, ref projectionMatrix);

            if (!first)
            {
                foreach (KeyValuePair<int, BVHNode> keyValuePair in pendingQueries)
                {
                    int samplesPassed;
                    GL.GetQueryObject(keyValuePair.Key, GetQueryObjectParam.QueryResult, out samplesPassed);
                    keyValuePair.Value.samplesPassedPrevFrame = samplesPassed;
                    queryPool.ReturnQuery(keyValuePair.Key);
                }
                pendingQueries.Clear();
            }

            GL.DepthMask(false);
            PerformOcclusionQueriesForBVHRecursive(node.Root, ref aabbVbo, ref aabbVao, ref shader, ref camera, ref frustum, ref queryPool, ref pendingQueries, first);
            GL.DepthMask(true);

            aabbVbo.Unbind();
            aabbVao.Unbind();

        }


        public static void PerformOcclusionQueriesForBVHRecursive(BVHNode node, ref VBO aabbVbo, ref VAO aabbVao, ref Shader shader, ref Camera camera,
                                                                  ref Frustum frustum, ref QueryPool queryPool, ref Dictionary<int, BVHNode> pendingQueries,
                                                                  bool first)
        {

            if (first)
            {
                if (node == null) return;
                if (!frustum.IsAABBInside(node.bounds))
                {
                    ManageVisibility(node, false);

                    return;
                }

                int query = queryPool.GetQuery();

                // 1. Initiate occlusion query
                GL.BeginQuery(QueryTarget.SamplesPassed, query);

                // 2. Render the AABB of the current BVH node
                RenderAABB(node.bounds, aabbVbo, aabbVao, shader, camera);

                // 3. End occlusion query
                GL.EndQuery(QueryTarget.SamplesPassed);

                // 4. Buffer the pending results
                if (!pendingQueries.ContainsKey(query))
                {
                    pendingQueries.Add(query, node);
                }

                PerformOcclusionQueriesForBVHRecursive(node.left, ref aabbVbo, ref aabbVao, ref shader, ref camera, ref frustum, ref queryPool, ref pendingQueries, first);
                PerformOcclusionQueriesForBVHRecursive(node.right, ref aabbVbo, ref aabbVao, ref shader, ref camera, ref frustum, ref queryPool, ref pendingQueries, first);
            }
            else
            {
                if (node == null) return;
                if (!frustum.IsAABBInside(node.bounds))
                {
                    ManageVisibility(node, false);

                    return;
                }

                int query = queryPool.GetQuery();

                // 1. Initiate occlusion query
                GL.BeginQuery(QueryTarget.SamplesPassed, query);

                // 2. Render the AABB of the current BVH node
                RenderAABB(node.bounds, aabbVbo, aabbVao, shader, camera);

                // 3. End occlusion query
                GL.EndQuery(QueryTarget.SamplesPassed);

                // 4. Buffer the pending results
                if (!pendingQueries.ContainsKey(query))
                {
                    pendingQueries.Add(query, node);
                }

                if(node.samplesPassedPrevFrame > 0)
                {
                    ManageVisibility(node, true);

                    PerformOcclusionQueriesForBVHRecursive(node.left, ref aabbVbo, ref aabbVao, ref shader, ref camera, ref frustum, ref queryPool, ref pendingQueries, first);
                    PerformOcclusionQueriesForBVHRecursive(node.right, ref aabbVbo, ref aabbVao, ref shader, ref camera, ref frustum, ref queryPool, ref pendingQueries, first);
                }
                else
                {
                    ManageVisibility(node, false);

                    return;
                }
            }

            // 5. Cleanup the query object
            //queryPool.ReturnQuery(query);


            // 6. If there are samples passed, then mark the node as visible
            //if (samplesPassed > 0)
            //{
            //    node.isVisible = true;


            //    PerformOcclusionQueriesForBVHRecursive(node.left, ref aabbVbo, ref aabbVao, ref shader, ref camera, ref frustum, ref queryPool, ref pendingQueries);
            //    PerformOcclusionQueriesForBVHRecursive(node.right, ref aabbVbo, ref aabbVao, ref shader, ref camera, ref frustum, ref queryPool, ref pendingQueries);
            //}
            //else
            //{
            //    node.isVisible = false;
            //    return;
            //}
        }

        public static double GTRandom(int index)
        {
            byte[] hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                // Compute hash from the index
                hash = sha256.ComputeHash(BitConverter.GetBytes(index));
            }

            // Convert the hash bytes to an int seed (we'll just use the first 4 bytes)
            int seed = BitConverter.ToInt32(hash, 0);

            // Use the seed to get a random number
            Random random = new Random(seed);
            return random.NextDouble();
        }

        public static void TraverseBVHNode(BVHNode node, ref List<triangle> notOccludedTris, ref int i, ref Frustum frustum)
        {
            if (node == null)
                return;



            //If it's a leaf node (i.e., no children but has triangles)
            if (node.left == null && node.right == null && node.triangles != null)
            {

                //Color4 c = new Color4((float)GTRandom(i), (float)GTRandom(i+1), (float)GTRandom(i+2), 1.0f);
                //i += 3;
                //List<triangle> colorTris = new List<triangle>(node.triangles);
                //colorTris.ForEach(x => x.SetColor(c));
                //notOccludedTris.AddRange(colorTris);
                if (frustum.IsAABBInside(node.bounds))
                {
                    if (node.visibility.Any(x => x == true))
                    {
                        //Color4 c = new Color4((float)GTRandom(i), (float)GTRandom(i + 1), (float)GTRandom(i + 2), 1.0f);
                        //i += 3;
                        //List<triangle> colorTris = new List<triangle>(node.triangles);
                        //colorTris.ForEach(x => x.SetColor(c));
                        //notOccludedTris.AddRange(colorTris);
                        node.triangles.ForEach(x => x.visibile = true);

                        notOccludedTris.AddRange(node.triangles);
                    }
                }
            }
            else
            {
                TraverseBVHNode(node.left, ref notOccludedTris, ref i, ref frustum);
                TraverseBVHNode(node.right, ref notOccludedTris, ref i, ref frustum);
            }
        }

        public static void TraverseBVHNode(BVHNode node)
        {
            if (node == null)
                return;



            // If it's a leaf node (i.e., no children but has triangles)
            if (node.left == null && node.right == null && node.triangles != null)
            {
                if (node.visibility.First())
                {
                    ;
                }
            }
            else
            {
                TraverseBVHNode(node.left);
                TraverseBVHNode(node.right);
            }
        }
    }
}
