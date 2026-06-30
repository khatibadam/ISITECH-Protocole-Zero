using UnityEngine;

namespace ProtocoleZero
{
    public sealed class BatteryTimer : MonoBehaviour
    {
        [SerializeField, Range(0f, 100f)] private float batteryPercent = 97f;
        [SerializeField] private float fullDrainSeconds = 480f;
        [SerializeField] private float criticalThreshold = 18f;
        [SerializeField] private StressDirector stressDirector;
        [SerializeField] private SubtitleManager subtitles;

        private bool criticalAnnounced;
        private bool emptyAnnounced;

        public float BatteryPercent => batteryPercent;
        public bool IsCritical => batteryPercent <= criticalThreshold;
        public bool IsEmpty => batteryPercent <= 0f;

        private void Awake()
        {
            if (stressDirector == null)
            {
                stressDirector = FindFirstObjectByType<StressDirector>();
            }

            if (subtitles == null)
            {
                subtitles = FindFirstObjectByType<SubtitleManager>();
            }
        }

        private void Update()
        {
            if (batteryPercent <= 0f)
            {
                return;
            }

            batteryPercent = Mathf.Max(0f, batteryPercent - (100f / Mathf.Max(1f, fullDrainSeconds)) * Time.deltaTime);

            if (!criticalAnnounced && batteryPercent <= criticalThreshold)
            {
                criticalAnnounced = true;
                subtitles?.ShowLine("Batterie faible. Reste calme. Termine le protocole.", 4f);
                stressDirector?.AddStress(8f, "battery critical");
            }

            if (!emptyAnnounced && batteryPercent <= 0f)
            {
                emptyAnnounced = true;
                subtitles?.ShowLine("Derniere chance. Ouvre la sortie.", 4f);
                stressDirector?.ForceCrisis("battery empty");
            }
        }

        public void RechargeTo(float percent)
        {
            batteryPercent = Mathf.Clamp(percent, 0f, 100f);
            emptyAnnounced = batteryPercent <= 0f;
            criticalAnnounced = batteryPercent <= criticalThreshold;
        }
    }
}
