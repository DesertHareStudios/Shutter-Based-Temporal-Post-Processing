using UnityEngine;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    [CreateAssetMenu(fileName = "SBTPP Data", menuName = "Shutter Based Temporal Post-Processing/Data")]
    public class ShutterBasedTemporalPostProcessingData : ScriptableObject {

        public Shader prepassShader;
        public Shader sbtppShader;

    }
}