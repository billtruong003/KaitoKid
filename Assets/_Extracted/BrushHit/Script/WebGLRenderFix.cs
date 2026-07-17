// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.Universal;
// using System.Reflection;

// /// <summary>
// /// WebGL Pipeline Fix — Unity 6 (6000.x)
// ///
// /// Full magenta trên WebGL xảy ra vì:
// /// 1. GPUResidentDrawer cố khởi tạo BatchBufferTarget (SSBO) → fail → null
// /// 2. Render Graph dùng resource handles → null propagation → toàn bộ pipeline crash
// /// 3. Kết quả: camera render fallback color (magenta) mỗi frame
// ///
// /// Script này chạy BeforeSceneLoad, dùng reflection để force disable
// /// các feature không support trên WebGL, kể cả khi URP Asset UI không apply đúng.
// /// </summary>
// public static class WebGLPipelineFix
// {
//     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
//     static void Apply()
//     {
// #if UNITY_WEBGL && !UNITY_EDITOR
//         Debug.Log("[WebGLPipelineFix] Applying WebGL compatibility fixes...");

//         var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
//         if (urpAsset == null)
//         {
//             Debug.LogError("[WebGLPipelineFix] No URP Asset found!");
//             return;
//         }

//         // ═══════════════════════════════════════════
//         // 1. Disable GPU Resident Drawer (BRG)
//         // ═══════════════════════════════════════════
//         try
//         {
//             urpAsset.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
//             Debug.Log("[WebGLPipelineFix] GPU Resident Drawer → Disabled");
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogWarning($"[WebGLPipelineFix] Could not set gpuResidentDrawerMode: {e.Message}");
//         }

//         // ═══════════════════════════════════════════
//         // 2. Disable SRPBatcher — nó phụ thuộc vào
//         //    constant buffer layout mà WebGL có thể
//         //    không support đúng với custom shaders
//         // ═══════════════════════════════════════════
//         // try
//         // {
//         //     // GraphicsSettings.useScriptableRenderPipelineBatching = false;
//         //     Debug.Log("[WebGLPipelineFix] SRP Batcher → Disabled");
//         // }
//         // catch (System.Exception e)
//         // {
//         //     Debug.LogWarning($"[WebGLPipelineFix] Could not disable SRP Batcher: {e.Message}");
//         // }

//         // ═══════════════════════════════════════════
//         // 3. Force Render Graph Compatibility Mode
//         //    via reflection (API thay đổi giữa Unity 6 versions)
//         // ═══════════════════════════════════════════
//         TryDisableRenderGraph(urpAsset);

//         // ═══════════════════════════════════════════
//         // 4. Reduce shadow features — WebGL shadow
//         //    issues are very common
//         // ═══════════════════════════════════════════
//         try
//         {
//             // Force simple shadows
//             var mainLightShadowField = typeof(UniversalRenderPipelineAsset)
//                 .GetField("m_MainLightShadowsSupported", BindingFlags.NonPublic | BindingFlags.Instance);
//             if (mainLightShadowField != null)
//             {
//                 mainLightShadowField.SetValue(urpAsset, true);
//             }

//             // Disable additional light shadows (heavy on WebGL)
//             var addShadowField = typeof(UniversalRenderPipelineAsset)
//                 .GetField("m_AdditionalLightShadowsSupported", BindingFlags.NonPublic | BindingFlags.Instance);
//             if (addShadowField != null)
//             {
//                 addShadowField.SetValue(urpAsset, false);
//             }

//             Debug.Log("[WebGLPipelineFix] Shadow settings adjusted");
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogWarning($"[WebGLPipelineFix] Could not adjust shadow settings: {e.Message}");
//         }

//         Debug.Log("[WebGLPipelineFix] All fixes applied successfully.");
// #endif
//     }

//     static void TryDisableRenderGraph(UniversalRenderPipelineAsset urpAsset)
//     {
//         // Unity 6 has different API versions for render graph control
//         // Try multiple approaches

//         // Approach 1: Direct property (Unity 6000.1+)
//         try
//         {
//             var prop = typeof(UniversalRenderPipelineAsset)
//                 .GetProperty("renderGraphSettings", BindingFlags.Public | BindingFlags.Instance);
//             if (prop != null)
//             {
//                 var settings = prop.GetValue(urpAsset);
//                 if (settings != null)
//                 {
//                     var compatField = settings.GetType()
//                         .GetField("enableRenderCompatibilityMode", BindingFlags.Public | BindingFlags.Instance);
//                     if (compatField != null)
//                     {
//                         compatField.SetValue(settings, true);
//                         prop.SetValue(urpAsset, settings);
//                         Debug.Log("[WebGLPipelineFix] Render Graph → Compatibility Mode (via renderGraphSettings)");
//                         return;
//                     }
//                 }
//             }
//         }
//         catch { }

//         // Approach 2: Private field (older Unity 6 builds)
//         try
//         {
//             var field = typeof(UniversalRenderPipelineAsset)
//                 .GetField("m_RenderGraphEnabled", BindingFlags.NonPublic | BindingFlags.Instance);
//             if (field != null)
//             {
//                 field.SetValue(urpAsset, false);
//                 Debug.Log("[WebGLPipelineFix] Render Graph → Disabled (via m_RenderGraphEnabled)");
//                 return;
//             }
//         }
//         catch { }

//         // Approach 3: pipelineRenderGraphEnabled
//         try
//         {
//             var field = typeof(UniversalRenderPipelineAsset)
//                 .GetField("m_PipelineRenderGraphEnabled", BindingFlags.NonPublic | BindingFlags.Instance);
//             if (field != null)
//             {
//                 field.SetValue(urpAsset, false);
//                 Debug.Log("[WebGLPipelineFix] Render Graph → Disabled (via m_PipelineRenderGraphEnabled)");
//                 return;
//             }
//         }
//         catch { }

//         Debug.LogWarning("[WebGLPipelineFix] Could not disable Render Graph via reflection. Please disable manually in URP Asset.");
//     }
// }