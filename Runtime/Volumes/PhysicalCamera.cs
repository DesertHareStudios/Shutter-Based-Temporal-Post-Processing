using UnityEngine;
using UnityEngine.Rendering;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    [VolumeComponentMenu("Shutter Based Temporal Post-Processing/Physical Camera")]
    public class PhysicalCamera : VolumeComponent {
        [Header("Camera Body")]
        public EnumParameter<DataSource> cameraBodySource = new(DataSource.PhysicalCamera);
        [InspectorName("ISO")]
        public MinFloatParameter iso = new(4000f, 50f);
        public ClampedFloatParameter shutterSpeed = new(1f / 48f, 1f / 576f, 1f / 7.5f);

        [Header("Lens")]
        public EnumParameter<DataSource> lensSource = new(DataSource.PhysicalCamera);
        public MinFloatParameter focusDistance = new(333.333f, 0.1f);
        public ClampedFloatParameter aperture = new(3.2f, 0.7f, 32f);

        [Header("Aperture Shape")]
        public EnumParameter<DataSource> apertureShapeSource = new(DataSource.PhysicalCamera);

        public ClampedIntParameter blades = new(5, 3, 11);
        public ClampedFloatParameter minBladeCurvature = new(2.1f, 0.7f, 32f);
        public ClampedFloatParameter maxBladeCurvature = new(8.3f, 0.7f, 32f);

        public LensData GetLensData(Camera source) {
            return new LensData {
                focusDistance = (source.usePhysicalProperties && lensSource.value == DataSource.PhysicalCamera)
                    ? source.focusDistance
                    : focusDistance.value,
                aperture = (source.usePhysicalProperties && lensSource.value == DataSource.PhysicalCamera)
                    ? source.aperture
                    : aperture.value,
                shutterSpeed = (source.usePhysicalProperties && cameraBodySource.value == DataSource.PhysicalCamera)
                    ? source.shutterSpeed
                    : shutterSpeed.value,
                iso = (source.usePhysicalProperties && cameraBodySource.value == DataSource.PhysicalCamera)
                    ? source.iso
                    : iso.value,
                blades = (source.usePhysicalProperties && apertureShapeSource.value == DataSource.PhysicalCamera)
                    ? source.bladeCount
                    : blades.value,
                bladeCurvature = (source.usePhysicalProperties && apertureShapeSource.value == DataSource.PhysicalCamera)
                    ? source.curvature
                    : new Vector2(minBladeCurvature.value, maxBladeCurvature.value)
            };
        }

        public enum DataSource {
            [InspectorName("Use this volume")] Volume = 0,

            [InspectorName("Use physical camera settings if present (Falls back to this volume)")]
            PhysicalCamera = 1
        }
    }
}