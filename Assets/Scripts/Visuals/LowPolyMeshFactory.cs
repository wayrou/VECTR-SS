using System.Collections.Generic;
using UnityEngine;

namespace GTX.Visuals
{
    public static class LowPolyMeshFactory
    {
        public static GameObject CreateTrackRibbon(string name, Transform parent, Vector3[] centerline, float[] widths, Material material, bool keepCollider)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;

            Mesh mesh = BuildTrackRibbonMesh(centerline, widths);
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = material;

            if (keepCollider)
            {
                MeshCollider collider = gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = false;
            }

            return gameObject;
        }

        public static GameObject CreateChamferedBox(string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, bool keepCollider, float bevel = 0.08f)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = localRotation;
            gameObject.transform.localScale = localScale;

            Mesh mesh = BuildChamferedBoxMesh(bevel);
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = material;

            if (keepCollider)
            {
                gameObject.AddComponent<BoxCollider>();
            }

            return gameObject;
        }

        public static GameObject CreatePrism(string name, Transform parent, int sides, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, bool keepCollider)
        {
            sides = Mathf.Clamp(sides, 3, 16);
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = localRotation;
            gameObject.transform.localScale = localScale;

            Mesh mesh = BuildPrismMesh(sides);
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = material;

            if (keepCollider)
            {
                MeshCollider collider = gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = true;
            }

            return gameObject;
        }

        private static Mesh BuildTrackRibbonMesh(Vector3[] centerline, float[] widths)
        {
            int count = centerline == null ? 0 : centerline.Length;
            if (count < 2)
            {
                Mesh empty = new Mesh { name = "GTX Empty Track Ribbon" };
                return empty;
            }

            List<Vector3> vertices = new List<Vector3>(count * 2);
            List<int> triangles = new List<int>((count - 1) * 6);
            bool closed = Vector3.SqrMagnitude(centerline[0] - centerline[count - 1]) < 0.001f;
            for (int i = 0; i < count; i++)
            {
                if (closed && i == count - 1)
                {
                    vertices.Add(vertices[0]);
                    vertices.Add(vertices[1]);
                    continue;
                }

                Vector3 previous = closed && i == 0 ? centerline[count - 2] : centerline[Mathf.Max(0, i - 1)];
                Vector3 next = closed && i == 0 ? centerline[1] : centerline[Mathf.Min(count - 1, i + 1)];
                Vector3 tangent = next - previous;
                tangent.y = 0f;
                if (tangent.sqrMagnitude < 0.001f)
                {
                    tangent = Vector3.forward;
                }

                Vector3 right = Vector3.Cross(Vector3.up, tangent.normalized).normalized;
                float width = WidthAt(widths, i);
                vertices.Add(centerline[i] - right * width * 0.5f);
                vertices.Add(centerline[i] + right * width * 0.5f);
            }

            for (int i = 0; i < count - 1; i++)
            {
                int start = i * 2;
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }

            Mesh mesh = new Mesh { name = "GTX Low Poly Track Ribbon" };
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildChamferedBoxMesh(float bevel)
        {
            float inset = Mathf.Clamp(0.5f - bevel, 0.26f, 0.49f);
            List<Vector3> vertices = new List<Vector3>(144);
            List<int> triangles = new List<int>(216);

            AddBoxFace(vertices, triangles, new Vector3(-inset, 0.5f, -inset), new Vector3(inset, 0.5f, -inset), new Vector3(inset, 0.5f, inset), new Vector3(-inset, 0.5f, inset), Vector3.up);
            AddBoxFace(vertices, triangles, new Vector3(-inset, -0.5f, inset), new Vector3(inset, -0.5f, inset), new Vector3(inset, -0.5f, -inset), new Vector3(-inset, -0.5f, -inset), Vector3.down);
            AddBoxFace(vertices, triangles, new Vector3(0.5f, -inset, -inset), new Vector3(0.5f, -inset, inset), new Vector3(0.5f, inset, inset), new Vector3(0.5f, inset, -inset), Vector3.right);
            AddBoxFace(vertices, triangles, new Vector3(-0.5f, -inset, inset), new Vector3(-0.5f, -inset, -inset), new Vector3(-0.5f, inset, -inset), new Vector3(-0.5f, inset, inset), Vector3.left);
            AddBoxFace(vertices, triangles, new Vector3(-inset, -inset, 0.5f), new Vector3(inset, -inset, 0.5f), new Vector3(inset, inset, 0.5f), new Vector3(-inset, inset, 0.5f), Vector3.forward);
            AddBoxFace(vertices, triangles, new Vector3(inset, -inset, -0.5f), new Vector3(-inset, -inset, -0.5f), new Vector3(-inset, inset, -0.5f), new Vector3(inset, inset, -0.5f), Vector3.back);

            int[] signs = { -1, 1 };
            foreach (int ySign in signs)
            {
                foreach (int zSign in signs)
                {
                    AddBoxFace(
                        vertices,
                        triangles,
                        new Vector3(-inset, ySign * 0.5f, zSign * inset),
                        new Vector3(inset, ySign * 0.5f, zSign * inset),
                        new Vector3(inset, ySign * inset, zSign * 0.5f),
                        new Vector3(-inset, ySign * inset, zSign * 0.5f),
                        new Vector3(0f, ySign, zSign).normalized);
                }
            }

            foreach (int xSign in signs)
            {
                foreach (int zSign in signs)
                {
                    AddBoxFace(
                        vertices,
                        triangles,
                        new Vector3(xSign * 0.5f, -inset, zSign * inset),
                        new Vector3(xSign * 0.5f, inset, zSign * inset),
                        new Vector3(xSign * inset, inset, zSign * 0.5f),
                        new Vector3(xSign * inset, -inset, zSign * 0.5f),
                        new Vector3(xSign, 0f, zSign).normalized);
                }
            }

            foreach (int xSign in signs)
            {
                foreach (int ySign in signs)
                {
                    AddBoxFace(
                        vertices,
                        triangles,
                        new Vector3(xSign * inset, ySign * 0.5f, -inset),
                        new Vector3(xSign * inset, ySign * 0.5f, inset),
                        new Vector3(xSign * 0.5f, ySign * inset, inset),
                        new Vector3(xSign * 0.5f, ySign * inset, -inset),
                        new Vector3(xSign, ySign, 0f).normalized);
                }
            }

            foreach (int xSign in signs)
            {
                foreach (int ySign in signs)
                {
                    foreach (int zSign in signs)
                    {
                        AddTriangleFacing(
                            vertices,
                            triangles,
                            new Vector3(xSign * 0.5f, ySign * inset, zSign * inset),
                            new Vector3(xSign * inset, ySign * 0.5f, zSign * inset),
                            new Vector3(xSign * inset, ySign * inset, zSign * 0.5f),
                            new Vector3(xSign, ySign, zSign).normalized);
                    }
                }
            }

            Mesh mesh = new Mesh { name = "GTX Low Poly Chamfered Box" };
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static GameObject CreateWedge(string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, bool keepCollider)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = localRotation;
            gameObject.transform.localScale = localScale;

            Mesh mesh = BuildWedgeMesh();
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = material;

            if (keepCollider)
            {
                MeshCollider collider = gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = true;
            }

            return gameObject;
        }

        private static Mesh BuildPrismMesh(int sides)
        {
            List<Vector3> vertices = new List<Vector3>(sides * 12);
            List<int> triangles = new List<int>(sides * 12);
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                Vector3 backA = PrismPoint(i, sides, -0.5f);
                Vector3 frontA = PrismPoint(i, sides, 0.5f);
                Vector3 backB = PrismPoint(next, sides, -0.5f);
                Vector3 frontB = PrismPoint(next, sides, 0.5f);

                AddQuad(vertices, triangles, backA, frontA, frontB, backB);
                AddTriangle(vertices, triangles, Vector3.back * 0.5f, backB, backA);
                AddTriangle(vertices, triangles, Vector3.forward * 0.5f, frontA, frontB);
            }

            Mesh mesh = new Mesh { name = "GTX Low Poly Prism" };
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildWedgeMesh()
        {
            Vector3 v0 = new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3 v1 = new Vector3(0.5f, -0.5f, -0.5f);
            Vector3 v2 = new Vector3(-0.5f, -0.5f, 0.5f);
            Vector3 v3 = new Vector3(0.5f, -0.5f, 0.5f);
            Vector3 v4 = new Vector3(-0.5f, 0.5f, -0.5f);
            Vector3 v5 = new Vector3(0.5f, 0.5f, -0.5f);

            List<Vector3> vertices = new List<Vector3>(18);
            List<int> triangles = new List<int>(18);
            AddQuad(vertices, triangles, v0, v2, v3, v1);
            AddQuad(vertices, triangles, v0, v1, v5, v4);
            AddQuad(vertices, triangles, v2, v4, v5, v3);
            AddTriangle(vertices, triangles, v0, v4, v2);
            AddTriangle(vertices, triangles, v1, v3, v5);

            Mesh mesh = new Mesh { name = "GTX Low Poly Wedge" };
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 PrismPoint(int index, int sides, float z)
        {
            float angle = index * Mathf.PI * 2f / sides;
            return new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.5f, z);
        }

        private static float WidthAt(float[] widths, int index)
        {
            if (widths == null || widths.Length == 0)
            {
                return 18f;
            }

            return widths[Mathf.Clamp(index, 0, widths.Length - 1)];
        }

        private static void AddBoxFace(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);

            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) >= 0f)
            {
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }
            else
            {
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start);
                triangles.Add(start + 3);
                triangles.Add(start + 2);
            }
        }

        private static void AddTriangleFacing(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);

            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) >= 0f)
            {
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }
            else
            {
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
            }
        }

        private static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }
    }
}
