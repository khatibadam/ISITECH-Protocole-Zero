using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProtocoleZero
{
    /// <summary>
    /// Tier-aware post-processing trimmer. On the mobile/Quest quality tier (or any
    /// Android build) it disables the fullscreen effects whose cost/benefit is poor on a
    /// tiled mobile GPU (chromatic aberration, film grain) and keeps the cheap mood
    /// effects (vignette, color grading) plus bloom. PCVR keeps the full stack.
    /// Operates on the runtime-instanced profile so the shared asset is never mutated.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public sealed class QuestPostFXTuner : MonoBehaviour
    {
        [SerializeField] private Volume volume;
        [Tooltip("Also drop bloom on mobile (bloom is the most expensive fullscreen pass).")]
        [SerializeField] private bool dropBloomOnMobile = false;

        private void Start()
        {
            if (volume == null)
            {
                volume = GetComponent<Volume>();
            }

            if (volume == null || !IsMobileTier())
            {
                return;
            }

            // volume.profile returns a runtime clone; editing it never touches the asset.
            VolumeProfile p = volume.profile;
            if (p == null)
            {
                return;
            }

            if (p.TryGet(out ChromaticAberration ca))
            {
                ca.active = false;
            }

            if (p.TryGet(out FilmGrain fg))
            {
                fg.active = false;
            }

            if (dropBloomOnMobile && p.TryGet(out Bloom bloom))
            {
                bloom.active = false;
            }
        }

        private static bool IsMobileTier()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                return true;
            }

            int level = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;
            if (level >= 0 && level < names.Length)
            {
                return names[level].ToLowerInvariant().Contains("mobile");
            }

            return false;
        }
    }
}
