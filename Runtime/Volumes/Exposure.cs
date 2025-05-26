using UnityEngine.Rendering;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    
    [VolumeComponentMenu("Shutter Based Temporal Post-Processing/Exposure")]
    public class Exposure : VolumeComponent{
        
        public ClampedFloatParameter desiredExposure = new(0f, -10f, 10f);
        public EnumParameter<ExposureMode> exposureMode = new(ExposureMode.OverrideISO);
        
        public enum ExposureMode {
            DoNothing = 0,
            OverrideISO = 1,
            OverrideAperture = 2,
            OverrideShutterSpeed = 3
        }
        
    }
}