using UnityEngine;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    public class LensData {
        public float focusDistance = 10f;
        public float aperture = 5.6f;
        public float shutterSpeed = 1f / 48f;
        public float iso = 3200f;
        public int blades = 5;
        public Vector2 bladeCurvature = new(2f, 11f);
        public float anamorphism = 0f;

        public void SetExposure(float targetExposure, Exposure.ExposureMode exposureMode) {
            float desiredColorMultiplier = Mathf.Pow(2f, -targetExposure);
            switch (exposureMode) {
                case Exposure.ExposureMode.OverrideISO:
                    iso = (120f * desiredColorMultiplier * (aperture * aperture)) / shutterSpeed;
                    break;
                case Exposure.ExposureMode.OverrideAperture:
                    aperture = Mathf.Abs((0.09128709f * Mathf.Sqrt(desiredColorMultiplier * shutterSpeed * iso)) /
                                         desiredColorMultiplier);
                    break;
                case Exposure.ExposureMode.OverrideShutterSpeed:
                    shutterSpeed = (120f * desiredColorMultiplier * (aperture * aperture)) / iso;
                    break;
            }
        }

        public void SetExposure(Exposure exposure) {
            SetExposure(exposure.desiredExposure.value, exposure.exposureMode.value);
        }

        public void Validate() {
            aperture = Mathf.Min(Mathf.Max(aperture, 0.7f), 32f);
            iso = Mathf.Max(iso, 50f);
            shutterSpeed = Mathf.Max(shutterSpeed, 0.002f);
            focusDistance = Mathf.Max(focusDistance, 0.01f);
            blades = Mathf.Min(Mathf.Max(blades, 3), 11);
            anamorphism = Mathf.Min(Mathf.Max(anamorphism, -1f), 1f);

            bladeCurvature.x = Mathf.Min(Mathf.Max(bladeCurvature.x, 0.7f), 32f);
            bladeCurvature.y = Mathf.Min(Mathf.Max(bladeCurvature.y, 0.7f), 32f);

            bladeCurvature.Set(Mathf.Min(bladeCurvature.x, bladeCurvature.y),
                Mathf.Max(bladeCurvature.x, bladeCurvature.y));
        }

        public float ExposureColorMultiplier =>
            1f / (1.2f * Mathf.Pow(2f, Mathf.Log(((aperture * aperture) / shutterSpeed) * (100f / iso), 2f)));

        public float CurrentCurvature => Mathf.Clamp01(Remap(aperture, bladeCurvature.x, bladeCurvature.y, 1f, 0f));

        private static float Remap(float value, float rangeMin, float rangeMax, float targetMin, float targetMax) {
            return targetMin + (value - rangeMin) * (targetMax - targetMin) / (rangeMax - rangeMin);
        }
    }
}