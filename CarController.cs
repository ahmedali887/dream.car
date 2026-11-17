
using UnityEngine;

namespace UCAS
{
    public class CarController : MonoBehaviour
    {
        public CarSpec spec;
        public CarState state;
        public WheelCollider[] wheels;
        public Transform[] wheelVis;
        public bool isPlayer;

        public float KPH { get; private set; }
        public float Rpm { get; private set; }
        public int Gear { get; private set; }
        public bool Reversing { get; private set; }
        public bool AutoGear = true;
        public float Throttle { get; private set; }
        public float distanceM;

        Rigidbody rb;
        EngineAudio audioFx;
        float wheelRadius;
        float powerMul = 1, speedMul = 1, gripMul = 1;
        float lastShift;

        static readonly float[] Ratios =
            { 3.4f, 2.3f, 1.65f, 1.25f, 0.98f, 0.78f };

        const float Final = 3.4f;
        const float ReverseRatio = 3.0f;

        public void Init()
        {
            rb = GetComponent<Rigidbody>();
            wheelRadius = wheels[0].radius;
            ApplyUpgrades();
        }

        public void ApplyUpgrades()
        {
            powerMul = CarCatalog.PowerMul(state);
            speedMul = CarCatalog.SpeedMul(state);
            gripMul = CarCatalog.GripMul(state);

            foreach (var w in wheels)
            {
                var f = w.forwardFriction;
                f.stiffness = 1.7f * gripMul;
                w.forwardFriction = f;

                var sd = w.sidewaysFriction;
                sd.stiffness = 1.9f * gripMul;
                w.sidewaysFriction = sd;
            }
        }

        public void PlaceWheelsStatic(float wr)
        {
            foreach (var t in wheelVis)
            {
                var p = t.localPosition;
                p.y = wr;
                t.localPosition = p;
            }
        }

        public void EnableAudio(bool on)
        {
            if (on && audioFx == null)
            {
                audioFx = gameObject.AddComponent<EngineAudio>();
                audioFx.Setup(this);
            }

            if (audioFx != null)
                audioFx.enabled = on;

            if (!on && audioFx != null)
                audioFx.Silence();
        }

        bool Drives(int i)
        {
            switch (spec.cat)
            {
                case CarCategory.Economy:
                case CarCategory.Sedan:
                    return i < 2;

                case CarCategory.SUV:
                case CarCategory.Super:
                    return true;

                default:
                    return i >= 2;
            }
        }

        void Update()
        {
            for (int i = 0; i < 4; i++)
            {
                if (!wheels[i].enabled) continue;

                wheels[i].GetWorldPose(out var p, out var q);
                wheelVis[i].position = p;
                wheelVis[i].rotation = q;
            }
        }

        void FixedUpdate()
        {
            if (rb == null || rb.isKinematic) return;

            float thr = 0, brk = 0, steer = 0;
            bool hand = false;

            if (isPlayer)
            {
                thr = CarInput.Throttle;
                brk = CarInput.Brake;
                steer = CarInput.Steer;
                hand = CarInput.Handbrake;
            }

            float fwdSpeed =
                Vector3.Dot(rb.velocity, transform.forward);

            KPH = Mathf.Abs(fwdSpeed) * 3.6f;

            float driveInput = 0;
            float brakeInput = 0;

            Reversing = false;

            if (thr > 0.01f)
            {
                if (fwdSpeed < -1f)
                    brakeInput = thr;
                else
                    driveInput = thr;
            }

            if (brk > 0.01f)
            {
                if (fwdSpeed > 1f || thr > 0.01f)
                    brakeInput = Mathf.Max(brakeInput, brk);
                else
                {
                    Reversing = true;
                    driveInput = -brk;
                }
            }

            Throttle = Mathf.Abs(driveInput);

            float wheelRpm = 0;
            int driven = 0;

            for (int i = 0; i < 4; i++)
            {
                if (Drives(i))
                {
                    wheelRpm += Mathf.Abs(wheels[i].rpm);
                    driven++;
                }
            }

            wheelRpm /= Mathf.Max(1, driven);

            float ratio = Reversing
                ? ReverseRatio
                : Ratios[Gear];

            float targetRpm = Mathf.Clamp(
                wheelRpm * ratio * Final,
                900f,
                7600f
            );

            if (Throttle > 0.1f && KPH < 15f)
                targetRpm = Mathf.Max(
                    targetRpm,
                    900f + Throttle * 3800f
                );

            Rpm = Mathf.Lerp(
                Rpm,
                targetRpm,
                12f * Time.fixedDeltaTime
            );

            if (!Reversing)
                Shift();

            float peak = spec.hp * 2.1f * powerMul;

            float curve = Mathf.Clamp01(
                1f - Mathf.Pow((Rpm - 4600f) / 5200f, 2f) * 0.55f
            );

            float engineNm =
                peak * curve * Mathf.Abs(driveInput);

            float limitKph = spec.maxKph * speedMul;

            if (Rpm > 7300f || KPH > limitKph)
                engineNm = 0f;

            if (Reversing && KPH > 28f)
                engineNm = 0f;

            float wheelNm =
                engineNm * ratio * Final * 0.85f /
                Mathf.Max(1, driven);

            wheelNm *= Mathf.Sign(driveInput);

            float maxAngle = Mathf.Lerp(
                34f, 5f, Mathf.Clamp01(KPH / 150f)
            );

            float angle = steer * maxAngle;
            float brakeTorque = brakeInput * spec.mass * 0.9f;

            for (int i = 0; i < 4; i++)
            {
                var w = wheels[i];

                if (i < 2)
                    w.steerAngle = angle;

                w.motorTorque = Drives(i) ? wheelNm : 0f;

                float bt = brakeTorque;

                if (hand && i >= 2)
                    bt = spec.mass * 3f;

                if (driveInput == 0f &&
                    brakeInput == 0f &&
                    !hand)
                    bt = spec.mass * 0.05f;

                w.brakeTorque = bt;
            }

            float rearSide = hand ? 0.55f : 1f;

            for (int i = 2; i < 4; i++)
            {
                var sd = wheels[i].sidewaysFriction;

                sd.stiffness = Mathf.Lerp(
                    sd.stiffness,
                    1.9f * gripMul * rearSide,
                    10f * Time.fixedDeltaTime
                );

                wheels[i].sidewaysFriction = sd;
            }

            AntiRoll(wheels[0], wheels[1]);
            AntiRoll(wheels[2], wheels[3]);

            float df =
                (spec.cat == CarCategory.Sports ||
                 spec.cat == CarCategory.Super)
                ? 0.45f : 0.2f;

            rb.AddForce(
                -transform.up * rb.velocity.sqrMagnitude * df
            );

            distanceM += Mathf.Abs(fwdSpeed) * Time.fixedDeltaTime;
        }

        void Shift()
        {
            if (AutoGear)
            {
                if (Time.time - lastShift > 0.35f)
                {
                    if (Rpm > 6500f &&
                        Gear < Ratios.Length - 1 &&
                        Throttle > 0.2f)
                    {
                        Gear++;
                        lastShift = Time.time;
                    }
                    else if (Rpm < 2300f && Gear > 0)
                    {
                        Gear--;
                        lastShift = Time.time;
                    }
                }
            }
            else if (isPlayer)
            {
                if (CarInput.shiftUp && Gear < Ratios.Length - 1)
                    Gear++;

                if (CarInput.shiftDown && Gear > 0)
                    Gear--;
            }

            if (isPlayer)
                CarInput.ClearPulses();
        }

        void AntiRoll(WheelCollider l, WheelCollider r)
        {
            float travelL = 1f, travelR = 1f;

            bool gl = l.GetGroundHit(out var hl);
            bool gr = r.GetGroundHit(out var hr);

            if (gl)
                travelL =
                    (-l.transform.InverseTransformPoint(hl.point).y -
                    l.radius) / l.suspensionDistance;

            if (gr)
                travelR =
                    (-r.transform.InverseTransformPoint(hr.point).y -
                    r.radius) / r.suspensionDistance;

            float force = (travelL - travelR) * spec.mass * 6f;

            if (gl)
                rb.AddForceAtPosition(
                    l.transform.up * -force,
                    l.transform.position
                );

            if (gr)
                rb.AddForceAtPosition(
                    r.transform.up * force,
                    r.transform.position
                );
        }

        public void Recover()
        {
            if (rb == null) return;

            transform.position += Vector3.up * 1.2f;

            transform.rotation = Quaternion.Euler(
                0,
                transform.eulerAngles.y,
                0
            );

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        public void Teleport(Vector3 pos, Quaternion rot)
        {
            if (rb == null)
            {
                transform.SetPositionAndRotation(pos, rot);
                return;
            }

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            transform.SetPositionAndRotation(pos, rot);

            rb.position = pos;
            rb.rotation = rot;

            foreach (var w in wheels)
            {
                w.brakeTorque = spec.mass;
                w.motorTorque = 0;
            }
        }

        public bool IsUpsideDown =>
            Vector3.Dot(transform.up, Vector3.up) < 0.25f;
    }
}
