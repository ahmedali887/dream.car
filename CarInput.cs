
using UnityEngine;

namespace UCAS
{
    /// <summary>
    /// Single place the car reads input from.
    /// Touch UI writes the touch fields.
    /// Keyboard/gamepad is polled in Poll().
    /// </summary>
    public static class CarInput
    {
        public static float touchThrottle, touchBrake, touchSteer;
        public static bool touchHandbrake;
        public static bool shiftUp, shiftDown;

        static float steerSmooth;

        public static float Throttle, Brake, Steer;
        public static bool Handbrake;

        public static void Poll()
        {
            float kThr = Mathf.Max(
                Input.GetKey(KeyCode.W) ? 1 : 0,
                Input.GetKey(KeyCode.UpArrow) ? 1 : 0
            );

            float kBrk = Mathf.Max(
                Input.GetKey(KeyCode.S) ? 1 : 0,
                Input.GetKey(KeyCode.DownArrow) ? 1 : 0
            );

            float kSt =
                (Input.GetKey(KeyCode.D) ||
                 Input.GetKey(KeyCode.RightArrow) ? 1 : 0) -
                (Input.GetKey(KeyCode.A) ||
                 Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);

            Throttle = Mathf.Max(kThr, touchThrottle);
            Brake = Mathf.Max(kBrk, touchBrake);

            Handbrake =
                Input.GetKey(KeyCode.Space) ||
                touchHandbrake;

            float target =
                Mathf.Abs(touchSteer) > 0.01f
                ? touchSteer
                : kSt;

            steerSmooth = Mathf.MoveTowards(
                steerSmooth,
                target,
                Time.deltaTime *
                (Mathf.Abs(touchSteer) > 0.01f ? 8f : 3.5f)
            );

            Steer = steerSmooth;

            if (Input.GetKeyDown(KeyCode.E))
                shiftUp = true;

            if (Input.GetKeyDown(KeyCode.Q))
                shiftDown = true;
        }

        public static void ClearPulses()
        {
            shiftUp = shiftDown = false;
        }

        public static void Reset()
        {
            touchThrottle = touchBrake = touchSteer = 0;
            touchHandbrake = false;

            Throttle = Brake = Steer = 0;
            Handbrake = false;

            steerSmooth = 0;
        }
    }
}
