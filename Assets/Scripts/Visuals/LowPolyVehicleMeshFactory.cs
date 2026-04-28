using System.Collections.Generic;
using GTX.Progression;
using UnityEngine;

namespace GTX.Visuals
{
    internal readonly struct RetroCarVisualProfile
    {
        public readonly VectorSSVehicleId vehicleId;
        public readonly Vector3 scale;
        public readonly float length;
        public readonly float halfWidth;
        public readonly float cabinHalfWidth;
        public readonly float lowerY;
        public readonly float rockerY;
        public readonly float beltY;
        public readonly float hoodY;
        public readonly float deckY;
        public readonly float roofY;
        public readonly float cabinFrontZ;
        public readonly float cabinRearZ;
        public readonly float frontWheelZ;
        public readonly float rearWheelZ;
        public readonly float wheelRadius;
        public readonly float fenderWidth;
        public readonly float hoodLength;
        public readonly float trunkLength;

        private RetroCarVisualProfile(
            VectorSSVehicleId vehicleId,
            Vector3 scale,
            float length,
            float halfWidth,
            float cabinHalfWidth,
            float lowerY,
            float rockerY,
            float beltY,
            float hoodY,
            float deckY,
            float roofY,
            float cabinFrontZ,
            float cabinRearZ,
            float frontWheelZ,
            float rearWheelZ,
            float wheelRadius,
            float fenderWidth,
            float hoodLength,
            float trunkLength)
        {
            this.vehicleId = vehicleId;
            this.scale = scale;
            this.length = length;
            this.halfWidth = halfWidth;
            this.cabinHalfWidth = cabinHalfWidth;
            this.lowerY = lowerY;
            this.rockerY = rockerY;
            this.beltY = beltY;
            this.hoodY = hoodY;
            this.deckY = deckY;
            this.roofY = roofY;
            this.cabinFrontZ = cabinFrontZ;
            this.cabinRearZ = cabinRearZ;
            this.frontWheelZ = frontWheelZ;
            this.rearWheelZ = rearWheelZ;
            this.wheelRadius = wheelRadius;
            this.fenderWidth = fenderWidth;
            this.hoodLength = hoodLength;
            this.trunkLength = trunkLength;
        }

        public float FrontZ
        {
            get { return length * 0.5f; }
        }

        public float RearZ
        {
            get { return -length * 0.5f; }
        }

        public static RetroCarVisualProfile ForVehicle(VectorSSVehicleId vehicleId, Vector3 visualScale)
        {
            switch (vehicleId)
            {
                case VectorSSVehicleId.Hammer:
                    return Build(vehicleId, visualScale, 4.92f, 1.08f, 0.74f, 0.32f, 0.55f, 0.9f, 1.08f, 0.98f, 1.5f, 0.34f, -0.98f, 1.35f, -1.38f, 0.54f, 0.28f, 1.72f, 1.24f);
                case VectorSSVehicleId.Needle:
                    return Build(vehicleId, visualScale, 5.08f, 0.98f, 0.66f, 0.28f, 0.48f, 0.82f, 1.0f, 0.88f, 1.34f, 0.46f, -1.02f, 1.42f, -1.34f, 0.52f, 0.2f, 1.9f, 1.12f);
                case VectorSSVehicleId.Surge:
                    return Build(vehicleId, visualScale, 4.86f, 1.0f, 0.68f, 0.3f, 0.5f, 0.85f, 1.04f, 0.9f, 1.42f, 0.38f, -0.92f, 1.34f, -1.3f, 0.52f, 0.18f, 1.78f, 1.1f);
                case VectorSSVehicleId.Hauler:
                    return Build(vehicleId, visualScale, 5.36f, 1.06f, 0.72f, 0.34f, 0.58f, 0.96f, 1.12f, 1.02f, 1.58f, 0.62f, -0.28f, 1.38f, -1.38f, 0.56f, 0.28f, 1.64f, 2.16f);
                default:
                    return Build(vehicleId, visualScale, 4.9f, 1.02f, 0.68f, 0.3f, 0.5f, 0.84f, 1.02f, 0.92f, 1.4f, 0.4f, -0.95f, 1.36f, -1.32f, 0.52f, 0.2f, 1.78f, 1.12f);
            }
        }

        private static RetroCarVisualProfile Build(
            VectorSSVehicleId vehicleId,
            Vector3 visualScale,
            float length,
            float halfWidth,
            float cabinHalfWidth,
            float lowerY,
            float rockerY,
            float beltY,
            float hoodY,
            float deckY,
            float roofY,
            float cabinFrontZ,
            float cabinRearZ,
            float frontWheelZ,
            float rearWheelZ,
            float wheelRadius,
            float fenderWidth,
            float hoodLength,
            float trunkLength)
        {
            return new RetroCarVisualProfile(
                vehicleId,
                visualScale,
                length * visualScale.z,
                halfWidth * visualScale.x,
                cabinHalfWidth * visualScale.x,
                lowerY * visualScale.y,
                rockerY * visualScale.y,
                beltY * visualScale.y,
                hoodY * visualScale.y,
                deckY * visualScale.y,
                roofY * visualScale.y,
                cabinFrontZ * visualScale.z,
                cabinRearZ * visualScale.z,
                frontWheelZ * visualScale.z,
                rearWheelZ * visualScale.z,
                wheelRadius * Mathf.Max(visualScale.x, visualScale.y),
                fenderWidth * visualScale.x,
                hoodLength * visualScale.z,
                trunkLength * visualScale.z);
        }
    }

    internal static class LowPolyVehicleMeshFactory
    {
        private readonly struct BodySection
        {
            public readonly float z;
            public readonly float lowerHalfWidth;
            public readonly float shoulderHalfWidth;
            public readonly float upperHalfWidth;
            public readonly float lowerY;
            public readonly float beltY;
            public readonly float topY;

            public BodySection(float z, float lowerHalfWidth, float shoulderHalfWidth, float upperHalfWidth, float lowerY, float beltY, float topY)
            {
                this.z = z;
                this.lowerHalfWidth = lowerHalfWidth;
                this.shoulderHalfWidth = shoulderHalfWidth;
                this.upperHalfWidth = upperHalfWidth;
                this.lowerY = lowerY;
                this.beltY = beltY;
                this.topY = topY;
            }
        }

        private readonly struct BikeBodySection
        {
            public readonly float z;
            public readonly float lowerHalfWidth;
            public readonly float shoulderHalfWidth;
            public readonly float upperHalfWidth;
            public readonly float lowerY;
            public readonly float beltY;
            public readonly float topY;

            public BikeBodySection(float z, float lowerHalfWidth, float shoulderHalfWidth, float upperHalfWidth, float lowerY, float beltY, float topY)
            {
                this.z = z;
                this.lowerHalfWidth = lowerHalfWidth;
                this.shoulderHalfWidth = shoulderHalfWidth;
                this.upperHalfWidth = upperHalfWidth;
                this.lowerY = lowerY;
                this.beltY = beltY;
                this.topY = topY;
            }
        }

        public static GameObject CreateRetroCarBody(string name, Transform parent, RetroCarVisualProfile profile, Material material, bool keepCollider)
        {
            GameObject gameObject = CreateMeshObject(name, parent, Vector3.zero, Quaternion.identity, Vector3.one, material);
            Mesh mesh = BuildRetroCarBodyMesh(profile);
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;

            if (keepCollider)
            {
                MeshCollider collider = gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = true;
            }

            return gameObject;
        }

        public static GameObject CreateRazorBikeBody(string name, Transform parent, Vector3 visualScale, Material material, bool keepCollider)
        {
            GameObject gameObject = CreateMeshObject(name, parent, Vector3.zero, Quaternion.identity, Vector3.one, material);
            Mesh mesh = BuildRazorBikeBodyMesh(visualScale);
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;

            if (keepCollider)
            {
                MeshCollider collider = gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = true;
            }

            return gameObject;
        }

        public static GameObject CreateQuadPanel(string name, Transform parent, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material material)
        {
            GameObject gameObject = CreateMeshObject(name, parent, Vector3.zero, Quaternion.identity, Vector3.one, material);
            Mesh mesh = new Mesh { name = name + " Mesh" };
            mesh.vertices = new[] { a, b, c, d, a, d, c, b };
            mesh.triangles = new[]
            {
                0, 1, 2,
                0, 2, 3,
                4, 5, 6,
                4, 6, 7
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            return gameObject;
        }

        private static GameObject CreateMeshObject(string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = localRotation;
            gameObject.transform.localScale = localScale;
            gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = material;
            return gameObject;
        }

        private static Mesh BuildRetroCarBodyMesh(RetroCarVisualProfile profile)
        {
            BodySection[] sections = BuildSections(profile);
            List<Vector3> vertices = new List<Vector3>(sections.Length * 6);
            List<int> triangles = new List<int>((sections.Length - 1) * 36 + 36);

            for (int i = 0; i < sections.Length; i++)
            {
                AddSectionVertices(vertices, sections[i]);
            }

            for (int i = 0; i < sections.Length - 1; i++)
            {
                int a = i * 6;
                int b = (i + 1) * 6;
                AddQuadFacing(vertices, triangles, a + 0, b + 0, b + 1, a + 1, Vector3.down);
                AddQuadFacing(vertices, triangles, a + 0, a + 2, b + 2, b + 0, Vector3.left);
                AddQuadFacing(vertices, triangles, a + 1, b + 1, b + 3, a + 3, Vector3.right);
                AddQuadFacing(vertices, triangles, a + 2, a + 4, b + 4, b + 2, new Vector3(-0.6f, 0.8f, 0f));
                AddQuadFacing(vertices, triangles, a + 3, b + 3, b + 5, a + 5, new Vector3(0.6f, 0.8f, 0f));
                AddQuadFacing(vertices, triangles, a + 4, a + 5, b + 5, b + 4, Vector3.up);
            }

            AddCap(vertices, triangles, 0, Vector3.forward);
            AddCap(vertices, triangles, (sections.Length - 1) * 6, Vector3.back);

            Mesh mesh = new Mesh { name = "VECTR Retro Car Body Mesh" };
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildRazorBikeBodyMesh(Vector3 scale)
        {
            BikeBodySection[] sections = BuildRazorBikeSections(scale);
            List<Vector3> vertices = new List<Vector3>(sections.Length * 6);
            List<int> triangles = new List<int>((sections.Length - 1) * 36 + 36);

            for (int i = 0; i < sections.Length; i++)
            {
                AddBikeSectionVertices(vertices, sections[i]);
            }

            for (int i = 0; i < sections.Length - 1; i++)
            {
                int a = i * 6;
                int b = (i + 1) * 6;
                AddQuadFacing(vertices, triangles, a + 0, b + 0, b + 1, a + 1, Vector3.down);
                AddQuadFacing(vertices, triangles, a + 0, a + 2, b + 2, b + 0, Vector3.left);
                AddQuadFacing(vertices, triangles, a + 1, b + 1, b + 3, a + 3, Vector3.right);
                AddQuadFacing(vertices, triangles, a + 2, a + 4, b + 4, b + 2, new Vector3(-0.45f, 0.9f, 0f));
                AddQuadFacing(vertices, triangles, a + 3, b + 3, b + 5, a + 5, new Vector3(0.45f, 0.9f, 0f));
                AddQuadFacing(vertices, triangles, a + 4, a + 5, b + 5, b + 4, Vector3.up);
            }

            AddCap(vertices, triangles, 0, Vector3.forward);
            AddCap(vertices, triangles, (sections.Length - 1) * 6, Vector3.back);

            Mesh mesh = new Mesh { name = "VECTR Razor Bike Body Mesh" };
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static BikeBodySection[] BuildRazorBikeSections(Vector3 scale)
        {
            return new[]
            {
                new BikeBodySection(1.44f * scale.z, 0.08f * scale.x, 0.16f * scale.x, 0.05f * scale.x, 0.58f * scale.y, 0.78f * scale.y, 0.96f * scale.y),
                new BikeBodySection(0.92f * scale.z, 0.22f * scale.x, 0.38f * scale.x, 0.18f * scale.x, 0.56f * scale.y, 0.95f * scale.y, 1.2f * scale.y),
                new BikeBodySection(0.22f * scale.z, 0.3f * scale.x, 0.46f * scale.x, 0.3f * scale.x, 0.66f * scale.y, 1.05f * scale.y, 1.28f * scale.y),
                new BikeBodySection(-0.46f * scale.z, 0.2f * scale.x, 0.34f * scale.x, 0.24f * scale.x, 0.72f * scale.y, 0.98f * scale.y, 1.12f * scale.y),
                new BikeBodySection(-1.28f * scale.z, 0.1f * scale.x, 0.26f * scale.x, 0.08f * scale.x, 0.78f * scale.y, 1.02f * scale.y, 1.2f * scale.y)
            };
        }

        private static BodySection[] BuildSections(RetroCarVisualProfile profile)
        {
            float front = profile.FrontZ;
            float rear = profile.RearZ;
            float w = profile.halfWidth;
            float cabinW = profile.cabinHalfWidth;
            float frontDeckZ = Mathf.Max(profile.cabinFrontZ + profile.hoodLength * 0.52f, front * 0.58f);
            float roofFrontZ = profile.cabinFrontZ - profile.hoodLength * 0.18f;
            float roofRearZ = profile.cabinRearZ + profile.trunkLength * 0.12f;
            float rearGlassZ = profile.cabinRearZ - profile.trunkLength * 0.38f;
            float deckZ = Mathf.Min(profile.cabinRearZ - profile.trunkLength * 0.68f, rear * 0.68f);
            float noseBelt = Mathf.Lerp(profile.rockerY, profile.beltY, 0.35f);
            float tailBelt = Mathf.Lerp(profile.rockerY, profile.beltY, 0.42f);

            return new[]
            {
                new BodySection(front, w * 0.72f, w * 0.82f, w * 0.58f, profile.lowerY, noseBelt, noseBelt + 0.04f * profile.scale.y),
                new BodySection(frontDeckZ, w * 1.0f, w * 1.04f, w * 0.72f, profile.lowerY, profile.beltY, profile.hoodY),
                new BodySection(profile.cabinFrontZ, w * 1.03f, w * 1.0f, cabinW * 1.05f, profile.lowerY, profile.beltY + 0.04f * profile.scale.y, profile.hoodY + 0.08f * profile.scale.y),
                new BodySection(roofFrontZ, w * 0.98f, w * 0.92f, cabinW, profile.lowerY, profile.beltY + 0.1f * profile.scale.y, profile.roofY),
                new BodySection(roofRearZ, w * 0.96f, w * 0.9f, cabinW * 0.94f, profile.lowerY, profile.beltY + 0.08f * profile.scale.y, profile.roofY - 0.03f * profile.scale.y),
                new BodySection(rearGlassZ, w * 1.0f, w * 0.98f, cabinW * 1.02f, profile.lowerY, profile.beltY, profile.deckY + 0.24f * profile.scale.y),
                new BodySection(deckZ, w * 1.02f, w * 1.02f, w * 0.78f, profile.lowerY, profile.beltY - 0.04f * profile.scale.y, profile.deckY),
                new BodySection(rear, w * 0.82f, w * 0.9f, w * 0.64f, profile.lowerY, tailBelt, tailBelt + 0.08f * profile.scale.y)
            };
        }

        private static void AddSectionVertices(List<Vector3> vertices, BodySection section)
        {
            vertices.Add(new Vector3(-section.lowerHalfWidth, section.lowerY, section.z));
            vertices.Add(new Vector3(section.lowerHalfWidth, section.lowerY, section.z));
            vertices.Add(new Vector3(-section.shoulderHalfWidth, section.beltY, section.z));
            vertices.Add(new Vector3(section.shoulderHalfWidth, section.beltY, section.z));
            vertices.Add(new Vector3(-section.upperHalfWidth, section.topY, section.z));
            vertices.Add(new Vector3(section.upperHalfWidth, section.topY, section.z));
        }

        private static void AddBikeSectionVertices(List<Vector3> vertices, BikeBodySection section)
        {
            vertices.Add(new Vector3(-section.lowerHalfWidth, section.lowerY, section.z));
            vertices.Add(new Vector3(section.lowerHalfWidth, section.lowerY, section.z));
            vertices.Add(new Vector3(-section.shoulderHalfWidth, section.beltY, section.z));
            vertices.Add(new Vector3(section.shoulderHalfWidth, section.beltY, section.z));
            vertices.Add(new Vector3(-section.upperHalfWidth, section.topY, section.z));
            vertices.Add(new Vector3(section.upperHalfWidth, section.topY, section.z));
        }

        private static void AddCap(List<Vector3> vertices, List<int> triangles, int start, Vector3 outward)
        {
            AddTriangleFacing(vertices, triangles, start + 0, start + 1, start + 3, outward);
            AddTriangleFacing(vertices, triangles, start + 0, start + 3, start + 2, outward);
            AddTriangleFacing(vertices, triangles, start + 2, start + 3, start + 5, outward);
            AddTriangleFacing(vertices, triangles, start + 2, start + 5, start + 4, outward);
        }

        private static void AddQuadFacing(List<Vector3> vertices, List<int> triangles, int a, int b, int c, int d, Vector3 outward)
        {
            AddTriangleFacing(vertices, triangles, a, b, c, outward);
            AddTriangleFacing(vertices, triangles, a, c, d, outward);
        }

        private static void AddTriangleFacing(List<Vector3> vertices, List<int> triangles, int a, int b, int c, Vector3 outward)
        {
            Vector3 va = vertices[a];
            Vector3 vb = vertices[b];
            Vector3 vc = vertices[c];
            if (Vector3.Dot(Vector3.Cross(vb - va, vc - va), outward) >= 0f)
            {
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }
            else
            {
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
            }
        }
    }
}
