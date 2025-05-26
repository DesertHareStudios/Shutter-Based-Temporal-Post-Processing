using UnityEngine.Rendering;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    
    [VolumeComponentMenu("Shutter Based Temporal Post-Processing/Additional Settings")]
    public class AdditionalSettings : VolumeComponent{
        
        public ClampedFloatParameter maxApertureJitterAllowed = new(1f, 0f, 1f);
        public ClampedFloatParameter maxPixelJitterAllowed = new(1f, 0f, 1f);
        public BoolParameter enableLensFlares = new(true);

    }
}