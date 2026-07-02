using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProtocoleZero
{
    /// <summary>
    /// One-shot power "brown-out" horror beat. Darkens the whole rendered image via the
    /// horror volume's ColorAdjustments.postExposure (so it works with BAKED lighting too,
    /// not just realtime) AND cuts any remaining realtime, non-directional lights for extra
    /// flicker, then restores both. Fire it from a puzzle's onSolved event or let it watch a
    /// puzzle's IsSolved. Non-destructive: original exposure and light intensities are captured.
    /// </summary>
    public sealed class BlackoutPulse : MonoBehaviour
    {
        [SerializeField] private float blackoutSeconds = 0.7f;
        [SerializeField] private float recoverSeconds = 0.6f;
        [SerializeField] private AudioSource stinger;
        [Tooltip("Optional: fire the blackout once this puzzle becomes solved.")]
        [SerializeField] private ElectricalPanelPuzzle watchedPuzzle;
        [Tooltip("Horror post-processing volume. Auto-found if left empty.")]
        [SerializeField] private Volume postVolume;
        [Tooltip("Exposure (EV) applied at the darkest point of the blackout.")]
        [SerializeField] private float darkExposure = -6f;
        [Tooltip("Optional: rumble the VR controllers during the blackout. Auto-found if left empty.")]
        [SerializeField] private HapticFeedbackRouter haptics;

        private bool running;
        private bool firedFromPuzzle;
        private ColorAdjustments colorAdjust;

        private void Update()
        {
            if (!firedFromPuzzle && watchedPuzzle != null && watchedPuzzle.IsSolved)
            {
                firedFromPuzzle = true;
                Trigger();
            }
        }

        public void Trigger()
        {
            if (running)
            {
                return;
            }

            StartCoroutine(Pulse());
        }

        private ColorAdjustments ResolveColorAdjustments()
        {
            if (colorAdjust != null)
            {
                return colorAdjust;
            }

            if (postVolume == null)
            {
                foreach (var v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
                {
                    if (v.isGlobal && v.sharedProfile != null)
                    {
                        postVolume = v;
                        break;
                    }
                }
            }

            // volume.profile is a runtime clone; editing it never touches the shared asset.
            if (postVolume != null && postVolume.profile != null &&
                postVolume.profile.TryGet(out ColorAdjustments ca))
            {
                colorAdjust = ca;
            }

            return colorAdjust;
        }

        private IEnumerator Pulse()
        {
            running = true;

            ColorAdjustments ca = ResolveColorAdjustments();
            float baseExposure = ca != null ? ca.postExposure.value : 0f;

            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            var captured = new List<(Light light, float intensity)>(lights.Length);
            foreach (Light l in lights)
            {
                if (l != null && l.enabled && l.gameObject.activeInHierarchy && l.type != LightType.Directional)
                {
                    captured.Add((l, l.intensity));
                }
            }

            if (haptics == null)
            {
                haptics = FindFirstObjectByType<HapticFeedbackRouter>();
            }

            if (stinger != null)
            {
                stinger.Stop();
                stinger.Play();
            }

            haptics?.Pulse(0.85f, 0.3f, "blackout-cut");

            // hard cut: dark exposure + kill realtime lights
            if (ca != null) ca.postExposure.value = baseExposure + darkExposure;
            foreach (var c in captured)
            {
                c.light.intensity = 0f;
            }

            // stutter flashes during the blackout
            float t = 0f;
            while (t < blackoutSeconds)
            {
                float flash = (Random.value < 0.25f) ? 0.25f : 0f;
                if (ca != null) ca.postExposure.value = baseExposure + darkExposure * (1f - flash);
                foreach (var c in captured)
                {
                    c.light.intensity = c.intensity * flash;
                }

                float step = Random.Range(0.04f, 0.12f);
                haptics?.Pulse(flash > 0f ? 0.45f : 0.2f, step, "blackout-stutter");
                t += step;
                yield return new WaitForSeconds(step);
            }

            // smooth recover
            float r = 0f;
            while (r < recoverSeconds)
            {
                r += Time.deltaTime;
                float k = Mathf.Clamp01(r / recoverSeconds);
                if (ca != null) ca.postExposure.value = baseExposure + darkExposure * (1f - k);
                foreach (var c in captured)
                {
                    c.light.intensity = c.intensity * k;
                }

                yield return null;
            }

            if (ca != null) ca.postExposure.value = baseExposure;
            foreach (var c in captured)
            {
                c.light.intensity = c.intensity;
            }

            running = false;
        }
    }
}
