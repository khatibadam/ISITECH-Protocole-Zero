using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace ProtocoleZero
{
    /// <summary>
    /// Clean end-of-game sequence shared by the two real exits (final door walked
    /// through, or the elevator once the power is back): final line, fade to black
    /// inside the headset, move the rig to the calm end room, fade back in front of
    /// the FIN board showing the mission time, with a restart button.
    /// </summary>
    public sealed class GameEnding : MonoBehaviour
    {
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Renderer fadeRenderer;
        [SerializeField] private Transform endRoomSpawn;
        [SerializeField] private TextMesh endStatsText;
        [SerializeField] private ProtocoleZeroGameFlow gameFlow;
        [SerializeField] private MissionTimer missionTimer;
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private StressDirector stressDirector;
        [SerializeField] private AudioFeedbackRouter audioFeedback;
        [SerializeField, Min(0.2f)] private float fadeSeconds = 2f;

        private bool endingStarted;
        private MaterialPropertyBlock fadeBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool EndingStarted => endingStarted;

        private void Awake()
        {
            fadeBlock = new MaterialPropertyBlock();

            if (xrOrigin == null)
            {
                xrOrigin = FindFirstObjectByType<XROrigin>();
            }

            if (gameFlow == null)
            {
                gameFlow = FindFirstObjectByType<ProtocoleZeroGameFlow>();
            }

            if (missionTimer == null)
            {
                missionTimer = FindFirstObjectByType<MissionTimer>();
            }

            if (subtitles == null)
            {
                subtitles = FindFirstObjectByType<SubtitleManager>();
            }

            if (stressDirector == null)
            {
                stressDirector = FindFirstObjectByType<StressDirector>();
            }

            if (audioFeedback == null)
            {
                audioFeedback = FindFirstObjectByType<AudioFeedbackRouter>();
            }

            if (fadeRenderer != null)
            {
                fadeRenderer.gameObject.SetActive(false);
            }
        }

        public void BeginEnding(string exitKind)
        {
            if (endingStarted)
            {
                return;
            }

            endingStarted = true;
            StartCoroutine(EndingRoutine(exitKind));
        }

        private IEnumerator EndingRoutine(string exitKind)
        {
            gameFlow?.TriggerEnding();
            stressDirector?.Calm(100f, "ending sequence");

            string line = exitKind == "ascenseur"
                ? "Niveau 0. Tu peux rentrer chez toi."
                : "Tu etais dehors. La porte n'etait jamais fermee. Respire.";
            subtitles?.ShowLine(line, 4.5f);

            yield return new WaitForSeconds(2.2f);
            yield return Fade(0f, 1f);

            MoveRigToEndRoom();
            UpdateStats(exitKind);

            yield return new WaitForSeconds(0.5f);
            yield return Fade(1f, 0f);

            if (fadeRenderer != null)
            {
                fadeRenderer.gameObject.SetActive(false);
            }

            audioFeedback?.PlayBraceletPulse();
            subtitles?.ShowLine("Merci d'avoir joue.", 5f);
        }

        private void MoveRigToEndRoom()
        {
            if (xrOrigin == null || endRoomSpawn == null)
            {
                return;
            }

            CharacterController controller = xrOrigin.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            xrOrigin.transform.SetPositionAndRotation(endRoomSpawn.position, endRoomSpawn.rotation);

            if (controller != null)
            {
                controller.enabled = true;
            }
        }

        private void UpdateStats(string exitKind)
        {
            if (endStatsText == null)
            {
                return;
            }

            // The timer is created lazily by the game flow, so resolve it at use time
            // and report the elapsed time (FormattedTime is the remaining countdown).
            if (missionTimer == null)
            {
                missionTimer = MissionTimer.Instance;
            }

            string time = missionTimer != null
                ? MissionTimer.Format(missionTimer.TotalSeconds - missionTimer.RemainingSeconds)
                : "--:--";
            string exitLabel = exitKind == "ascenseur" ? "Sortie : ascenseur" : "Sortie : porte du hall";
            endStatsText.text = "Temps de mission : " + time + "\n" + exitLabel + "\nLe batiment n'etait jamais ferme.";
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeRenderer == null)
            {
                yield break;
            }

            fadeRenderer.gameObject.SetActive(true);
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.deltaTime;
                SetFadeAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeSeconds)));
                yield return null;
            }

            SetFadeAlpha(to);
        }

        private void SetFadeAlpha(float alpha)
        {
            Color color = new Color(0f, 0f, 0f, alpha);
            fadeRenderer.GetPropertyBlock(fadeBlock);
            fadeBlock.SetColor(BaseColorId, color);
            fadeBlock.SetColor(ColorId, color);
            fadeRenderer.SetPropertyBlock(fadeBlock);
        }
    }
}
