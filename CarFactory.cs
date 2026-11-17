
using UnityEngine;

namespace UCAS
{
    public static class CarFactory
    {
        public const string BodyName = "Body";

        public static CarController Build(
            CarSpec s,
            CarState st,
            Vector3 pos,
            Quaternion rot,
            bool physics)
        {
            var root = new GameObject("Car_" + s.id);
            root.transform.SetPositionAndRotation(pos, rot);

            Color paint = st != null ? st.Paint : s.color;
            var paintMat = Mats.Get(paint, 0.6f, 0.75f);
            var glass = Mats.Get(new Color(0.08f, 0.1f, 0.13f), 0.9f, 0.9f);
            var dark = Mats.Get(new Color(0.07f, 0.07f, 0.07f), 0f, 0.2f);
            var tyre = Mats.Get(new Color(0.05f, 0.05f, 0.05f), 0f, 0.1f);
            var rim = Mats.Get(new Color(0.75f, 0.75f, 0.78f), 1f, 0.8f);
            var head = Mats.Get(new Color(1f, 1f, 0.85f), 0f, 0.9f, new Color(1.4f, 1.4f, 1.1f));
            var tail = Mats.Get(new Color(0.8f, 0.05f, 0.05f), 0f, 0.8f, new Color(1.2f, 0.05f, 0.05f));

            float wr = s.cat == CarCategory.SUV ? 0.40f : 0.34f;
            float L = s.length, W = s.width, H = s.height;
            float wheelBase = L * 0.62f;
            float track = W - 0.28f;
            float bodyH = H * 0.36f;
            float bodyY = wr * 0.9f + bodyH * 0.5f;
            float cabH = H * 0.34f;

            var vis = new GameObject(BodyName).transform;
            vis.SetParent(root.transform, false);

            Mats.Prim(PrimitiveType.Cube, vis, "Paint_Body",
                new Vector3(0, bodyY, 0),
                new Vector3(W, bodyH, L), paintMat);

            float roofZ =
                s.cat == CarCategory.Sports ||
                s.cat == CarCategory.Super
                ? -0.08f * L : -0.04f * L;

            Mats.Prim(PrimitiveType.Cube, vis, "Glass",
                new Vector3(0, bodyY + bodyH * 0.5f + cabH * 0.5f, roofZ),
                new Vector3(W * 0.88f, cabH,
                L * (s.cat == CarCategory.SUV ? 0.62f : 0.46f)),
                glass);

            Mats.Prim(PrimitiveType.Cube, vis, "Paint_Roof",
                new Vector3(0, bodyY + bodyH * 0.5f + cabH + 0.02f, roofZ),
                new Vector3(W * 0.9f, 0.05f,
                L * (s.cat == CarCategory.SUV ? 0.64f : 0.48f)),
                paintMat);

            Mats.Prim(PrimitiveType.Cube, vis, "Bumper_F",
                new Vector3(0, wr * 0.9f + 0.05f, L * 0.5f),
                new Vector3(W * 0.98f, 0.18f, 0.12f), dark);

            Mats.Prim(PrimitiveType.Cube, vis, "Bumper_R",
                new Vector3(0, wr * 0.9f + 0.05f, -L * 0.5f),
                new Vector3(W * 0.98f, 0.18f, 0.12f), dark);

            for (int i = -1; i <= 1; i += 2)
            {
                Mats.Prim(PrimitiveType.Cube, vis, "Headlight",
                    new Vector3(i * W * 0.34f, bodyY + 0.02f, L * 0.5f + 0.005f),
                    new Vector3(W * 0.2f, 0.1f, 0.05f), head);

                Mats.Prim(PrimitiveType.Cube, vis, "Taillight",
                    new Vector3(i * W * 0.34f, bodyY + 0.02f, -L * 0.5f - 0.005f),
                    new Vector3(W * 0.2f, 0.09f, 0.05f), tail);
            }

            if (s.cat == CarCategory.Sports || s.cat == CarCategory.Super)
            {
                Mats.Prim(PrimitiveType.Cube, vis, "Paint_Spoiler",
                    new Vector3(0, bodyY + bodyH * 0.5f + 0.22f, -L * 0.47f),
                    new Vector3(W * 0.85f, 0.04f, 0.22f), paintMat);
            }

            Mats.Prim(PrimitiveType.Cylinder, vis, "Exhaust",
                new Vector3(W * 0.3f, wr * 0.9f + 0.08f, -L * 0.5f - 0.08f),
                new Vector3(0.09f, 0.08f, 0.09f), rim)
                .transform.localRotation = Quaternion.Euler(90, 0, 0);

            var bc = new GameObject("Collider_Body").AddComponent<BoxCollider>();
            bc.transform.SetParent(root.transform, false);
            bc.center = new Vector3(0, bodyY, 0);
            bc.size = new Vector3(W, bodyH, L);

            var cc = new GameObject("Collider_Cabin").AddComponent<BoxCollider>();
            cc.transform.SetParent(root.transform, false);
            cc.center = new Vector3(0, bodyY + bodyH * 0.5f + cabH * 0.5f, roofZ);
            cc.size = new Vector3(W * 0.85f, cabH, L * 0.45f);

            var ctrl = root.AddComponent<CarController>();
            ctrl.spec = s;
            ctrl.state = st ?? new CarState { id = s.id };
            ctrl.wheels = new WheelCollider[4];
            ctrl.wheelVis = new Transform[4];

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = s.mass;
            rb.centerOfMass = new Vector3(0, wr * 0.55f, 0);
            rb.drag = 0.02f;
            rb.angularDrag = 0.6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.isKinematic = !physics;

            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * track * 0.5f;
                float z = (i < 2 ? 1 : -1) * wheelBase * 0.5f;

                var wcGo = new GameObject("WheelCollider_" + i);
                wcGo.transform.SetParent(root.transform, false);
                wcGo.transform.localPosition = new Vector3(x, wr + 0.2f, z);

                var wc = wcGo.AddComponent<WheelCollider>();
                wc.radius = wr;
                wc.mass = 25f;
                wc.suspensionDistance = 0.22f;

                var sp = wc.suspensionSpring;
                sp.spring = s.mass * 22f;
                sp.damper = s.mass * 2.6f;
                sp.targetPosition = 0.5f;
                wc.suspensionSpring = sp;
                wc.enabled = physics;
                ctrl.wheels[i] = wc;

                var pivot = new GameObject("WheelVis_" + i).transform;
                pivot.SetParent(root.transform, false);
                pivot.localPosition = new Vector3(x, wr + 0.1f, z);

                var tyreGo = Mats.Prim(PrimitiveType.Cylinder, pivot,
                    "Tyre", Vector3.zero,
                    new Vector3(wr * 2f, 0.12f, wr * 2f), tyre);

                tyreGo.transform.localRotation = Quaternion.Euler(0, 0, 90);

                var rimGo = Mats.Prim(PrimitiveType.Cylinder, pivot,
                    "Rim",
                    new Vector3((i % 2 == 0 ? -1 : 1) * 0.02f, 0, 0),
                    new Vector3(wr * 1.25f, 0.125f, wr * 1.25f), rim);

                rimGo.transform.localRotation = Quaternion.Euler(0, 0, 90);
                ctrl.wheelVis[i] = pivot;
            }

            ctrl.Init();

            if (!physics)
                ctrl.PlaceWheelsStatic(wr);

            return ctrl;
        }

        public static void Repaint(GameObject car, Color c)
        {
            var m = Mats.Get(c, 0.6f, 0.75f);

            foreach (var r in car.GetComponentsInChildren<Renderer>())
            {
                if (r.name.StartsWith("Paint"))
                    r.sharedMaterial = m;
            }
        }
    }
}
