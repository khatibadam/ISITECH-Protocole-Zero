using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace ProtocoleZero
{
    /// <summary>
    /// Routes gameplay pulses to the real XRI haptic channels of both controllers.
    /// Light/Medium/Strong map to amplitude/duration pairs kept deliberately gentle
    /// (horror context: haptics should underline, not startle by themselves).
    /// </summary>
    public sealed class HapticFeedbackRouter : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float globalIntensity = 1f;
        [SerializeField] private HapticImpulsePlayer leftHand;
        [SerializeField] private HapticImpulsePlayer rightHand;
        [SerializeField] private bool logPulses;

        private void Awake()
        {
            if (leftHand == null || rightHand == null)
            {
                foreach (var player in FindObjectsByType<HapticImpulsePlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (player.name.Contains("Left") && leftHand == null)
                    {
                        leftHand = player;
                    }
                    else if (player.name.Contains("Right") && rightHand == null)
                    {
                        rightHand = player;
                    }
                }
            }
        }

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
            if (amplitude <= 0f)
            {
                return;
            }

            leftHand?.SendHapticImpulse(amplitude, duration);
            rightHand?.SendHapticImpulse(amplitude, duration);

            if (logPulses)
            {
                Debug.Log($"[Haptics] {label} amp={amplitude:0.00} duration={duration:0.00}");
            }
        }
    }
}
