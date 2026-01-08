using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RuntimeConfig
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        QualitySettings.lodBias = 3.0f;
        Screen.SetResolution(320, 240, true);

        QualitySettings.antiAliasing = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.pixelLightCount = 0;
        QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;

        Physics.autoSyncTransforms = false;
        Physics.reuseCollisionCallbacks = true;


        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            urp.renderScale = 1f;
            urp.msaaSampleCount = 1;
            urp.supportsHDR = false;
        }
    }
}
