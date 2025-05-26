using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    
    [SupportedOnRenderer(typeof(UniversalRendererData))]
    [DisallowMultipleRendererFeature("Shutter Based Temporal Post-Processing")]
    [Tooltip("Simulates physical camera attributes on top of a custom TAA solution.")]
    public class ShutterBasedTemporalPostProcessing : ScriptableRendererFeature {

        [SerializeField] private ShutterBasedTemporalPostProcessingData data;

        private ShutterBasedRenderPass pass;

        /// <inheritdoc/>
        public override void Create() {
            pass = new ShutterBasedRenderPass {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents,
                material = data.exposureOnlyMaterial
            };
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            pass = null;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (!renderingData.postProcessingEnabled) return;
            if (!renderingData.cameraData.postProcessEnabled) return;

            if (ShutterCamera.GetShutterCamera(renderingData.cameraData.camera, out ShutterCamera shutter)) {
                shutter.pass ??= new ShutterBasedTemporalRenderPass() {
                    renderPassEvent = RenderPassEvent.AfterRenderingTransparents,
                    material = data.sbtppMaterial
                };
                renderer.EnqueuePass(shutter.pass);
            }
            else {
                renderer.EnqueuePass(pass);
            }
        }
    }
}