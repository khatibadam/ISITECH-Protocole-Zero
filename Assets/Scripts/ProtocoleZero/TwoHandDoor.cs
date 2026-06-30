using UnityEngine;

namespace ProtocoleZero
{
    public sealed class TwoHandDoor : MonoBehaviour
    {
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float handleLatchSeconds = 2f;
        [SerializeField] private float openYaw = -85f;
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private ProtocoleZeroGameFlow gameFlow;
        [SerializeField] private AudioFeedbackRouter audioFeedback;
        [SerializeField] private HapticFeedbackRouter haptics;

        private float leftLatch;
        private float rightLatch;
        private bool opened;
        private Quaternion closedRotation;

        public bool IsOpened => opened;

        private void Awake()
        {
            if (doorPivot == null)
            {
                doorPivot = transform;
            }

            closedRotation = doorPivot.localRotation;

            if (subtitles == null)
            {
                subtitles = FindFirstObjectByType<SubtitleManager>();
            }

            if (gameFlow == null)
            {
                gameFlow = FindFirstObjectByType<ProtocoleZeroGameFlow>();
            }

            if (audioFeedback == null)
            {
                audioFeedback = FindFirstObjectByType<AudioFeedbackRouter>();
            }

            if (haptics == null)
            {
                haptics = FindFirstObjectByType<HapticFeedbackRouter>();
            }
        }

        private void Update()
        {
            if (opened)
            {
                return;
            }

            leftLatch = Mathf.Max(0f, leftLatch - Time.deltaTime);
            rightLatch = Mathf.Max(0f, rightLatch - Time.deltaTime);

            if (leftLatch > 0f && rightLatch > 0f)
            {
                Open();
            }
        }

        public void PulseLeftHandle()
        {
            leftLatch = handleLatchSeconds;
            haptics?.PulseLight("door left handle");
            subtitles?.ShowLine("Main gauche en place.", 1.2f);
        }

        public void PulseRightHandle()
        {
            rightLatch = handleLatchSeconds;
            haptics?.PulseLight("door right handle");
            subtitles?.ShowLine("Main droite en place.", 1.2f);
        }

        public void Open()
        {
            if (opened)
            {
                return;
            }

            if (gameFlow != null && !gameFlow.AllRequiredPuzzlesSolved)
            {
                haptics?.PulseLight("door locked");
                subtitles?.ShowLine("Le verrou attend encore le courant.", 2.5f);
                return;
            }

            opened = true;
            doorPivot.localRotation = closedRotation * Quaternion.Euler(0f, openYaw, 0f);
            Collider doorCollider = doorPivot.GetComponent<Collider>();
            if (doorCollider != null)
            {
                doorCollider.isTrigger = true;
            }

            subtitles?.ShowLine("La sortie etait ouverte.", 4f);
            audioFeedback?.PlayFinalDoorOpen();
            haptics?.PulseStrong("door open");
            gameFlow?.TriggerEnding();
        }
    }
}
