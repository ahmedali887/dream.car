
using System.Collections.Generic;
using UnityEngine;

namespace UCAS
{
    public class Coin : MonoBehaviour
    {
        public Currency currency;
        public int amount;

        float respawnAt = -1;
        Renderer rend;
        Collider col;

        void Awake()
        {
            rend = GetComponentInChildren<Renderer>();
            col = GetComponent<Collider>();
        }

        void Update()
        {
            if (respawnAt > 0)
            {
                if (Time.time >= respawnAt)
                {
                    respawnAt = -1;
                    rend.enabled = true;
                    col.enabled = true;
                }
                return;
            }

            transform.Rotate(0, 140 * Time.deltaTime, 0, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (respawnAt > 0) return;

            var car = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<CarController>()
                : null;

            if (car == null || !car.isPlayer) return;

            SaveSystem.Add(currency, amount);

            Toast.Show(
                "+" + amount + " " +
                Loc.T(currency == Currency.Green ? "green" : "yellow"),
                currency == Currency.Green
                    ? new Color(0.4f, 1f, 0.4f)
                    : new Color(1f, 0.9f, 0.2f)
            );

            respawnAt = Time.time + 90f;
            rend.enabled = false;
            col.enabled = false;
        }

        public static Transform SpawnAll()
        {
            var root = new GameObject("Coins").transform;
            var rng = new System.Random(77);

            var gMat = Mats.Get(
                new Color(0.2f, 0.85f, 0.25f),
                0.7f, 0.8f,
                new Color(0.1f, 0.5f, 0.1f)
            );

            var yMat = Mats.Get(
                new Color(1f, 0.82f, 0.1f),
                0.9f, 0.85f,
                new Color(0.7f, 0.5f, 0f)
            );

            var spots = new List<Vector3>();

            for (int k = -4; k <= 4; k++)
            {
                for (int n = 0; n < 6; n++)
                {
                    float t = (float)(rng.NextDouble() * 760 - 380);

                    spots.Add(new Vector3(
                        t, 1.1f,
                        k * 100 + (rng.NextDouble() > 0.5 ? 3.5f : -3.5f)
                    ));

                    spots.Add(new Vector3(
                        k * 100 + (rng.NextDouble() > 0.5 ? 3.5f : -3.5f),
                        1.1f, t
                    ));
                }
            }

            for (int i = 0; i < 24; i++)
                spots.Add(new Vector3(
                    -1300 + i * 110,
                    1.1f,
                    -553.5f - (i % 2) * 13
                ));

            for (int i = 0; i < 16; i++)
            {
                float ang = i / 16f * Mathf.PI * 2;

                spots.Add(new Vector3(
                    Mathf.Cos(ang) * 170,
                    1.1f,
                    970 + Mathf.Sin(ang) * 120
                ));
            }

            for (int i = 0; i < 10; i++)
            {
                spots.Add(new Vector3(
                    450 + i * 100,
                    1.1f,
                    3.5f * (i % 2 == 0 ? 1 : -1)
                ));
            }

            for (int i = 0; i < spots.Count; i++)
            {
                bool yellow = i % 6 == 0;

                var g = new GameObject(
                    yellow ? "CoinYellow" : "CoinGreen"
                );

                g.transform.SetParent(root, false);
                g.transform.position = spots[i];

                var vis = Mats.Prim(
                    PrimitiveType.Cylinder,
                    g.transform,
                    "Vis",
                    Vector3.zero,
                    new Vector3(1.1f, 0.08f, 1.1f),
                    yellow ? yMat : gMat
                );

                vis.transform.localRotation =
                    Quaternion.Euler(90, 0, 0);

                var sc = g.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 2.2f;

                var c = g.AddComponent<Coin>();
                c.currency = yellow ? Currency.Yellow : Currency.Green;
                c.amount = yellow ? 5 : 150;
            }

            return root;
        }
    }
}
