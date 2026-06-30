using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProtocoleZero
{
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

        private Rigidbody body;
        private XRGrabInteractable grabInteractable;
        private MaterialPropertyBlock feedbackBlock;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private ElectricalSocket lockedSocket;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public string PlugId => plugId;
        public bool IsLocked => lockedSocket != null;

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
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(HandleSelected);
            }
        }

        private void OnMouseDown()
        {
            PlayGrabFeedback();
        }

        public void AttachTo(ElectricalSocket socket)
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

            SetColor(lockedColor);
            haptics?.PulseMedium("cable locked");
        }

        private void HandleSelected(SelectEnterEventArgs args)
        {
            PlayGrabFeedback();
        }

        private void PlayGrabFeedback()
        {
            if (!IsLocked)
            {
                audioFeedback?.PlayCableGrab();
                haptics?.PulseLight("cable grab");
            }
        }

        public void ResetPlug()
        {
            lockedSocket = null;
            transform.SetPositionAndRotation(startPosition, startRotation);
            if (body != null)
            {
                body.isKinematic = false;
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
