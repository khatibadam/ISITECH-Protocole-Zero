using UnityEngine;

namespace ProtocoleZero
{
    public sealed class WristAnchorUI : MonoBehaviour
    {
        [SerializeField] private TextMesh wristText;
        [SerializeField] private StressDirector stressDirector;
        [SerializeField] private MusicAnchorController musicAnchor;
        [SerializeField] private BatteryTimer batteryTimer;
        [SerializeField] private ProtocoleZeroGameFlow gameFlow;

        private void Awake()
        {
            if (stressDirector == null)
            {
                stressDirector = FindFirstObjectByType<StressDirector>();
            }

            if (musicAnchor == null)
            {
                musicAnchor = FindFirstObjectByType<MusicAnchorController>();
            }

            if (batteryTimer == null)
            {
                batteryTimer = FindFirstObjectByType<BatteryTimer>();
            }

            if (gameFlow == null)
            {
                gameFlow = FindFirstObjectByType<ProtocoleZeroGameFlow>();
            }
        }

        private void Update()
        {
            if (wristText == null)
            {
                return;
            }

            string heart = stressDirector != null ? StressLabel(stressDirector.Stage) : "COEUR ?";
            string music = musicAnchor != null && musicAnchor.IsMusicAwake ? "MUSIQUE ON" : "MUSIQUE OFF";
            string battery = batteryTimer != null ? Mathf.RoundToInt(batteryTimer.BatteryPercent) + "%" : "--%";
            string objective = gameFlow != null ? gameFlow.CurrentObjective : "Mars";

            wristText.text = heart + "\n" + music + "  " + battery + "\n" + objective;
        }

        private static string StressLabel(StressStage stage)
        {
            switch (stage)
            {
                case StressStage.Altered:
                    return "COEUR HAUT";
                case StressStage.Panic:
                    return "RESPIRE";
                case StressStage.Crisis:
                    return "CRISE";
                default:
                    return "ANCRE";
            }
        }
    }
}
