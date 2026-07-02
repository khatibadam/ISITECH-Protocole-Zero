using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace ProtocoleZero
{
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

            if (logPulses)
            {
                Debug.Log($"[Haptics] {label} amp={amplitude:0.00} duration={duration:0.00}");
            }

            if (amplitude <= 0f || duration <= 0f)
            {
                return;
            }

            bool sentViaRumble = false;
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is XRControllerWithRumble rumble && device.added)
                {
                    rumble.SendImpulse(amplitude, duration);
                    sentViaRumble = true;
                }
            }

            if (sentViaRumble)
            {
                return;
            }

            leftHand?.SendHapticImpulse(amplitude, duration);
            rightHand?.SendHapticImpulse(amplitude, duration);
        }
    }
}
