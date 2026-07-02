using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ProtocoleZero
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class ValveWheelInteractable : MonoBehaviour
    {
        [SerializeField] private Transform wheelVisual;
        [SerializeField] private Transform wheelCenter;
        [SerializeField] private Vector3 localRotationAxis = Vector3.forward;
        [SerializeField, Min(5f)] private float requiredDegrees = 115f;
        [SerializeField, Range(0f, 1f)] private float openThreshold = 0.92f;
        [SerializeField] private bool invertDrag;
        [SerializeField] private Renderer feedbackRenderer;
        [SerializeField] private Color lockedColor = new Color(0.15f, 0.08f, 0.06f);
        [SerializeField] private Color openColor = new Color(0.1f, 0.65f, 0.45f);
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private AudioFeedbackRouter audioFeedback;
        [SerializeField] private HapticFeedbackRouter haptics;

        private XRGrabInteractable grab;
        private IXRSelectInteractor selectingInteractor;
        private Quaternion visualRestLocalRotation;
        private float selectStartAngle;
        private float selectStartDegrees;
        private float currentDegrees;
        private bool open;
        private MaterialPropertyBlock block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool IsOpen => open;
        public float NormalizedOpen => Mathf.Clamp01(currentDegrees / requiredDegrees);

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            block = new MaterialPropertyBlock();

            if (wheelVisual == null)
            {
                wheelVisual = transform;
            }

            if (wheelCenter == null)
            {
                wheelCenter = wheelVisual;
            }

            visualRestLocalRotation = wheelVisual.localRotation;

            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            grab.trackPosition = false;
            grab.trackRotation = false;
            grab.throwOnDetach = false;

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

            ApplyDegrees(0f);
            SetFeedback(lockedColor);
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

        private void Update()
        {
            if (selectingInteractor == null || open)
            {
                return;
            }

            float angle = GetInteractorAngle(selectingInteractor);
            float delta = Mathf.DeltaAngle(selectStartAngle, angle);
            if (invertDrag)
            {
                delta = -delta;
            }

            ApplyDegrees(Mathf.Clamp(selectStartDegrees + delta, 0f, requiredDegrees));
            if (NormalizedOpen >= openThreshold)
            {
                MarkOpen();
            }
        }

        private void HandleSelected(SelectEnterEventArgs args)
        {
            selectingInteractor = args.interactorObject;
            selectStartAngle = GetInteractorAngle(selectingInteractor);
            selectStartDegrees = currentDegrees;
            audioFeedback?.PlayCableGrab(transform.position);
            haptics?.PulseLight("valve grab");
        }

        private void HandleExited(SelectExitEventArgs args)
        {
            if (selectingInteractor == args.interactorObject)
            {
                selectingInteractor = null;
            }
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            ForceOpen();
        }

        private void OnMouseDown()
        {
            ForceOpen();
        }

        public void ForceOpen()
        {
            ApplyDegrees(requiredDegrees);
            MarkOpen();
        }

        private void MarkOpen()
        {
            if (open)
            {
                return;
            }

            open = true;
            selectingInteractor = null;
            SetFeedback(openColor);
            audioFeedback?.PlayFinalDoorOpen(transform.position);
            haptics?.PulseStrong("valve open");
            subtitles?.ShowLine("Vanne ouverte : la pression retombe dans le tableau.", 3f);
        }

        private float GetInteractorAngle(IXRSelectInteractor interactor)
        {
            if (interactor == null || wheelCenter == null)
            {
                return 0f;
            }

            Vector3 local = wheelCenter.InverseTransformPoint(interactor.transform.position);
            if (local.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            return Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
        }

        private void ApplyDegrees(float degrees)
        {
            currentDegrees = Mathf.Clamp(degrees, 0f, requiredDegrees);
            if (wheelVisual != null)
            {
                wheelVisual.localRotation = visualRestLocalRotation * Quaternion.AngleAxis(currentDegrees, GetAxis());
            }
        }

        private Vector3 GetAxis()
        {
            if (localRotationAxis.sqrMagnitude < 0.001f)
            {
                return Vector3.forward;
            }

            return localRotationAxis.normalized;
        }

        private void SetFeedback(Color color)
        {
            if (feedbackRenderer == null)
            {
                return;
            }

            feedbackRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            feedbackRenderer.SetPropertyBlock(block);
        }
    }
}
