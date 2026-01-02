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

        private Vector4 ShutterScreenInfo = Vector4.one;
        public PhysicalCamera.CoCTarget cocTarget = PhysicalCamera.CoCTarget.Accumulation;
        private Material material;
        private Material preMaterial;

#if UNITY_EDITOR
        public bool debugCoC = false;
#endif

        public ShutterBasedTemporalRenderPass(ShutterBasedTemporalPostProcessingData data) {
            ConfigureInput(ScriptableRenderPassInput.Color);
            ConfigureInput(ScriptableRenderPassInput.Depth);
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            preMaterial = new Material(data.prepassShader);
            material = new Material(data.sbtppShader);
        }

        public void Dispose() {
            accumulation.Release();
        }

        private class PassData {
            public TextureHandle source;
            public TextureHandle coc;
            public Material material;
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
            // builder.UseTexture(passData.source);
            builder.UseTexture(passData.coc);
            builder.SetRenderAttachment(to, 0);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                ExecutePass(data, context));
        }

        protected class PrePassData {
            public TextureHandle source;
            public Material material;
        }

        protected static void ExecutePrePass(PrePassData data, RasterGraphContext context) {
            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, data.material, 0);
        }

        private static void AddPrePass(RenderGraph renderGraph, TextureHandle source, TextureHandle target, Material material, string name = "Prepass") {
            using var builder = renderGraph.AddRasterRenderPass<PrePassData>(name, out var passData);
            passData.material = material;
            passData.source = source;
            // builder.UseTexture(passData.source);
            builder.SetRenderAttachment(target, 0);
            builder.SetRenderFunc((PrePassData data, RasterGraphContext context) =>
                ExecutePrePass(data, context));
        }

        protected class CopyPassData {
            public TextureHandle source;
        }

        protected static void ExecuteCopyPass(CopyPassData data, RasterGraphContext context) {
            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, 0f, true);
        }

        private static void AddCopyPass(RenderGraph renderGraph, TextureHandle from,
            TextureHandle to,
            string name = "Copy") {
            using var builder = renderGraph.AddRasterRenderPass<CopyPassData>(name, out var passData);
            passData.source = from;
            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(to, 0);
            builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                ExecuteCopyPass(data, context));
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            Shader.SetGlobalVector(ShutterInfoID, ShutterInfo);
            
#if UNITY_EDITOR
            if (debugCoC) {
                Shader.EnableKeyword("_SBTPP_DEBUG_COC");
            }
            else {
                Shader.DisableKeyword("_SBTPP_DEBUG_COC");
            }
#endif

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

            ShutterScreenInfo.x = 0.5f / desc.width;
            ShutterScreenInfo.y = 0.5f / desc.height;
            ShutterScreenInfo.z = 0.25f / desc.width;
            ShutterScreenInfo.w = 0.25f / desc.height;
            
            preMaterial.SetVector(ShutterScreenInfoID, ShutterScreenInfo);
            material.SetVector(ShutterScreenInfoID, ShutterScreenInfo);

            TextureHandle prepassHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, CoCName, false,
                FilterMode.Bilinear);

            AddPrePass(renderGraph, cocTarget switch {
                PhysicalCamera.CoCTarget.ActiveColor => resourceData.activeColorTexture,
                PhysicalCamera.CoCTarget.Accumulation => accumulationHandle,
                _ => resourceData.activeColorTexture}, prepassHandle, preMaterial, "SBTPP Prepass");
            
            AddPass(renderGraph, material, resourceData.activeColorTexture, accumulationHandle, prepassHandle,
                "Shutter Based Temporal Post-Processing");

            AddCopyPass(renderGraph, accumulationHandle, resourceData.activeColorTexture,
                "SBTPP Copy Accumulation");
        }
    }
}