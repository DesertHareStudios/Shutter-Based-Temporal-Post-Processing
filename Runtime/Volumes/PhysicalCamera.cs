using UnityEngine;
using UnityEngine.Rendering;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    [VolumeComponentMenu("Shutter Based Temporal Post-Processing/Physical Camera")]
    public class PhysicalCamera : VolumeComponent {
        public enum DataSource {
            [InspectorName("Use this volume")] Volume = 0,
            [InspectorName("Use physical camera settings if present (Falls back to this volume)")]
            PhysicalCamera = 1
        }

        public enum Downscale {
            Half = 2,
            Third = 3,
            Quarter = 4
        }

        [Header("Camera Body")] public EnumParameter<DataSource> cameraBodySource = new(DataSource.PhysicalCamera);
        public ClampedFloatParameter shutterAngle = new(0.5f, 0f, 1f);

        [Header("Lens")] public EnumParameter<DataSource> lensSource = new(DataSource.PhysicalCamera);
        public MinFloatParameter focusDistance = new(333.333f, 0.1f);
        public ClampedFloatParameter aperture = new(3.2f, 0.7f, 32f);
        public EnumParameter<Downscale> depthOfFieldResolution = new(Downscale.Half);

        // [Header("Aperture Shape")]
        // public EnumParameter<DataSource> apertureShapeSource = new(DataSource.PhysicalCamera);
        //
        // public ClampedIntParameter blades = new(5, 3, 11);
        // public ClampedFloatParameter minBladeCurvature = new(2.1f, 0.7f, 32f);
        // public ClampedFloatParameter maxBladeCurvature = new(8.3f, 0.7f, 32f);

        public float ShutterSpeed {
            get {
                var rate = Screen.currentResolution.refreshRateRatio;
                var multiplier = Mathf.Lerp(1f, 5f, shutterAngle.value * shutterAngle.value);
                if (QualitySettings.vSyncCount > 0) {
                    return ((float)rate.denominator * multiplier) /
                           ((float)rate.numerator * (float)QualitySettings.vSyncCount);
                }

                if (Application.targetFrameRate > 0) {
                    // return multiplier / (float)Application.targetFrameRate;
                    return multiplier / Mathf.Min((float)Application.targetFrameRate,
                        (float)rate.numerator / (float)rate.denominator);
                }

                return ((float)rate.denominator * multiplier) / (float)rate.numerator;
            }
        }

        public LensData GetLensData(Camera source) {
            return new LensData {
                focusDistance = (source.usePhysicalProperties && lensSource.value == DataSource.PhysicalCamera)
                    ? source.focusDistance
                    : focusDistance.value,
                aperture = (source.usePhysicalProperties && lensSource.value == DataSource.PhysicalCamera)
                    ? source.aperture
                    : aperture.value,
                // blades = (source.usePhysicalProperties && apertureShapeSource.value == DataSource.PhysicalCamera)
                //     ? source.bladeCount
                //     : blades.value,
                // bladeCurvature =
                //     (source.usePhysicalProperties && apertureShapeSource.value == DataSource.PhysicalCamera)
                //         ? source.curvature
                //         : new Vector2(minBladeCurvature.value, maxBladeCurvature.value),
                shutterSpeed = (source.usePhysicalProperties && cameraBodySource.value == DataSource.PhysicalCamera)
                    ? source.shutterSpeed
                    : ShutterSpeed
            };
        }
    }
}