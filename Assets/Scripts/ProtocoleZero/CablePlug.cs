using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProtocoleZero
{
    /// <summary>
    /// A grabbable rigid cable connector. In VR it is picked up with an
    /// <see cref="XRGrabInteractable"/> and inserted into a matching
    /// <see cref="ElectricalSocket"/> (which owns an XRSocketInteractor).
    /// The socket calls <see cref="Lock"/> to freeze the cable once seated.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CablePlug : MonoBehaviour
    {
        [SerializeField] private string plugId = "A";
        [SerializeField] private Renderer feedbackRenderer;
        [SerializeField] private Color freeColor = new Color(0.1f, 0.35f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.1f, 1f, 0.35f);
        [SerializeField] private AudioFeedbackRouter audioFeedback;
        [SerializeField] private HapticFeedbackRouter haptics;
        [Tooltip("Delai apres avoir lache le cable avant qu'il revienne a sa position de depart, s'il n'est ni tenu ni branche.")]
        [SerializeField, Min(0f)] private float returnDelay = 1.5f;

        private Rigidbody body;
        private float returnTimer = -1f;
        private XRGrabInteractable grabInteractable;
        private MaterialPropertyBlock feedbackBlock;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private ElectricalSocket lockedSocket;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public string PlugId => plugId;
        public bool IsLocked => lockedSocket != null;
        public XRGrabInteractable Grab => grabInteractable;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            grabInteractable = GetComponent<XRGrabInteractable>();
            feedbackBlock = new MaterialPropertyBlock();
            if (audioFeedback == null)
            {
                audioFeedback = FindFirstObjectByType<AudioFeedbackRouter>();
            }

            if (haptics == null)
            {
                haptics = FindFirstObjectByType<HapticFeedbackRouter>();
            }

            startPosition = transform.position;
            startRotation = transform.rotation;
            SetColor(freeColor);
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(HandleSelected);
                grabInteractable.selectExited.AddListener(HandleReleased);
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(HandleSelected);
                grabInteractable.selectExited.RemoveListener(HandleReleased);
            }
        }

        // Cable lache dans le vide : apres un court delai (le temps qu'un socket proche
        // puisse le happer), il revient a sa position de depart au lieu de s'envoler.
        private void HandleReleased(SelectExitEventArgs args)
        {
            returnTimer = returnDelay;
        }

        private void Update()
        {
            if (returnTimer < 0f)
            {
                return;
            }

            returnTimer -= Time.deltaTime;
            if (returnTimer >= 0f)
            {
                return;
            }

            if (!IsLocked && grabInteractable != null && grabInteractable.enabled && !grabInteractable.isSelected)
            {
                ResetPlug();
            }
        }

        private void OnMouseDown()
        {
            PlayGrabFeedback();
        }

        /// <summary>Seat and freeze this plug on a socket. Called by the socket once accepted.</summary>
        public void Lock(ElectricalSocket socket)
        {
            lockedSocket = socket;
            transform.SetPositionAndRotation(socket.SnapPosition, socket.SnapRotation);
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = true;
            }

            // Once correctly seated the cable stays put: it can no longer be grabbed
            // or pulled out, which also releases any interactor still holding it.
            if (grabInteractable != null)
            {
                grabInteractable.enabled = false;
            }

            SetColor(lockedColor);
            haptics?.PulseMedium("cable locked");
        }

        // Backwards-compatible alias used by ElectricalSocket.ForceSolved / smoke tests.
        public void AttachTo(ElectricalSocket socket) => Lock(socket);

        private void HandleSelected(SelectEnterEventArgs args)
        {
            returnTimer = -1f;
            PlayGrabFeedback();
        }

        private void PlayGrabFeedback()
        {
            if (!IsLocked)
            {
                audioFeedback?.PlayCableGrab(transform.position);
                haptics?.PulseLight("cable grab");
            }
        }

        public void ResetPlug()
        {
            lockedSocket = null;
            if (grabInteractable != null)
            {
                grabInteractable.enabled = true;
            }

            transform.SetPositionAndRotation(startPosition, startRotation);
            if (body != null)
            {
                body.isKinematic = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            SetColor(freeColor);
        }

        private void SetColor(Color color)
        {
            if (feedbackRenderer != null)
            {
                feedbackRenderer.GetPropertyBlock(feedbackBlock);
                feedbackBlock.SetColor(BaseColorId, color);
                feedbackBlock.SetColor(ColorId, color);
                feedbackRenderer.SetPropertyBlock(feedbackBlock);
            }
        }
    }
}
