using UnityEngine;
using Game.Interfaces;
using Sources.Code.Interfaces;

namespace Sources.Controllers
{
    public class CameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform bodyTransform;
        [SerializeField] private Camera cam;
        [SerializeField] private SineMotion sineMotion;

        [Header("Follow")]
        [SerializeField] private Vector3 offset = new(0f, 0.2f, 0f);
        [SerializeField] private float amplitude = 0.05f;
        [SerializeField] private float frequency = 7f;

        [Header("Rotation")]
        [SerializeField] private float mouseSensitivity   = 2f;
        [SerializeField] private float maxLookUp          = 80f;
        [SerializeField] private float minLookDown        = -80f;
        [SerializeField] private float rotationSmoothTime = 0.05f;

        private IInputManager input;
        private ICameraInputProvider inputProvider;

        private CameraFollow   follow;
        private CameraRotation rotation;

        private bool  shakeEnabled;
        private float shakeIntensity;
        private float shakeTime;

        private float   baseFov;
        private Vector3 baseLocalPos;

        // Для сейва
        public Camera   Camera        => cam;
        public Transform BodyTransform => bodyTransform;

        public float GetYaw()   => rotation != null ? rotation.CurrentYaw   : bodyTransform.eulerAngles.y;
        public float GetPitch() => rotation != null ? rotation.CurrentPitch : cam.transform.localEulerAngles.x;

        public void Construct(IInputManager input)
        {
            this.input = input;
            inputProvider = new MouseInputProvider(input);

            baseFov      = cam.fieldOfView;
            baseLocalPos = cam.transform.localPosition;

            follow = new CameraFollow(
                headBone,
                offset,
                amplitude,
                frequency
            );
            follow.SetInputProvider(inputProvider);

            rotation = new CameraRotation(
                mouseSensitivity,
                maxLookUp,
                minLookDown,
                rotationSmoothTime
            );
            rotation.Init(cam.transform, bodyTransform);
            rotation.SetInputProvider(inputProvider);

            cam.enabled = true;
        }

        public void Apply(CameraSettings settings)
        {
            if (!settings.overrideCamera)
                return;

            rotation.SetRotationBlocked(settings.blockRotation);
            rotation.SetSensitivityMultiplier(settings.sensitivityMultiplier);

            if (settings.overrideFov)
                cam.fieldOfView = settings.fov;

            shakeEnabled  = settings.cameraShake;
            shakeIntensity = settings.shakeIntensity;
        }

        public void Restore()
        {
            rotation.SetRotationBlocked(false);
            rotation.ResetSensitivity();

            cam.fieldOfView            = baseFov;
            cam.transform.localPosition = baseLocalPos;

            shakeEnabled  = false;
            shakeIntensity = 0f;
            shakeTime      = 0f;
        }

        private void LateUpdate()
        {
            if (input.IsLocked)
                return;

            if (follow != null)
                follow.UpdateCameraPosition(cam.transform);

            if (rotation != null)
                rotation.UpdateRotation(cam.transform, bodyTransform);

            ApplyShake();
        }

        private void ApplyShake()
        {
            if (!shakeEnabled || sineMotion == null)
                return;

            shakeTime += Time.deltaTime;

            float shake =
                sineMotion.GetSine(shakeTime, 25f) * shakeIntensity * 0.02f;

            cam.transform.localPosition =
                baseLocalPos + Vector3.up * shake;
        }

        public void SetRotationFromSave(float yaw, float pitch)
        {
            bodyTransform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            rotation.ForceSetAngles(yaw, pitch);
        }
    }
}
