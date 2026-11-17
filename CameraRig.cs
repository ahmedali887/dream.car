
using UnityEngine;
using UnityEngine.EventSystems;

namespace UCAS
{
    public enum CamMode { Chase, Hood, Far, Orbit }

    /// <summary>
    /// One camera that follows the player car
    /// or orbits a display car.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        public CamMode mode = CamMode.Chase;

        public float orbitYaw = 25f;
        public float orbitPitch = 12f;
        public float orbitDist = 7f;

        public bool orbitAuto = true;

        Vector3 vel;
        int dragFinger = -1;
        Vector2 lastPos;

        public void CycleView()
        {
            if (mode == CamMode.Orbit) return;

            mode = mode == CamMode.Chase
                ? CamMode.Hood
                : mode == CamMode.Hood
                ? CamMode.Far
                : CamMode.Chase;
        }

        public void SnapBehind()
        {
            if (target == null) return;

            transform.position =
                target.position -
                target.forward * 7f +
                Vector3.up * 3f;

            transform.rotation =
                Quaternion.LookRotation(
                    target.position - transform.position
                );
        }

        void LateUpdate()
        {
            if (target == null) return;

            if (mode == CamMode.Orbit)
            {
                DoOrbit();
                return;
            }

            Vector3 fwd = target.forward;
            fwd.y = 0;
            fwd.Normalize();

            Vector3 desiredPos;
            Quaternion desiredRot;

            switch (mode)
            {
                case CamMode.Hood:
                    desiredPos =
                        target.position +
                        target.up * 1.15f +
                        target.forward * 0.9f;

                    desiredRot =
                        Quaternion.LookRotation(
                            target.forward,
                            Vector3.up
                        );

                    transform.position = desiredPos;

                    transform.rotation =
                        Quaternion.Slerp(
                            transform.rotation,
                            desiredRot,
                            14f * Time.deltaTime
                        );
                    return;

                case CamMode.Far:
                    desiredPos =
                        target.position -
                        fwd * 13f +
                        Vector3.up * 6.5f;
                    break;

                default:
                    desiredPos =
                        target.position -
                        fwd * 6.2f +
                        Vector3.up * 2.4f;
                    break;
            }

            transform.position =
                Vector3.SmoothDamp(
                    transform.position,
                    desiredPos,
                    ref vel,
                    0.12f
                );

            Vector3 look =
                target.position +
                Vector3.up * 1.2f +
                fwd * 3f;

            desiredRot =
                Quaternion.LookRotation(
                    look - transform.position,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    desiredRot,
                    10f * Time.deltaTime
                );
        }

        void DoOrbit()
        {
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);

                if (
                    t.phase == TouchPhase.Began &&
                    !EventSystem.current.IsPointerOverGameObject(
                        t.fingerId
                    )
                )
                {
                    dragFinger = t.fingerId;
                }

                if (
                    t.fingerId == dragFinger &&
                    t.phase == TouchPhase.Moved
                )
                {
                    orbitYaw += t.deltaPosition.x * 0.3f;

                    orbitPitch = Mathf.Clamp(
                        orbitPitch - t.deltaPosition.y * 0.15f,
                        2f,
                        55f
                    );

                    orbitAuto = false;
                }

                if (
                    t.phase == TouchPhase.Ended ||
                    t.phase == TouchPhase.Canceled
                )
                {
                    dragFinger = -1;
                }
            }
            else if (
                Input.GetMouseButton(0) &&
                !EventSystem.current.IsPointerOverGameObject()
            )
            {
                Vector2 d =
                    (Vector2)Input.mousePosition - lastPos;

                if (Input.GetMouseButtonDown(0))
                    d = Vector2.zero;

                orbitYaw += d.x * 0.3f;

                orbitPitch = Mathf.Clamp(
                    orbitPitch - d.y * 0.15f,
                    2f,
                    55f
                );

                orbitAuto = false;
            }

            lastPos = Input.mousePosition;

            if (orbitAuto)
                orbitYaw += 14f * Time.deltaTime;

            Vector3 center =
                target.position + Vector3.up * 0.8f;

            var rot =
                Quaternion.Euler(
                    orbitPitch,
                    orbitYaw,
                    0
                );

            transform.position =
                center + rot * new Vector3(0, 0, -orbitDist);

            transform.rotation =
                Quaternion.LookRotation(
                    center - transform.position,
                    Vector3.up
                );
        }
    }
}
