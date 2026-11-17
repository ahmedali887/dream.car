
using System.Collections.Generic;
using UnityEngine;

namespace UCAS
{
    public class TrafficCar : MonoBehaviour
    {
        public Vector3 dir;
        public float speed;
        public float limit = 430f;

        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (rb == null) return;

            var p = Traffic.Player;
            float v = speed;

            if (p != null)
            {
                Vector3 to = p.position - transform.position;
                float ahead = Vector3.Dot(to, dir);
                float side = Mathf.Abs(
                    Vector3.Dot(to, Vector3.Cross(Vector3.up, dir))
                );

                if (ahead > 0 && ahead < 14f && side < 3f)
                    v = 0f;
            }

            Vector3 np = rb.position + dir * v * Time.fixedDeltaTime;

            if (Mathf.Abs(np.x) > limit ||
                Mathf.Abs(np.z) > limit)
                np -= dir * limit * 2f;

            rb.MovePosition(np);
        }

        public static Transform SpawnAll(int count)
        {
            var root = new GameObject("Traffic").transform;
            var rng = new System.Random(9);

            var ids = new[]
            {
                "compact", "hatch", "sedan",
                "sedan2", "suv", "classic"
            };

            for (int i = 0; i < count; i++)
            {
                var spec = CarCatalog.Get(ids[rng.Next(ids.Length)]);

                int k = rng.Next(-3, 4);
                bool alongX = rng.NextDouble() > 0.5;
                bool positive = rng.NextDouble() > 0.5;

                float along = (float)(rng.NextDouble() * 800 - 400);

                Vector3 dir, pos;

                if (alongX)
                {
                    dir = positive ? Vector3.right : Vector3.left;
                    pos = new Vector3(
                        along, 0f,
                        k * 100 + (positive ? -3.5f : 3.5f)
                    );
                }
                else
                {
                    dir = positive ? Vector3.forward : Vector3.back;
                    pos = new Vector3(
                        k * 100 + (positive ? 3.5f : -3.5f),
                        0f, along
                    );
                }

                var st = new CarState
                {
                    id = spec.id,
                    r = Random.value,
                    g = Random.value,
                    b = Random.value
                };

                var car = CarFactory.Build(
                    spec, st, pos,
                    Quaternion.LookRotation(dir), false
                );

                car.transform.SetParent(root, true);

                var tc = car.gameObject.AddComponent<TrafficCar>();
                tc.dir = dir;
                tc.speed = Random.Range(7f, 13f);
            }

            return root;
        }
    }

    public static class Traffic
    {
        public static Transform Player;
    }
}
