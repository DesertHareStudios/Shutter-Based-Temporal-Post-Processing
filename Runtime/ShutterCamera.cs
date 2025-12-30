using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        public bool controlTemporalAntiAliasingSettings = true;
        
        private Camera target;
        private UniversalAdditionalCameraData cameraData;

        internal ShutterBasedTemporalRenderPass pass { get; set; }

        private void Awake() {
            target = GetComponent<Camera>();
            cameraData = target.GetUniversalAdditionalCameraData();
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
        private int frameIndex;
        private LensData lens;
        private float dt0, dt1, dt2, dt3, dt4, dt5, dt6;
        private float normalizedAperture = 0.5f;

        private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam) {
            if (cam != target) {
                return;
            }

            if (pass == null) return;

            const float t = 7f / 6f;
            float predictedDeltaTime = Mathf.LerpUnclamped(dt6, dt5, t);
            predictedDeltaTime = Mathf.LerpUnclamped(predictedDeltaTime, dt4, t);
            predictedDeltaTime = Mathf.LerpUnclamped(predictedDeltaTime, dt3, t);
            predictedDeltaTime = Mathf.LerpUnclamped(predictedDeltaTime, dt2, t);
            predictedDeltaTime = Mathf.LerpUnclamped(predictedDeltaTime, dt1, t);
            predictedDeltaTime = Mathf.LerpUnclamped(predictedDeltaTime, dt0, t);

            var stack = VolumeManager.instance.stack;
            var physicalSettings = stack.GetComponent<PhysicalCamera>();
            lens = physicalSettings.GetLensData(target).Validate();

            pass.dofResolutionDownscaler = (int)physicalSettings.depthOfFieldResolution.value;

            // float focalLength = target.usePhysicalProperties
            //     ? target.focalLength
            //     : Camera.FieldOfViewToFocalLength(target.fieldOfView, 24f);

            frameIndex = (frameIndex + 1) % 1024;
            intensity = Mathf.Clamp01(1f - (predictedDeltaTime / lens.shutterSpeed));
            normalizedAperture = Mathf.Clamp01(0.7f / lens.aperture);

            pass.ShutterInfo.x = intensity;
            pass.ShutterInfo.y = lens.focusDistance;
            pass.ShutterInfo.z = frameIndex % 64;
            pass.ShutterInfo.w = normalizedAperture;

            if (controlTemporalAntiAliasingSettings) {
                cameraData.taaSettings.baseBlendFactor = Mathf.LerpUnclamped(0.6f, 0.98f, intensity);
                cameraData.taaSettings.jitterScale = 1f - (normalizedAperture * intensity);
                cameraData.taaSettings.jitterScale *= cameraData.taaSettings.jitterScale;
                cameraData.taaSettings.jitterScale = 1f - cameraData.taaSettings.jitterScale;
                cameraData.taaSettings.contrastAdaptiveSharpening = 1f - normalizedAperture;
                float shutterMS = lens.shutterSpeed * 100f;
                cameraData.taaSettings.varianceClampScale = Mathf.LerpUnclamped(0.6f, 1.2f, shutterMS / (shutterMS + 1f));
            }

            // if (intensity <= 0f) return;
            //
            // apertureJitter.y = Mathf.Abs(Random.value * 0.5f);
            // apertureJitter.y *= intensity * intensity;
            // apertureJitter.y *= (focalLength / lens.aperture) / 2f;
            // apertureJitter.y /= 1000f;
            //
            // if (apertureJitter.y <= 0f) return;
            //
            // apertureJitter.x = Random.value * 2f * Mathf.PI;
            //
            // float bladeCount = lens.blades;
            // float curvature = lens.CurrentCurvature;
            //
            // float nt = Mathf.Cos(Mathf.PI / bladeCount);
            // float dt = Mathf.Cos(apertureJitter.x - ((2f * Mathf.PI) / bladeCount) *
            //     Mathf.Floor((bladeCount * apertureJitter.x + Mathf.PI) / (2f * Mathf.PI)));
            // float r = apertureJitter.y * Mathf.Pow(nt / dt, curvature);
            // float u = r * Mathf.Cos(apertureJitter.x);
            // float v = r * Mathf.Sin(apertureJitter.x);
            //
            // apertureJitter.x = u;
            // apertureJitter.y = v;
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
        }
    }
}