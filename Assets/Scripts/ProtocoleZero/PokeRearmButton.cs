using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProtocoleZero
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class PokeRearmButton : MonoBehaviour
    {
        [SerializeField] private Transform buttonVisual;
        [SerializeField, Min(0f)] private float pressDepth = 0.045f;
        [SerializeField, Min(1f)] private float returnSpeed = 18f;
        [SerializeField] private ElectricalPanelPuzzle requiredPuzzle;
        [SerializeField] private Renderer feedbackRenderer;
        [SerializeField] private Color idleColor = new Color(0.25f, 0.08f, 0.06f);
        [SerializeField] private Color armedColor = new Color(0.1f, 0.9f, 0.35f);
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private AudioFeedbackRouter audioFeedback;
        [SerializeField] private HapticFeedbackRouter haptics;

        private XRSimpleInteractable interactable;
        private Vector3 visualRestLocalPosition;
        private float pressAmount;
        private bool armed;
        private MaterialPropertyBlock block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool IsArmed => armed;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            block = new MaterialPropertyBlock();

            if (buttonVisual == null)
            {
                buttonVisual = transform;
            }

            visualRestLocalPosition = buttonVisual.localPosition;

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

            SetFeedback(idleColor);
        }

        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.AddListener(HandleSelect);
            }
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(HandleSelect);
            }
        }

        private void Update()
        {
            pressAmount = Mathf.MoveTowards(pressAmount, 0f, returnSpeed * Time.deltaTime);
            if (buttonVisual != null)
            {
                buttonVisual.localPosition = visualRestLocalPosition + Vector3.back * (pressDepth * pressAmount);
            }
        }

        private void HandleSelect(SelectEnterEventArgs args)
        {
            Press();
        }

        private void OnMouseDown()
        {
            Press();
        }

        public void Press()
        {
            pressAmount = 1f;

            if (requiredPuzzle != null && !requiredPuzzle.IsSolved)
            {
                subtitles?.ShowLine("Rearme impossible : branche d'abord les cables.", 2.5f);
                audioFeedback?.PlaySocketWrong(transform.position);
                haptics?.PulseLight("poke rearm blocked");
                return;
            }

            if (armed)
            {
                subtitles?.ShowLine("Rearmement deja verrouille.", 1.8f);
                haptics?.PulseLight("poke rearm already");
                return;
            }

            armed = true;
            SetFeedback(armedColor);
            audioFeedback?.PlaySocketCorrect(transform.position);
            haptics?.PulseMedium("poke rearm ok");
            subtitles?.ShowLine("Bouton de rearmement enfonce. Le tableau tient.", 3f);
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
