using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class ShutterCamera : MonoBehaviour {
        private static List<ShutterCamera> cameras = new();

        public static bool GetShutterCamera(Camera source, out ShutterCamera Out) {
            foreach (ShutterCamera camera in cameras) {
                if (camera.target == source) {
                    Out = camera;
                    return true;
                }
            }

            Out = null;
            return false;
        }

        private Camera target;
        private Transform cameraTransform;

        internal ShutterBasedTemporalRenderPass pass { get; set; }

        private void Awake() {
            target = GetComponent<Camera>();
            cameraTransform = target.transform;
        }

        private void OnEnable() {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            cameras.Add(this);
        }

        private void OnDisable() {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            cameras.Remove(this);
            if (pass != null) {
                pass.Dispose();
                pass = null;
            }
        }

        private float intensity;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector2 apertureJitter = Vector2.zero;
        private Vector2 pixelJitter = Vector2.zero;
        private Vector3 focusPoint = Vector3.forward;
        private int frameIndex;
        private LensData lens;
        private float dt0, dt1, dt2, dt3, dt4, dt5, dt6;
        private float normalizedAperture = 0.5f;
        private bool didJitter = false;

        private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam) {
            if (cam != target) {
                return;
            }

            cameraTransform = target.transform;
            originalPosition = cameraTransform.position;
            originalRotation = cameraTransform.rotation;
            Vector3 up = cameraTransform.up;
            didJitter = false;

            if (pass == null) return;

            const float t = 7f / 6f;
            float predictedDeltaTime = Mathf.LerpUnclamped(dt6, dt5, t);
            predictedDeltaTime = Mathf.LerpUnclamped(predictedDeltaTime, dt4, t);
            predictedDeltaTime = Mathf.LerpUnclamped(predictedDeltaTime, dt3, t);
            predictedDeltaTime = Mathf.LerpUnclamped(predictedDeltaTime, dt2, t);
            predictedDeltaTime = Mathf.LerpUnclamped(predictedDeltaTime, dt1, t);
            predictedDeltaTime = Mathf.LerpUnclamped(predictedDeltaTime, dt0, t);
            
            var stack = VolumeManager.instance.stack;
            PhysicalCamera physicalCamera = stack.GetComponent<PhysicalCamera>();
            Exposure exposure = stack.GetComponent<Exposure>();
            AdditionalSettings settings = stack.GetComponent<AdditionalSettings>();
            lens = physicalCamera.GetLensData(target);
            lens.Validate();
            lens.SetExposure(exposure);
            lens.Validate();

            float focalLength = target.usePhysicalProperties
                ? target.focalLength
                : Camera.FieldOfViewToFocalLength(target.fieldOfView, 24f);

            frameIndex = (frameIndex + 1) % 1024;
            intensity = Mathf.Clamp01(1f - (predictedDeltaTime / lens.shutterSpeed));
            normalizedAperture = Mathf.Clamp01(0.7f / lens.aperture);

            pass.ShutterInfo.x = intensity;
            pass.ShutterInfo.y = lens.ExposureColorMultiplier;
            pass.ShutterInfo.z = frameIndex % 64;
            pass.ShutterInfo.w = normalizedAperture;

            if (!(intensity > 0f)) return;
            
            if (settings.maxApertureJitterAllowed.value > 0f) {
                apertureJitter.x = Random.value * 2f * Mathf.PI;
                apertureJitter.y = Mathf.Abs(Random.value * 0.5f);
                apertureJitter.y *= intensity * intensity * settings.maxApertureJitterAllowed.value;
                apertureJitter.y *= (focalLength / lens.aperture) / 2f;
                apertureJitter.y /= 1000f;

                float bladeCount = lens.blades;
                float curvature = lens.CurrentCurvature;

                float nt = Mathf.Cos(Mathf.PI / bladeCount);
                float dt = Mathf.Cos(apertureJitter.x - ((2f * Mathf.PI) / bladeCount) *
                    Mathf.Floor((bladeCount * apertureJitter.x + Mathf.PI) / (2f * Mathf.PI)));
                float r = apertureJitter.y * Mathf.Pow(nt / dt, curvature);
                float u = r * Mathf.Cos(apertureJitter.x);
                float v = r * Mathf.Sin(apertureJitter.x);

                apertureJitter.x = u;
                apertureJitter.y = v;
                focusPoint = cameraTransform.position + (cameraTransform.forward * lens.focusDistance);
                // target.projectionMatrix *= Matrix4x4.Translate(pixelJitter);
                cameraTransform.Translate(apertureJitter, Space.Self);
                cameraTransform.LookAt(focusPoint, up);
                didJitter = true;
            }

            if (settings.maxPixelJitterAllowed.value > 0f) {
                pixelJitter.x = HaltonSequence.Get((frameIndex & 1023) + 1, 2) - 0.5f;
                pixelJitter.y = HaltonSequence.Get((frameIndex & 1023) + 1, 3) - 0.5f;
                pixelJitter.x *= 2f / target.scaledPixelWidth;
                pixelJitter.y *= 2f / target.scaledPixelHeight;
                pixelJitter *= settings.maxPixelJitterAllowed.value;
                pixelJitter *= intensity;
                cameraTransform.Translate(pixelJitter, Space.Self);
                didJitter = true;
            }
        }

        private void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam) {
            if (cam != target) {
                return;
            }

            dt6 = dt5;
            dt5 = dt4;
            dt4 = dt3;
            dt3 = dt2;
            dt2 = dt1;
            dt1 = dt0;
            dt0 = Time.unscaledDeltaTime;
            
            if (!didJitter) return;
            
            cameraTransform.position = originalPosition;
            cameraTransform.rotation = originalRotation;
            // target.ResetProjectionMatrix();
            // target.ResetWorldToCameraMatrix();
        }
    }
}