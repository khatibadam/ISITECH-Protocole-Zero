using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ProtocoleZero
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class SlidingDrawerInteractable : MonoBehaviour
    {
        [SerializeField] private Transform drawerVisual;
        [SerializeField] private Transform axisRoot;
        [SerializeField] private Vector3 localAxis = Vector3.forward;
        [SerializeField, Min(0.01f)] private float travel = 0.34f;
        [SerializeField, Range(0f, 1f)] private float openThreshold = 0.82f;
        [SerializeField, Min(0.1f)] private float snapSpeed = 4f;
        [SerializeField] private Renderer feedbackRenderer;
        [SerializeField] private Color closedColor = new Color(0.11f, 0.1f, 0.09f);
        [SerializeField] private Color openColor = new Color(0.15f, 0.75f, 0.9f);
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private AudioFeedbackRouter audioFeedback;
        [SerializeField] private HapticFeedbackRouter haptics;

        private XRGrabInteractable grab;
        private IXRSelectInteractor selectingInteractor;
        private Vector3 visualRestLocalPosition;
        private Vector3 selectLocalPosition;
        private float selectStartAmount;
        private float amount;
        private float targetAmount;
        private bool open;
        private MaterialPropertyBlock block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool IsOpen => open;
        public float NormalizedOpen => amount;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            block = new MaterialPropertyBlock();

            if (drawerVisual == null)
            {
                drawerVisual = transform;
            }

            if (axisRoot == null)
            {
                axisRoot = transform;
            }

            visualRestLocalPosition = drawerVisual.localPosition;

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

            ApplyAmount(0f);
            SetFeedback(closedColor);
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
            if (selectingInteractor != null)
            {
                UpdateFromInteractor(selectingInteractor);
                if (!open && amount >= openThreshold)
                {
                    MarkOpen();
                }

                return;
            }

            if (!Mathf.Approximately(amount, targetAmount))
            {
                ApplyAmount(Mathf.MoveTowards(amount, targetAmount, snapSpeed * Time.deltaTime));
            }
        }

        private void HandleSelected(SelectEnterEventArgs args)
        {
            selectingInteractor = args.interactorObject;
            selectLocalPosition = axisRoot.InverseTransformPoint(selectingInteractor.transform.position);
            selectStartAmount = amount;
            audioFeedback?.PlayCableGrab(transform.position);
            haptics?.PulseLight("drawer grab");
        }

        private void HandleExited(SelectExitEventArgs args)
        {
            if (selectingInteractor == args.interactorObject)
            {
                selectingInteractor = null;
                targetAmount = open || amount >= openThreshold ? 1f : 0f;
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

        private void UpdateFromInteractor(IXRSelectInteractor interactor)
        {
            if (interactor == null)
            {
                return;
            }

            Vector3 axis = GetAxis();
            Vector3 localPosition = axisRoot.InverseTransformPoint(interactor.transform.position);
            float delta = Vector3.Dot(localPosition - selectLocalPosition, axis);
            ApplyAmount(Mathf.Clamp01(selectStartAmount + delta / travel));
            targetAmount = amount;
        }

        public void ForceOpen()
        {
            targetAmount = 1f;
            ApplyAmount(1f);
            MarkOpen();
        }

        public void ForceClose()
        {
            open = false;
            targetAmount = 0f;
            ApplyAmount(0f);
            SetFeedback(closedColor);
        }

        private void MarkOpen()
        {
            if (open)
            {
                return;
            }

            open = true;
            targetAmount = 1f;
            SetFeedback(openColor);
            audioFeedback?.PlaySocketCorrect(transform.position);
            haptics?.PulseMedium("drawer open");
            subtitles?.ShowLine("Casier ouvert : un indice tombe sous la main.", 3f);
        }

        private void ApplyAmount(float nextAmount)
        {
            amount = Mathf.Clamp01(nextAmount);
            if (drawerVisual != null)
            {
                drawerVisual.localPosition = visualRestLocalPosition + GetAxis() * (travel * amount);
            }
        }

        private Vector3 GetAxis()
        {
            if (localAxis.sqrMagnitude < 0.001f)
            {
                return Vector3.forward;
            }

            return localAxis.normalized;
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
