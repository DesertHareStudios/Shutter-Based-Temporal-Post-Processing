using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    internal class ShutterBasedRenderPass : ShutterBasedBaseRenderPass {
        private const string AnamorphicName = "_SBTPPAnamorphic";
        private static readonly int AnamorphicID = Shader.PropertyToID(AnamorphicName);

        private class PassData {
            public TextureHandle source;
            public TextureHandle anamorphic;
            public Material material;
            public Vector4 info;
        }

        protected override void OnRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var stack = VolumeManager.instance.stack;
            PhysicalCamera physicalCamera = stack.GetComponent<PhysicalCamera>();
            Exposure exposure = stack.GetComponent<Exposure>();
            AdditionalSettings settings = stack.GetComponent<AdditionalSettings>();
            var lens = physicalCamera.GetLensData(cameraData.camera);
            lens.Validate();
            lens.SetExposure(exposure);
            lens.Validate();

            ShutterInfo.x = 0f;
            ShutterInfo.y = lens.ExposureColorMultiplier;
            ShutterInfo.z = 0;
            ShutterInfo.w = Mathf.Clamp01(0.7f / lens.aperture);
            
            var desc = cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.mipCount = 0;
            // desc.sRGB = false;
            desc.depthStencilFormat = GraphicsFormat.None;
            desc.memoryless = RenderTextureMemoryless.None;
            desc.useMipMap = false;
            desc.autoGenerateMips = false;
            desc.enableRandomWrite = false;
            desc.bindMS = false;
            desc.useDynamicScale = false;

            if (!SystemInfo.IsFormatSupported(desc.graphicsFormat, GraphicsFormatUsage.Render)) {
                desc.graphicsFormat = GraphicsFormat.None;
                for (int i = 0; i < AccumulationFormatList.Length; i++)
                    if (SystemInfo.IsFormatSupported(AccumulationFormatList[i], GraphicsFormatUsage.Render)) {
                        desc.graphicsFormat = AccumulationFormatList[i];
                        break;
                    }
            }

            var target = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_SBTPPTarget", false,
                FilterMode.Point);
            
            desc.width /= 2;
            desc.height /= 2;
            
            var anamorphic = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, AnamorphicName, false,
                FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SBPP Downscale", out var passData)) {
                passData.source = resourceData.activeColorTexture;
                // passData.material = downscaleMaterial;
                // passData.texelSizes = sizes0;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(anamorphic, 0);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                    ExecuteCopyPass(data, context));
            }

            using (var builder =
                   renderGraph.AddRasterRenderPass<PassData>("Shutter Based Post-Processing",
                       out var passData)) {
                passData.source = resourceData.activeColorTexture;
                passData.anamorphic = anamorphic;
                passData.material = material;
                passData.info = ShutterInfo;
                builder.UseTexture(passData.source);
                builder.UseTexture(passData.anamorphic);
                builder.SetRenderAttachment(target, 0);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SBPP Output", out var passData)) {
                passData.source = target;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                    ExecuteCopyPass(data, context));
            }
        }

        static void ExecutePass(PassData data, RasterGraphContext context) {
            Shader.SetGlobalVector(ShutterInfoID, data.info);
            Shader.SetGlobalTexture(AnamorphicID, data.anamorphic);
            Blitter.BlitTexture(context.cmd, data.source, Vector4.one, data.material, 0);
        }
    }
}