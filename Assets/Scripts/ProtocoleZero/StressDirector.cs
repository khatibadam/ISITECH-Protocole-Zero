using System;
using UnityEngine;

namespace ProtocoleZero
{
    public enum StressStage
    {
        Anchored,
        Altered,
        Panic,
        Crisis
    }

    public sealed class StressDirector : MonoBehaviour
    {
        public static StressDirector Instance { get; private set; }

        [Header("State")]
        [SerializeField, Range(0f, 100f)] private float stress = 12f;
        [SerializeField] private StressStage stage = StressStage.Anchored;

        [Header("Tuning")]
        [SerializeField] private float anchoredThreshold = 25f;
        [SerializeField] private float alteredThreshold = 55f;
        [SerializeField] private float panicThreshold = 80f;
        [SerializeField] private float passiveDecayPerSecond = 1.25f;
        [SerializeField] private float musicCalmPerSecond = 2.5f;
        [SerializeField] private float darkGainPerSecond = 3f;
        [SerializeField] private float crisisCapBeforeFinalChance = 95f;
        [SerializeField] private float puzzleGraceSeconds = 12f;
        [SerializeField] private bool reducedFearMode;

        private float graceTimer;
        private bool musicActive = true;
        private bool inDarkZone;

        public event Action<float, StressStage> StressChanged;

        public float Stress => stress;
        public StressStage Stage => stage;
        public bool IsInGrace => graceTimer > 0f;
        public bool MusicActive => musicActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RecalculateStage(true);
        }

        private void Update()
        {
            if (graceTimer > 0f)
            {
                graceTimer -= Time.deltaTime;
            }

            float delta = -passiveDecayPerSecond;
            if (musicActive)
            {
                delta -= musicCalmPerSecond;
            }

            if (inDarkZone && graceTimer <= 0f)
            {
                delta += darkGainPerSecond;
            }

            if (Mathf.Abs(delta) > 0.001f)
            {
                AddStress(delta * Time.deltaTime, "ambient");
            }
        }

        public void SetReducedFear(bool enabled)
        {
            reducedFearMode = enabled;
        }

        public void SetMusicActive(bool active)
        {
            if (musicActive == active)
            {
                return;
            }

            musicActive = active;
            AddStress(active ? -8f : 10f, active ? "music restored" : "music lost");
        }

        public void SetDarkZone(bool active)
        {
            inDarkZone = active;
        }

        public void AddStress(float amount, string reason)
        {
            if (reducedFearMode && amount > 0f)
            {
                amount *= 0.65f;
            }

            if (graceTimer > 0f && amount > 0f)
            {
                amount *= 0.35f;
            }

            float max = stage == StressStage.Crisis ? 100f : crisisCapBeforeFinalChance;
            float next = Mathf.Clamp(stress + amount, 0f, max);
            if (Mathf.Approximately(next, stress))
            {
                return;
            }

            stress = next;
            RecalculateStage(false);
        }

        public void Calm(float amount, string reason)
        {
            AddStress(-Mathf.Abs(amount), reason);
        }

        public void RegisterPuzzleSolved()
        {
            graceTimer = puzzleGraceSeconds;
            Calm(18f, "puzzle solved");
        }

        public void ForceCrisis(string reason)
        {
            stress = Mathf.Max(stress, panicThreshold + 2f);
            RecalculateStage(false);
        }

        private void RecalculateStage(bool forceNotify)
        {
            StressStage nextStage = StressStage.Anchored;
            if (stress >= panicThreshold)
            {
                nextStage = StressStage.Crisis;
            }
            else if (stress >= alteredThreshold)
            {
                nextStage = StressStage.Panic;
            }
            else if (stress >= anchoredThreshold)
            {
                nextStage = StressStage.Altered;
            }

            if (forceNotify || nextStage != stage)
            {
                stage = nextStage;
                StressChanged?.Invoke(stress, stage);
                return;
            }

            StressChanged?.Invoke(stress, stage);
        }
    }
}
