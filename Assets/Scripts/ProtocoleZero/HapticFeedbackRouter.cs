using UnityEngine;

namespace ProtocoleZero
{
    public sealed class HapticFeedbackRouter : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float globalIntensity = 1f;
        [SerializeField] private bool logPlaceholderPulses;

        public void PulseLight(string label)
        {
            Pulse(0.18f, 0.06f, label);
        }

        public void PulseMedium(string label)
        {
            Pulse(0.42f, 0.1f, label);
        }

        public void PulseStrong(string label)
        {
            Pulse(0.72f, 0.16f, label);
        }

        public void Pulse(float amplitude, float duration, string label)
        {
            amplitude = Mathf.Clamp01(amplitude * globalIntensity);
            if (logPlaceholderPulses)
            {
                Debug.Log($"[Haptics placeholder] {label} amp={amplitude:0.00} duration={duration:0.00}");
            }
        }
    }
}
