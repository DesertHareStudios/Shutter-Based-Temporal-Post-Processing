using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    internal class ShutterBasedTemporalRenderPass : ShutterBasedBaseRenderPass {
        private const string LENSFLARES = "SBTPP_LENS_FLARES";

        private const string TargetHandle = "_SBTPPTarget";
        
        private const string DownscaledName = "_SBTPPDown";
        private static readonly int DownscaledID = Shader.PropertyToID(DownscaledName);

        private const string HistoryName = "_SBTPPHistory";
        private static readonly int HistoryID = Shader.PropertyToID(HistoryName);

        private const string DownscaledHistoryName = "_SBTPPDownHistory";
        private static readonly int DownscaledHistoryID = Shader.PropertyToID(DownscaledHistoryName);

        private RTHandle history = RTHandles.Alloc(HistoryID, HistoryName);

        public void Dispose() {
            history.Release();
        }

        private class PassData {
            public TextureHandle source;
            public TextureHandle downscaled;
            public TextureHandle history;
            public TextureHandle historyDownscaled;
            public Material material;
            public Vector4 info;
            public bool lensFlares;
        }

        protected override void OnRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>()
                ;
            var stack = VolumeManager.instance.stack;
            // PhysicalCamera physicalCamera = stack.GetComponent<PhysicalCamera>();
            // Exposure exposure = stack.GetComponent<Exposure>();
            AdditionalSettings settings = stack.GetComponent<AdditionalSettings>();

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

            var targetHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, TargetHandle, false,
                FilterMode.Bilinear);

            RenderingUtils.ReAllocateHandleIfNeeded(ref history, desc, FilterMode.Bilinear, TextureWrapMode.Clamp,
                name: HistoryName);

            var historyHandle = renderGraph.ImportTexture(history);

            desc.width /= 2;
            desc.height /= 2;

            var downscaledHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, DownscaledName, false,
                FilterMode.Bilinear);

            var downscaledHistoryHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc,
                DownscaledHistoryName, false,
                FilterMode.Bilinear);

            using (var builder =
                   renderGraph.AddRasterRenderPass<CopyPassData>("SBTPP Downscale", out var passData)) {
                passData.source = resourceData.activeColorTexture;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(downscaledHandle, 0);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                ExecuteCopyPass(data, context));
            }

            using (var builder =
                   renderGraph.AddRasterRenderPass<CopyPassData>("SBTPP History Downscale",
                       out var passData)) {
                passData.source = historyHandle;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(downscaledHistoryHandle, 0);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                    ExecuteCopyPass(data, context));
            }

            using (var builder =
                   renderGraph.AddRasterRenderPass<PassData>("Shutter Based Temporal Post-Processing",
                       out var passData)) {
                passData.source = resourceData.activeColorTexture;
                passData.downscaled = downscaledHandle;
                passData.history = historyHandle;
                passData.historyDownscaled = downscaledHistoryHandle;
                passData.material = material;
                passData.info = ShutterInfo;
                passData.lensFlares = settings.enableLensFlares.value;
                builder.UseTexture(passData.source);
                builder.UseTexture(passData.downscaled);
                builder.UseTexture(passData.history);
                builder.UseTexture(passData.historyDownscaled);
                builder.SetRenderAttachment(targetHandle, 0);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    ExecutePass(data, context));
            }

            using (var builder =
                   renderGraph.AddRasterRenderPass<CopyPassData>("SBTPP Copy History",
                       out var passData)) {
                passData.source = targetHandle;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(historyHandle, 0);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                    ExecuteCopyPass(data, context));
            }

            using (var builder =
                   renderGraph.AddRasterRenderPass<CopyPassData>("SBTPP Output",
                       out var passData)) {
                passData.source = targetHandle;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                    ExecuteCopyPass(data, context));
            }
        }

        static void ExecutePass(PassData data, RasterGraphContext context) {
            Shader.SetGlobalTexture(HistoryID, data.history);
            Shader.SetGlobalTexture(DownscaledID, data.downscaled);
            Shader.SetGlobalTexture(DownscaledHistoryID, data.historyDownscaled);
            Shader.SetGlobalVector(ShutterInfoID, data.info);
            if (data.lensFlares) {
                Shader.EnableKeyword(LENSFLARES);
            }
            else {
                Shader.DisableKeyword(LENSFLARES);
            }

            Blitter.BlitTexture(context.cmd, data.source, Vector4.one, data.material, 0);
        }
    }
}