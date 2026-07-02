using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProtocoleZero
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class ThrowableImpactFeedback : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float minImpactSpeed = 0.8f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 0.4f;
        [SerializeField] private AudioSource impactSource;
        [SerializeField] private HapticFeedbackRouter haptics;

        private XRGrabInteractable grab;
        private float cooldown;
        private bool releasedOnce;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            grab.throwOnDetach = true;

            if (haptics == null)
            {
                haptics = FindFirstObjectByType<HapticFeedbackRouter>();
            }
        }

        private void OnEnable()
        {
            if (grab != null)
            {
                grab.selectEntered.AddListener(HandleSelected);
                grab.selectExited.AddListener(HandleReleased);
            }
        }

        private void OnDisable()
        {
            if (grab != null)
            {
                grab.selectEntered.RemoveListener(HandleSelected);
                grab.selectExited.RemoveListener(HandleReleased);
            }
        }

        private void Update()
        {
            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);
        }

        private void HandleSelected(SelectEnterEventArgs args)
        {
            releasedOnce = false;
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
        }

        private void HandleReleased(SelectExitEventArgs args)
        {
            releasedOnce = true;
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!releasedOnce || cooldown > 0f || collision.relativeVelocity.magnitude < minImpactSpeed)
            {
                return;
            }

            cooldown = cooldownSeconds;
            if (impactSource != null)
            {
                impactSource.Stop();
                impactSource.Play();
            }

            haptics?.PulseLight("throw impact");
        }
    }
}
