using System.Collections;
using UnityEngine;

namespace ProtocoleZero
{
    /// <summary>
    /// The real exit elevator (right ASC door in the hall). Stays dead until every
    /// required electrical panel is solved; then its call button glows green. Pressing
    /// it slides the door open onto a lit cabin; stepping inside closes the door and
    /// hands over to the clean GameEnding sequence.
    /// </summary>
    public sealed class ElevatorExit : MonoBehaviour
    {
        [SerializeField] private ProtocoleZeroGameFlow gameFlow;
        [SerializeField] private GameEnding ending;
        [SerializeField] private Transform doorPanel;
        [SerializeField] private Transform doorSplit;
        [SerializeField] private Renderer callButtonRenderer;
        [SerializeField] private Light cabinLight;
        [SerializeField] private AudioSource chime;
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private HapticFeedbackRouter haptics;
        [SerializeField] private Color deadColor = new Color(0.25f, 0.1f, 0.1f);
        [SerializeField] private Color poweredColor = new Color(0.15f, 0.95f, 0.35f);
        [SerializeField, Min(0.1f)] private float slideDistance = 1f;
        [SerializeField, Min(0.05f)] private float slideSpeed = 0.8f;

        private bool powered;
        private bool doorOpen;
        private bool cabinReady;
        private bool departed;
        private Vector3 doorClosedPos;
        private Vector3 splitClosedPos;
        private MaterialPropertyBlock block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool IsPowered => powered;
        public bool IsDoorOpen => doorOpen;

        private void Awake()
        {
            block = new MaterialPropertyBlock();

            if (gameFlow == null)
            {
                gameFlow = FindFirstObjectByType<ProtocoleZeroGameFlow>();
            }

            if (ending == null)
            {
                ending = FindFirstObjectByType<GameEnding>();
            }

            if (subtitles == null)
            {
                subtitles = FindFirstObjectByType<SubtitleManager>();
            }

            if (haptics == null)
            {
                haptics = FindFirstObjectByType<HapticFeedbackRouter>();
            }

            if (doorPanel != null)
            {
                doorClosedPos = doorPanel.localPosition;
            }

            if (doorSplit != null)
            {
                splitClosedPos = doorSplit.localPosition;
            }

            if (cabinLight != null)
            {
                cabinLight.enabled = false;
            }

            SetCallButtonColor(deadColor);
        }

        private void Update()
        {
            if (!powered && gameFlow != null && gameFlow.AllRequiredPuzzlesSolved)
            {
                powered = true;
                SetCallButtonColor(poweredColor);
                PlayChime();
                subtitles?.ShowLine("L'ascenseur est alimente.", 3f);
            }
        }

        public void CallPressed()
        {
            haptics?.PulseLight("elevator call");

            if (!powered)
            {
                subtitles?.ShowLine("Aucun courant. Le tableau attend ses cables.", 2.5f);
                return;
            }

            if (doorOpen || departed)
            {
                return;
            }

            doorOpen = true;
            StartCoroutine(SlideDoor(true));
        }

        public void PlayerEnteredCabin()
        {
            // cabinReady only turns true once the door has finished sliding open, so a
            // player clipping the sensor through a half-open door can never soft-lock.
            if (!cabinReady || departed || ending == null || ending.EndingStarted)
            {
                return;
            }

            departed = true;
            StartCoroutine(DepartRoutine());
        }

        private IEnumerator DepartRoutine()
        {
            yield return SlideDoor(false);
            PlayChime();
            ending.BeginEnding("ascenseur");
        }

        private IEnumerator SlideDoor(bool open)
        {
            if (doorPanel == null)
            {
                yield break;
            }

            PlayChime();

            if (cabinLight != null && open)
            {
                cabinLight.enabled = true;
            }

            Vector3 doorTarget = open ? doorClosedPos + Vector3.forward * slideDistance : doorClosedPos;
            Vector3 splitTarget = open ? splitClosedPos + Vector3.forward * slideDistance : splitClosedPos;

            while (Vector3.Distance(doorPanel.localPosition, doorTarget) > 0.005f)
            {
                doorPanel.localPosition = Vector3.MoveTowards(doorPanel.localPosition, doorTarget, slideSpeed * Time.deltaTime);
                if (doorSplit != null)
                {
                    doorSplit.localPosition = Vector3.MoveTowards(doorSplit.localPosition, splitTarget, slideSpeed * Time.deltaTime);
                }

                yield return null;
            }

            doorPanel.localPosition = doorTarget;
            if (doorSplit != null)
            {
                doorSplit.localPosition = splitTarget;
            }

            if (open)
            {
                cabinReady = true;
            }
        }

        private void PlayChime()
        {
            if (chime != null)
            {
                chime.Stop();
                chime.Play();
            }
        }

        private void SetCallButtonColor(Color color)
        {
            if (callButtonRenderer == null)
            {
                return;
            }

            callButtonRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            callButtonRenderer.SetPropertyBlock(block);
        }
    }
}
