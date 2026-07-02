using UnityEngine;
using UnityEngine.Events;

namespace ProtocoleZero
{
    public sealed class ElectricalPanelPuzzle : MonoBehaviour
    {
        [SerializeField] private string puzzleId = "Mars";
        [SerializeField] private ElectricalSocket[] sockets;
        [SerializeField] private CablePlug[] plugs;
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Light statusLight;
        [SerializeField] private TextMesh statusText;
        [SerializeField] private StressDirector stressDirector;
        [SerializeField] private EntityDirector entityDirector;
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private HapticFeedbackRouter haptics;
        [SerializeField] private AudioFeedbackRouter audioFeedback;
        [SerializeField] private UnityEvent onSolved;

        private bool solved;
        private MaterialPropertyBlock statusBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public string PuzzleId => puzzleId;
        public bool IsSolved => solved;

        private void Awake()
        {
            statusBlock = new MaterialPropertyBlock();

            if (sockets == null || sockets.Length == 0)
            {
                sockets = GetComponentsInChildren<ElectricalSocket>();
            }

            if (plugs == null || plugs.Length == 0)
            {
                plugs = GetComponentsInChildren<CablePlug>();
            }

            if (stressDirector == null)
            {
                stressDirector = FindFirstObjectByType<StressDirector>();
            }

            if (entityDirector == null)
            {
                entityDirector = FindFirstObjectByType<EntityDirector>();
            }

            if (subtitles == null)
            {
                subtitles = FindFirstObjectByType<SubtitleManager>();
            }

            if (haptics == null)
            {
                haptics = FindFirstObjectByType<HapticFeedbackRouter>();
            }

            if (audioFeedback == null)
            {
                audioFeedback = FindFirstObjectByType<AudioFeedbackRouter>();
            }

            SetPanelFeedback(new Color(1f, 0.65f, 0.1f), "BT " + puzzleId + " / A brancher");
        }

        public void NotifySocketSolved(ElectricalSocket socket)
        {
            if (solved)
            {
                return;
            }

            haptics?.PulseMedium("socket ok");
            audioFeedback?.PlaySocketCorrect(socket != null ? socket.transform.position : transform.position);
            SetPanelFeedback(Color.cyan, "BT " + puzzleId + " / Signal OK");
            if (AllSocketsSolved())
            {
                Solve();
            }
        }

        public void NotifyWrongSocket(ElectricalSocket socket)
        {
            if (solved)
            {
                return;
            }

            haptics?.PulseLight("socket wrong");
            audioFeedback?.PlaySocketWrong(socket != null ? socket.transform.position : transform.position);
            stressDirector?.AddStress(6f, "wrong cable");
            subtitles?.ShowLine("Mauvais connecteur. Regarde les couleurs.", 2.5f);
            SetPanelFeedback(Color.red, "BT " + puzzleId + " / Erreur");
        }

        public void ForceSolve()
        {
            if (solved)
            {
                return;
            }

            for (int i = 0; i < sockets.Length; i++)
            {
                ElectricalSocket socket = sockets[i];
                if (socket == null || socket.IsSolved)
                {
                    continue;
                }

                socket.ForceSolved(FindPlug(socket.TargetPlugId));
            }

            Solve();
        }

        private CablePlug FindPlug(string plugId)
        {
            for (int i = 0; i < plugs.Length; i++)
            {
                if (plugs[i] != null && plugs[i].PlugId == plugId)
                {
                    return plugs[i];
                }
            }

            return null;
        }

        private bool AllSocketsSolved()
        {
            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i] != null && !sockets[i].IsSolved)
                {
                    return false;
                }
            }

            return true;
        }

        private void Solve()
        {
            if (solved)
            {
                return;
            }

            solved = true;
            SetPanelFeedback(Color.green, "BT " + puzzleId + " / Retabli");
            stressDirector?.RegisterPuzzleSolved();
            entityDirector?.RegisterPuzzleSolved();
            haptics?.PulseStrong("puzzle solved");
            audioFeedback?.PlayBraceletPulse();
            subtitles?.ShowLine("Courant retabli dans " + puzzleId + ".", 3f);
            onSolved?.Invoke();
        }

        private void SetPanelFeedback(Color color, string label)
        {
            if (statusRenderer != null)
            {
                statusRenderer.GetPropertyBlock(statusBlock);
                statusBlock.SetColor(BaseColorId, color);
                statusBlock.SetColor(ColorId, color);
                statusRenderer.SetPropertyBlock(statusBlock);
            }

            if (statusLight != null)
            {
                statusLight.color = color;
                statusLight.enabled = true;
            }

            if (statusText != null)
            {
                statusText.text = label;
            }
        }
    }
}
