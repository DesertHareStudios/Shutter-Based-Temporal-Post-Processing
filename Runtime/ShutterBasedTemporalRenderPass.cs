using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace DesertHareStudios.ShutterBasedTemporalPostProcessing {
    internal class ShutterBasedTemporalRenderPass : ScriptableRenderPass {
        private const string AccumulationName = "_SBTPPAccum";
        private static readonly int AccumulationID = Shader.PropertyToID(AccumulationName);

        private const string CoCName = "_SBTPPCoC";
        private static readonly int CoCID = Shader.PropertyToID(CoCName);

        private const string ShutterInfoName = "_ShutterInfo";
        private static readonly int ShutterInfoID = Shader.PropertyToID(ShutterInfoName);

        private const string ShutterScreenInfoName = "_ShutterScreenInfo";
        private static readonly int ShutterScreenInfoID = Shader.PropertyToID(ShutterScreenInfoName);

        private static readonly GraphicsFormat[] AccumulationFormatList = {
            GraphicsFormat.R16G16B16A16_SFloat,
            GraphicsFormat.B10G11R11_UFloatPack32,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.B8G8R8A8_UNorm
        };

        private static readonly GraphicsFormat[] AccumulationFormatListSDR = {
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.B8G8R8A8_UNorm
        };

        private static readonly GraphicsFormat[] AccumulationFormatListAlpha = {
            GraphicsFormat.R16G16B16A16_SFloat,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.B8G8R8A8_UNorm
        };

        private static readonly GraphicsFormat[] AccumulationFormatListAlphaSDR = {
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.B8G8R8A8_UNorm
        };

        private RTHandle accumulation = RTHandles.Alloc(AccumulationID, AccumulationName);

        //Intensity, Focus Distance, FrameIndex, Scattering
        public Vector4 ShutterInfo = Vector4.zero;

        public int dofResolutionDownscaler = 2;

        //uvscale.x, uvscale.y, texelsize.x, texelsize.y
        private Vector4 ShutterScreenInfo = Vector4.one;
        public Material material;
        public Material preMaterial;

        public ShutterBasedTemporalRenderPass(ShutterBasedTemporalPostProcessingData data) {
            ConfigureInput(ScriptableRenderPassInput.Color);
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            preMaterial = new Material(data.prepassShader);
            material = new Material(data.sbtppShader);
        }

        public void Dispose() {
            accumulation.Release();
        }

        protected class CopyPassData {
            public TextureHandle source;
            public TextureHandle target;
        }

        private class PassData {
            public TextureHandle source;
            public TextureHandle coc;
            public Material material;
        }


        protected static void ExecuteCopyPass(CopyPassData data, RasterGraphContext context) {
            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, 0f, true);
        }

        private static void AddCopyPass(RenderGraph renderGraph, TextureHandle from,
            TextureHandle to,
            string name = "Copy") {
            using var builder = renderGraph.AddRasterRenderPass<CopyPassData>(name, out var passData);
            passData.source = from;
            passData.target = to;
            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(passData.target, 0);
            builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                ExecuteCopyPass(data, context));
        }

        static void ExecutePass(PassData data, RasterGraphContext context) {
            data.material.SetTexture(CoCID, data.coc);
            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, data.material, 0);
        }

        private static void AddPass(RenderGraph renderGraph, Material material,
            TextureHandle from, TextureHandle to, TextureHandle coc, string name = "Pass") {
            using var builder = renderGraph.AddRasterRenderPass<PassData>(name, out var passData);
            passData.source = from;
            passData.coc = coc;
            passData.material = material;
            builder.UseTexture(passData.source);
            builder.UseTexture(passData.coc);
            builder.SetRenderAttachment(to, 0);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                ExecutePass(data, context));
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

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

                if (cameraData.isHdrEnabled) {
                    foreach (var t in AccumulationFormatList)
                        if (SystemInfo.IsFormatSupported(t, GraphicsFormatUsage.Render)) {
                            desc.graphicsFormat = t;
                            break;
                        }
                }
                else {
                    foreach (var t in AccumulationFormatListSDR)
                        if (SystemInfo.IsFormatSupported(t, GraphicsFormatUsage.Render)) {
                            desc.graphicsFormat = t;
                            break;
                        }
                }
            }

            RenderingUtils.ReAllocateHandleIfNeeded(ref accumulation, desc, FilterMode.Bilinear, TextureWrapMode.Mirror,
                name: AccumulationName);

            var accumulationHandle = renderGraph.ImportTexture(accumulation);

            desc.graphicsFormat = GraphicsFormat.None;
            if (cameraData.isHdrEnabled) {
                foreach (var t in AccumulationFormatListAlpha)
                    if (SystemInfo.IsFormatSupported(t, GraphicsFormatUsage.Render)) {
                        desc.graphicsFormat = t;
                        break;
                    }
            }
            else {
                foreach (var t in AccumulationFormatListAlphaSDR)
                    if (SystemInfo.IsFormatSupported(t, GraphicsFormatUsage.Render)) {
                        desc.graphicsFormat = t;
                        break;
                    }
            }

            desc.width /= dofResolutionDownscaler;
            desc.height /= dofResolutionDownscaler;

            ShutterScreenInfo.x = 1f / desc.width;
            ShutterScreenInfo.y = 1f / desc.height;
            ShutterScreenInfo.z = 0.5f / desc.width;
            ShutterScreenInfo.w = 0.5f / desc.height;

            TextureHandle prepassHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, CoCName, false,
                FilterMode.Bilinear);

            preMaterial.SetVector(ShutterInfoID, ShutterInfo);
            preMaterial.SetVector(ShutterScreenInfoID, ShutterScreenInfo);

            RenderGraphUtils.BlitMaterialParameters cocParameters =
                new(resourceData.activeColorTexture, prepassHandle, preMaterial, 0);
            renderGraph.AddBlitPass(cocParameters, "SBTPP CoC");

            material.SetVector(ShutterInfoID, ShutterInfo);
            material.SetVector(ShutterScreenInfoID, ShutterScreenInfo);

                AddPass(renderGraph, material, resourceData.activeColorTexture, accumulationHandle, prepassHandle,
                    "Shutter Based Temporal Post-Processing");

                AddCopyPass(renderGraph, accumulationHandle, resourceData.activeColorTexture,
                    "SBTPP Copy Accumulation");
        }
    }
}