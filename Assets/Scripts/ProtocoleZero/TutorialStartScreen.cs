using UnityEngine;

namespace ProtocoleZero
{
    /// <summary>
    /// Start-of-game onboarding board: explains the story, the objective (restore power
    /// through the electrical panels) and the controls, and lets the player pick a
    /// locomotion mode before starting. Mode presses apply immediately so the player
    /// can try them; COMMENCER hides the board and launches the run.
    /// </summary>
    public sealed class TutorialStartScreen : MonoBehaviour
    {
        [SerializeField] private LocomotionModeManager locomotion;
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private AudioFeedbackRouter audioFeedback;
        [SerializeField] private HapticFeedbackRouter haptics;
        [SerializeField] private Renderer teleportButtonRenderer;
        [SerializeField] private Renderer continuousButtonRenderer;
        [SerializeField] private Renderer bothButtonRenderer;
        [SerializeField] private GameObject boardRoot;
        [SerializeField] private ComfortSettingsManager comfort;
        [SerializeField] private TextMesh seatedLabel;
        [SerializeField] private TextMesh subtitlesLabel;
        [SerializeField] private Color selectedColor = new Color(0.15f, 0.85f, 0.75f);
        [SerializeField] private Color unselectedColor = new Color(0.22f, 0.26f, 0.33f);

        private MaterialPropertyBlock block;
        private bool started;
        private bool highlightInitialized;
        private bool seatedOn;
        private bool subtitlesOn = true;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool GameStarted => started;

        private void Awake()
        {
            block = new MaterialPropertyBlock();

            if (locomotion == null)
            {
                locomotion = FindFirstObjectByType<LocomotionModeManager>();
            }

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

            if (boardRoot == null)
            {
                boardRoot = gameObject;
            }
        }

        private void Update()
        {
            // The locomotion manager applies its saved mode in Start; reflect it once here.
            if (!highlightInitialized && locomotion != null)
            {
                highlightInitialized = true;
                HighlightMode(locomotion.CurrentMode);
            }
        }

        public void HandleOption(TutorialOptionButton.Option option)
        {
            if (started)
            {
                return;
            }

            haptics?.PulseLight("tutorial button");

            switch (option)
            {
                case TutorialOptionButton.Option.ModeTeleport:
                    ApplyMode(LocomotionModeManager.Mode.Teleport, "Teleportation : vise un pad turquoise avec le stick, relache.");
                    break;
                case TutorialOptionButton.Option.ModeContinuous:
                    ApplyMode(LocomotionModeManager.Mode.Continuous, "Deplacement continu : stick gauche pour marcher.");
                    break;
                case TutorialOptionButton.Option.ModeBoth:
                    ApplyMode(LocomotionModeManager.Mode.Both, "Marche au stick gauche + teleport au stick droit.");
                    break;
                case TutorialOptionButton.Option.Start:
                    StartGame();
                    break;
                case TutorialOptionButton.Option.ToggleSeated:
                    seatedOn = !seatedOn;
                    if (comfort == null)
                    {
                        comfort = FindFirstObjectByType<ComfortSettingsManager>();
                    }

                    comfort?.SetSeated(seatedOn);
                    if (seatedLabel != null)
                    {
                        seatedLabel.text = "ASSIS : " + (seatedOn ? "OUI" : "NON");
                    }

                    audioFeedback?.PlayBraceletPulse();
                    subtitles?.ShowLine(seatedOn ? "Mode assis : hauteur ajustee." : "Mode debout retabli.", 2.5f);
                    break;
                case TutorialOptionButton.Option.ToggleSubtitles:
                    subtitlesOn = !subtitlesOn;
                    if (comfort == null)
                    {
                        comfort = FindFirstObjectByType<ComfortSettingsManager>();
                    }

                    comfort?.SetSubtitles(subtitlesOn);
                    if (subtitlesLabel != null)
                    {
                        subtitlesLabel.text = "SOUS-TITRES : " + (subtitlesOn ? "OUI" : "NON");
                    }

                    audioFeedback?.PlayBraceletPulse();
                    // only displays when subtitles were just re-enabled
                    subtitles?.ShowLine("Sous-titres actives.", 2f);
                    break;
            }
        }

        private void ApplyMode(LocomotionModeManager.Mode mode, string hint)
        {
            locomotion?.SetMode(mode);
            HighlightMode(mode);
            audioFeedback?.PlayBraceletPulse();
            subtitles?.ShowLine(hint, 3f);
        }

        private void StartGame()
        {
            started = true;
            audioFeedback?.PlayPcWake();
            haptics?.PulseMedium("tutorial start");

            if (boardRoot != null)
            {
                boardRoot.SetActive(false);
            }
        }

        private void HighlightMode(LocomotionModeManager.Mode mode)
        {
            SetButtonColor(teleportButtonRenderer, mode == LocomotionModeManager.Mode.Teleport);
            SetButtonColor(continuousButtonRenderer, mode == LocomotionModeManager.Mode.Continuous);
            SetButtonColor(bothButtonRenderer, mode == LocomotionModeManager.Mode.Both);
        }

        private void SetButtonColor(Renderer target, bool selected)
        {
            if (target == null)
            {
                return;
            }

            Color color = selected ? selectedColor : unselectedColor;
            target.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            target.SetPropertyBlock(block);
        }
    }
}
