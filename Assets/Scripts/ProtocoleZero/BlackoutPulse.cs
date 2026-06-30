using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProtocoleZero
{
    /// <summary>
    /// One-shot power "brown-out": briefly cuts all scene lights, flickers, then restores them.
    /// Designed to be invoked from a puzzle's onSolved UnityEvent (e.g. the INFO electrical panel)
    /// for a horror beat. Captures each light's intensity at trigger time so it is non-destructive.
    /// </summary>
    public sealed class BlackoutPulse : MonoBehaviour
    {
        [SerializeField] private float blackoutSeconds = 0.7f;
        [SerializeField] private float recoverSeconds = 0.6f;
        [SerializeField] private AudioSource stinger;
        [Tooltip("Optional: fire the blackout once this puzzle becomes solved.")]
        [SerializeField] private ElectricalPanelPuzzle watchedPuzzle;

        private bool running;
        private bool firedFromPuzzle;

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

        private IEnumerator Pulse()
        {
            running = true;

            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            var captured = new List<(Light light, float intensity)>(lights.Length);
            foreach (Light l in lights)
            {
                if (l != null && l.enabled && l.gameObject.activeInHierarchy && l.type != LightType.Directional)
                {
                    captured.Add((l, l.intensity));
                }
            }

            if (stinger != null)
            {
                stinger.Stop();
                stinger.Play();
            }

            // hard cut
            foreach (var c in captured)
            {
                c.light.intensity = 0f;
            }

            // a couple of stutter flashes during the blackout
            float t = 0f;
            while (t < blackoutSeconds)
            {
                float flash = (Random.value < 0.25f) ? 0.25f : 0f;
                foreach (var c in captured)
                {
                    c.light.intensity = c.intensity * flash;
                }

                float step = Random.Range(0.04f, 0.12f);
                t += step;
                yield return new WaitForSeconds(step);
            }

            // smooth recover
            float r = 0f;
            while (r < recoverSeconds)
            {
                r += Time.deltaTime;
                float k = Mathf.Clamp01(r / recoverSeconds);
                foreach (var c in captured)
                {
                    c.light.intensity = c.intensity * k;
                }

                yield return null;
            }

            foreach (var c in captured)
            {
                c.light.intensity = c.intensity;
            }

            running = false;
        }
    }
}
