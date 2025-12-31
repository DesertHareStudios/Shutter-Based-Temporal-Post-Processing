using UnityEngine;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    public class LensData {
        
        public float shutterSpeed = 1f / 30f;
        public float focusDistance = 10f;
        public float aperture = 5.6f;
        public int blades = 5;
        public Vector2 bladeCurvature = new(2f, 11f);

        public LensData Validate() {
            aperture = Mathf.Min(Mathf.Max(aperture, 0.7f), 32f);
            focusDistance = Mathf.Max(focusDistance, 0.01f);
            blades = Mathf.Min(Mathf.Max(blades, 3), 11);
            
            bladeCurvature.x = Mathf.Min(Mathf.Max(bladeCurvature.x, 0.7f), 32f);
            bladeCurvature.y = Mathf.Min(Mathf.Max(bladeCurvature.y, 0.7f), 32f);
            
            bladeCurvature.Set(Mathf.Min(bladeCurvature.x, bladeCurvature.y),
                Mathf.Max(bladeCurvature.x, bladeCurvature.y));
            return this;
        }

        public float CurrentCurvature => Mathf.Clamp01(Remap(aperture, bladeCurvature.x, bladeCurvature.y, 1f, 0f));

        private static float Remap(float value, float rangeMin, float rangeMax, float targetMin, float targetMax) {
            return targetMin + (value - rangeMin) * (targetMax - targetMin) / (rangeMax - rangeMin);
        }
    }
}