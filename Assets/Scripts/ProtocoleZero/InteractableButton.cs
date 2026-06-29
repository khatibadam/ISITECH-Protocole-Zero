using UnityEngine;

namespace ProtocoleZero
{
    [RequireComponent(typeof(Collider))]
    public sealed class InteractableButton : MonoBehaviour
    {
        public enum ActionKind
        {
            WakeMusic,
            ForceSolvePuzzle,
            DoorLeft,
            DoorRight,
            TriggerEnding,
            AddStress,
            Calm
        }

        [SerializeField] private ActionKind action;
        [SerializeField] private MusicAnchorController musicAnchor;
        [SerializeField] private ElectricalPanelPuzzle puzzle;
        [SerializeField] private TwoHandDoor door;
        [SerializeField] private StressDirector stressDirector;
        [SerializeField] private ProtocoleZeroGameFlow gameFlow;
        [SerializeField] private HapticFeedbackRouter haptics;
        [SerializeField] private AudioFeedbackRouter audioFeedback;
        [SerializeField] private float stressAmount = 8f;
        [SerializeField] private float cooldownSeconds = 0.4f;

        private float cooldown;

        private void Awake()
        {
            if (musicAnchor == null)
            {
                musicAnchor = FindFirstObjectByType<MusicAnchorController>();
            }

            if (stressDirector == null)
            {
                stressDirector = FindFirstObjectByType<StressDirector>();
            }

            if (gameFlow == null)
            {
                gameFlow = FindFirstObjectByType<ProtocoleZeroGameFlow>();
            }

            if (haptics == null)
            {
                haptics = FindFirstObjectByType<HapticFeedbackRouter>();
            }

            if (audioFeedback == null)
            {
                audioFeedback = FindFirstObjectByType<AudioFeedbackRouter>();
            }
        }

        private void Update()
        {
            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);
        }

        private void OnMouseDown()
        {
            Activate();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<SimplePlayerController>() != null || other.name.Contains("Hand") || other.name.Contains("Controller"))
            {
                Activate();
            }
        }

        public void Activate()
        {
            if (cooldown > 0f)
            {
                return;
            }

            cooldown = cooldownSeconds;
            haptics?.PulseLight("button");

            switch (action)
            {
                case ActionKind.WakeMusic:
                    audioFeedback?.PlayPcWake();
                    musicAnchor?.WakeMusic();
                    break;
                case ActionKind.ForceSolvePuzzle:
                    puzzle?.ForceSolve();
                    break;
                case ActionKind.DoorLeft:
                    audioFeedback?.PlayBraceletPulse();
                    door?.PulseLeftHandle();
                    break;
                case ActionKind.DoorRight:
                    audioFeedback?.PlayBraceletPulse();
                    door?.PulseRightHandle();
                    break;
                case ActionKind.TriggerEnding:
                    gameFlow?.TriggerEnding();
                    break;
                case ActionKind.AddStress:
                    stressDirector?.AddStress(stressAmount, "button stress");
                    break;
                case ActionKind.Calm:
                    audioFeedback?.PlayBraceletPulse();
                    stressDirector?.Calm(stressAmount, "button calm");
                    break;
            }
        }
    }
}
