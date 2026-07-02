using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ProtocoleZero
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class BreakerLever : MonoBehaviour
    {
        [SerializeField] private Transform leverPivot;
        [SerializeField] private ElectricalPanelPuzzle requiredPuzzle;
        [SerializeField] private float restAngle = 0f;
        [SerializeField] private float armedAngle = -65f;
        [SerializeField, Range(0f, 1f)] private float armThreshold = 0.82f;
        [SerializeField, Min(1f)] private float returnSpeed = 160f;
        [SerializeField] private Renderer feedbackRenderer;
        [SerializeField] private Color idleColor = new Color(0.08f, 0.08f, 0.08f);
        [SerializeField] private Color armedColor = new Color(0.15f, 0.9f, 0.35f);
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private AudioFeedbackRouter audioFeedback;
        [SerializeField] private HapticFeedbackRouter haptics;

        private XRGrabInteractable grab;
        private IXRSelectInteractor selectingInteractor;
        private float currentAngle;
        private bool armed;
        private MaterialPropertyBlock block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool IsArmed => armed;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            block = new MaterialPropertyBlock();

            if (leverPivot == null)
            {
                leverPivot = transform;
            }

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

            ApplyAngle(restAngle);
            SetFeedback(idleColor);
        }

        private void OnEnable()
        {
            if (grab != null)
            {
                grab.selectEntered.AddListener(HandleSelectEntered);
                grab.selectExited.AddListener(HandleSelectExited);
                grab.activated.AddListener(HandleActivated);
            }
        }

        private void OnDisable()
        {
            if (grab != null)
            {
                grab.selectEntered.RemoveListener(HandleSelectEntered);
                grab.selectExited.RemoveListener(HandleSelectExited);
                grab.activated.RemoveListener(HandleActivated);
            }
        }

        private void Update()
        {
            if (armed)
            {
                ApplyAngle(Mathf.MoveTowards(currentAngle, armedAngle, returnSpeed * Time.deltaTime));
                return;
            }

            if (selectingInteractor != null)
            {
                UpdateFromInteractor(selectingInteractor);
                if (Mathf.InverseLerp(restAngle, armedAngle, currentAngle) >= armThreshold)
                {
                    Arm();
                }
            }
            else
            {
                ApplyAngle(Mathf.MoveTowards(currentAngle, restAngle, returnSpeed * Time.deltaTime));
            }
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            selectingInteractor = args.interactorObject;
            haptics?.PulseLight("breaker grab");
        }

        private void HandleSelectExited(SelectExitEventArgs args)
        {
            if (selectingInteractor == args.interactorObject)
            {
                selectingInteractor = null;
            }
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            ForceArm();
        }

        private void OnMouseDown()
        {
            ForceArm();
        }

        private void UpdateFromInteractor(IXRSelectInteractor interactor)
        {
            if (leverPivot == null || interactor == null)
            {
                return;
            }

            Vector3 local = leverPivot.InverseTransformPoint(interactor.transform.position);
            float pulled = Mathf.InverseLerp(0.16f, -0.18f, local.y);
            ApplyAngle(Mathf.Lerp(restAngle, armedAngle, pulled));
        }

        public void ForceArm()
        {
            if (requiredPuzzle != null && !requiredPuzzle.IsSolved)
            {
                subtitles?.ShowLine("Le disjoncteur resiste : il manque les cables.", 2.5f);
                audioFeedback?.PlaySocketWrong(transform.position);
                haptics?.PulseLight("breaker blocked");
                return;
            }

            Arm();
        }

        private void Arm()
        {
            if (armed)
            {
                return;
            }

            armed = true;
            selectingInteractor = null;
            ApplyAngle(armedAngle);
            SetFeedback(armedColor);
            audioFeedback?.PlayFinalDoorOpen(transform.position);
            haptics?.PulseStrong("breaker armed");
            subtitles?.ShowLine("Disjoncteur general rearme.", 3f);
        }

        private void ApplyAngle(float angle)
        {
            currentAngle = angle;
            if (leverPivot != null)
            {
                leverPivot.localRotation = Quaternion.Euler(currentAngle, 0f, 0f);
            }
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
