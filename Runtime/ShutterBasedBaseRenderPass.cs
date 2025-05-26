using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    
    internal class ShutterBasedBaseRenderPass : ScriptableRenderPass {
        
        protected static readonly GraphicsFormat[] AccumulationFormatList = {
            GraphicsFormat.R16G16B16A16_SFloat,
            GraphicsFormat.B10G11R11_UFloatPack32,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.B8G8R8A8_UNorm,
        };

        protected static readonly int ShutterInfoID = Shader.PropertyToID("_ShutterInfo");

        //Intensity, Color Multiplier, FrameIndex, Scattering
        public Vector4 ShutterInfo = Vector4.zero;

        public Material material;
        
        protected class CopyPassData {
            public TextureHandle source;
        }

        protected static void ExecuteCopyPass(CopyPassData data, RasterGraphContext context) {
            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, 0f, true);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            OnRenderGraph(renderGraph, frameData);
        }

        protected virtual void OnRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {}


    }
}