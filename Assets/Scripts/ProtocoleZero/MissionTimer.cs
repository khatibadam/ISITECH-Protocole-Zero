using System;
using UnityEngine;

namespace ProtocoleZero
{
    public sealed class MissionTimer : MonoBehaviour
    {
        public static MissionTimer Instance { get; private set; }

        [Header("Duree")]
        [SerializeField, Min(1f)] private float totalSeconds = 480f;
        [SerializeField] private bool runOnStart = true;

        [Header("Alertes (secondes restantes)")]
        [SerializeField] private float warnEarlySeconds = 120f;
        [SerializeField] private float warnLastMinuteSeconds = 60f;
        [SerializeField] private float warnCriticalSeconds = 20f;
        [SerializeField] private float criticalStressPerSecond = 2.5f;

        [Header("Refs (auto si vide)")]
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private StressDirector stressDirector;

        private float remaining;
        private bool running;
        private bool expired;
        private bool warnedEarly;
        private bool warnedLastMinute;
        private bool warnedCritical;

        public event Action Expired;

        public float RemainingSeconds => Mathf.Max(0f, remaining);
        public float TotalSeconds => totalSeconds;
        public bool IsRunning => running;
        public bool IsExpired => expired;
        public float Normalized => totalSeconds > 0f ? Mathf.Clamp01(RemainingSeconds / totalSeconds) : 0f;
        public string FormattedTime => Format(RemainingSeconds);

        public static MissionTimer EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            MissionTimer found = FindFirstObjectByType<MissionTimer>();
            if (found != null)
            {
                return found;
            }

            return new GameObject("MissionTimer").AddComponent<MissionTimer>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            remaining = totalSeconds;
            running = runOnStart;

            if (subtitles == null)
            {
                subtitles = FindFirstObjectByType<SubtitleManager>();
            }

            if (stressDirector == null)
            {
                stressDirector = FindFirstObjectByType<StressDirector>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!running || expired)
            {
                return;
            }

            remaining -= Time.deltaTime;

            if (!warnedEarly && remaining <= warnEarlySeconds)
            {
                warnedEarly = true;
                subtitles?.ShowLine("Il te reste " + Format(remaining) + ". Avance.", 3f);
            }

            if (!warnedLastMinute && remaining <= warnLastMinuteSeconds)
            {
                warnedLastMinute = true;
                subtitles?.ShowLine("Une minute. Termine le protocole.", 3f);
                stressDirector?.AddStress(6f, "mission timer 60s");
            }

            if (!warnedCritical && remaining <= warnCriticalSeconds)
            {
                warnedCritical = true;
                subtitles?.ShowLine("Le temps s'effondre.", 3f);
                stressDirector?.AddStress(10f, "mission timer 20s");
            }

            if (remaining <= warnCriticalSeconds && remaining > 0f)
            {
                stressDirector?.AddStress(criticalStressPerSecond * Time.deltaTime, "mission timer critical");
            }

            if (remaining <= 0f)
            {
                remaining = 0f;
                expired = true;
                running = false;
                subtitles?.ShowLine("Temps ecoule. Sors, maintenant.", 5f);
                stressDirector?.ForceCrisis("mission timer expired");
                Expired?.Invoke();
            }
        }

        public void Pause()
        {
            running = false;
        }

        public void Resume()
        {
            if (!expired)
            {
                running = true;
            }
        }

        public void SetRunning(bool value)
        {
            if (value)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void AddTime(float seconds)
        {
            remaining = Mathf.Clamp(remaining + seconds, 0f, totalSeconds);
            if (remaining > 0f)
            {
                expired = false;
            }
        }

        public void ResetTimer(float newTotalSeconds = -1f)
        {
            if (newTotalSeconds > 0f)
            {
                totalSeconds = newTotalSeconds;
            }

            remaining = totalSeconds;
            expired = false;
            running = runOnStart;
            warnedEarly = false;
            warnedLastMinute = false;
            warnedCritical = false;
        }

        public static string Format(float seconds)
        {
            if (seconds < 0f)
            {
                seconds = 0f;
            }

            int total = Mathf.CeilToInt(seconds);
            int minutes = total / 60;
            int secs = total % 60;
            return minutes.ToString("00") + ":" + secs.ToString("00");
        }
    }
}
