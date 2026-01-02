using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing
{
    [SupportedOnRenderer(typeof(UniversalRendererData))]
    [DisallowMultipleRendererFeature("Shutter Based Temporal Post-Processing")]
    [Tooltip("Simulates physical camera attributes on top of a custom TAA solution.")]
    public class ShutterBasedTemporalPostProcessing : ScriptableRendererFeature
    {
        [SerializeField] private ShutterBasedTemporalPostProcessingData data;
        
#if UNITY_EDITOR
        [Header("Debug")]
        public bool viewCircleOfConfussion = false;
#endif

        /// <inheritdoc/>
        public override void Create() { }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (!renderingData.postProcessingEnabled) return;
            if (!renderingData.cameraData.postProcessEnabled) return;

            if (!ShutterCamera.GetShutterCamera(renderingData.cameraData.camera, out ShutterCamera shutter)) return;

            shutter.pass ??= new(data);
            
            #if UNITY_EDITOR
            shutter.pass.debugCoC = viewCircleOfConfussion;
            #endif
            
            renderer.EnqueuePass(shutter.pass);
        }
    }
}