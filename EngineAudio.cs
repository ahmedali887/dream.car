
using UnityEngine;

namespace UCAS
{
    public class EngineAudio : MonoBehaviour
    {
        static AudioClip[] loops = new AudioClip[3];
        static AudioClip popClip;

        CarController car;
        AudioSource engine, pops;
        float basePitch = 1f, prevThrottle;
        float nextPop;
        bool popper;

        static AudioClip MakeLoop(int harmonics, float rough)
        {
            const int rate = 22050;
            const float f0 = 40f;
            int n = rate / 2;
            var data = new float[n];
            var rnd = new System.Random(harmonics * 31 + 7);

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float v = 0;

                for (int h = 1; h <= harmonics; h++)
                    v += Mathf.Sin(2 * Mathf.PI * f0 * h * t + h * 0.7f)
                        / (h * (h < 3 ? 0.8f : 1.3f));

                v += Mathf.Sin(2 * Mathf.PI * f0 * 0.5f * t) * 0.6f;

                float ph = (i % (rate / 40)) / (float)(rate / 40);
                v += (Mathf.PerlinNoise(ph * 6f, harmonics) - 0.5f) * rough * 3f;

                data[i] = Mathf.Clamp(v * 0.18f, -1f, 1f);
            }

            var c = AudioClip.Create("engine" + harmonics, n, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip MakePop()
        {
            const int rate = 22050;
            int n = rate / 6;
            var d = new float[n];
            var rnd = new System.Random(5);

            for (int i = 0; i < n; i++)
            {
                float env = Mathf.Exp(-i / (float)n * 14f);
                d[i] = ((float)rnd.NextDouble() * 2f - 1f) * env * 0.9f;
            }

            var c = AudioClip.Create("pop", n, 1, rate, false);
            c.SetData(d, 0);
            return c;
        }

        public void Setup(CarController c)
        {
            car = c;

            int voice = c.spec.cat == CarCategory.Economy
                ? 0 : (c.spec.cat >= CarCategory.Sports ? 2 : 1);

            if (loops[voice] == null)
                loops[voice] = MakeLoop(
                    new[] { 5, 8, 12 }[voice],
                    new[] { 0.35f, 0.25f, 0.15f }[voice]
                );

            if (popClip == null)
                popClip = MakePop();

            engine = gameObject.AddComponent<AudioSource>();
            engine.clip = loops[voice];
            engine.loop = true;
            engine.spatialBlend = 0.6f;
            engine.minDistance = 6f;
            engine.maxDistance = 70f;
            engine.volume = 0.3f;
            engine.Play();

            pops = gameObject.AddComponent<AudioSource>();
            pops.spatialBlend = 0.7f;
            pops.minDistance = 6f;
            pops.maxDistance = 80f;

            basePitch = Mathf.Lerp(1.15f, 0.8f,
                Mathf.Clamp01(c.spec.hp / 950f));

            popper = c.spec.cat >= CarCategory.Sports;
        }

        public void Silence()
        {
            if (engine != null)
                engine.volume = 0;
        }

        void Update()
        {
            if (car == null || engine == null) return;

            float rpm01 = Mathf.InverseLerp(900f, 7500f, car.Rpm);

            engine.pitch = basePitch * Mathf.Lerp(0.9f, 4.2f, rpm01);
            engine.volume = Mathf.Lerp(
                0.18f, 0.5f,
                Mathf.Max(car.Throttle, rpm01 * 0.5f)
            );

            if (popper && prevThrottle > 0.6f &&
                car.Throttle < 0.15f &&
                car.Rpm > 3800f &&
                Time.time > nextPop)
            {
                pops.pitch = Random.Range(0.8f, 1.3f);
                pops.PlayOneShot(popClip, 0.6f);
                nextPop = Time.time + Random.Range(0.08f, 0.22f);
            }

            if (popper && car.Throttle < 0.1f &&
                car.Rpm > 4500f &&
                Time.time > nextPop &&
                Random.value < 0.08f)
            {
                pops.pitch = Random.Range(0.9f, 1.4f);
                pops.PlayOneShot(popClip, 0.35f);
                nextPop = Time.time + Random.Range(0.1f, 0.3f);
            }

            prevThrottle = car.Throttle;
        }
    }
}
