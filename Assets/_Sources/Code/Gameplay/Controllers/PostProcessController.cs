using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sources.Controllers
{
    public class PostProcessController : MonoBehaviour
    {
        [SerializeField] private Volume volume;

        private ColorAdjustments color;
        private Vignette vignette;
        private ChromaticAberration chromatic;
        private DepthOfField blur;

        private Color baseColor;
        private float baseSaturation;
        private float baseContrast;

        private void Awake()
        {
            if (volume.profile == null)
                volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var profile = volume.profile;

            if (!profile.TryGet(out color))
                color = profile.Add<ColorAdjustments>(true);

            if (!profile.TryGet(out vignette))
                vignette = profile.Add<Vignette>(true);

            if (!profile.TryGet(out chromatic))
                chromatic = profile.Add<ChromaticAberration>(true);

            if (!profile.TryGet(out blur))
            {
                blur = profile.Add<DepthOfField>(true);
                blur.mode.Override(DepthOfFieldMode.Gaussian);
            }

            baseColor = color.colorFilter.value;
            baseSaturation = color.saturation.value;
            baseContrast = color.contrast.value;
        }

        public void ApplyVisual(VisualSettings settings)
        {
            color.colorFilter.Override(
                Color.Lerp(baseColor, settings.overlayColor, settings.overlayOpacity)
            );

            vignette.active = settings.vignette;
            vignette.intensity.Override(settings.vignette ? 0.45f : 0f);
            vignette.smoothness.Override(settings.vignette ? 0.6f : 0f);
            vignette.rounded.Override(false);

            chromatic.active = settings.chromaticAberration;
            chromatic.intensity.Override(settings.chromaticAberration ? 0.3f : 0f);

            blur.active = settings.blurAmount > 0f;
            blur.gaussianStart.Override(settings.blurAmount);
        }

        public void ApplyPost(PostEffectSettings settings)
        {
            color.colorFilter.Override(baseColor * settings.colorTint);
            color.saturation.Override(settings.saturation);
            color.contrast.Override(settings.contrast);
        }

        public void Restore()
        {
            color.colorFilter.Override(baseColor);
            color.saturation.Override(baseSaturation);
            color.contrast.Override(baseContrast);

            vignette.active = false;
            vignette.intensity.Override(0f);
            vignette.smoothness.Override(0f);

            chromatic.active = false;
            chromatic.intensity.Override(0f);

            blur.active = false;
        }
    }
}
