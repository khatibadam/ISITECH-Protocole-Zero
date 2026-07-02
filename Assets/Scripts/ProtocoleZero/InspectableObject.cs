using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProtocoleZero
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class InspectableObject : MonoBehaviour
    {
        [SerializeField] private string grabLine = "Indice recupere.";
        [SerializeField] private string activateLine = "Retourne l'objet : l'indice est au dos.";
        [SerializeField] private Transform revealWhenHeld;
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private AudioFeedbackRouter audioFeedback;
        [SerializeField] private HapticFeedbackRouter haptics;

        private XRGrabInteractable grab;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            if (subtitles == null)
            {
                subtitles = FindFirstObjectByType<SubtitleManager>();
            }

            if (audioFeedback == null)
            {
                audioFeedback = FindFirstObjectByType<AudioFeedbackRouter>();
            }

            if (haptics == null)
            {
                haptics = FindFirstObjectByType<HapticFeedbackRouter>();
            }

            if (revealWhenHeld != null)
            {
                revealWhenHeld.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (grab != null)
            {
                grab.selectEntered.AddListener(HandleSelected);
                grab.selectExited.AddListener(HandleExited);
                grab.activated.AddListener(HandleActivated);
            }
        }

        private void OnDisable()
        {
            if (grab != null)
            {
                grab.selectEntered.RemoveListener(HandleSelected);
                grab.selectExited.RemoveListener(HandleExited);
                grab.activated.RemoveListener(HandleActivated);
            }
        }

        private void HandleSelected(SelectEnterEventArgs args)
        {
            revealWhenHeld?.gameObject.SetActive(true);
            subtitles?.ShowLine(grabLine, 3f);
            audioFeedback?.PlayCableGrab(transform.position);
            haptics?.PulseLight("inspect grab");
        }

        private void HandleExited(SelectExitEventArgs args)
        {
            revealWhenHeld?.gameObject.SetActive(false);
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            subtitles?.ShowLine(activateLine, 4f);
            haptics?.PulseMedium("inspect activate");
        }
    }
}
