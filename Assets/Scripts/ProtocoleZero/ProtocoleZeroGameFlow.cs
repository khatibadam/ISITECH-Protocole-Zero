using UnityEngine;

namespace ProtocoleZero
{
    public sealed class ProtocoleZeroGameFlow : MonoBehaviour
    {
        [SerializeField] private ElectricalPanelPuzzle[] requiredPuzzles;
        [SerializeField] private TwoHandDoor finalDoor;
        [SerializeField] private MusicAnchorController musicAnchor;
        [SerializeField] private BatteryTimer batteryTimer;
        [SerializeField] private StressDirector stressDirector;
        [SerializeField] private EntityDirector entityDirector;
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private TextMesh pcScreenText;
        [SerializeField] private GameObject revealGroup;
        [SerializeField] private Light[] revealLights;
        [SerializeField, Min(0f)] private float revealLightIntensity = 2f;
        [SerializeField, Min(0f)] private float revealLightRange = 4.5f;

        private bool endingTriggered;
        private string currentObjective = "Mars : rallume le PC";
        private float pcScreenRefreshTimer;

        public string CurrentObjective => currentObjective;
        public bool AllRequiredPuzzlesSolved => CountSolvedPuzzles() >= CountRequiredPuzzles();

        private void Awake()
        {
            if (requiredPuzzles == null || requiredPuzzles.Length == 0)
            {
                requiredPuzzles = FindObjectsByType<ElectricalPanelPuzzle>(FindObjectsSortMode.None);
            }

            if (finalDoor == null)
            {
                finalDoor = FindFirstObjectByType<TwoHandDoor>();
            }

            if (musicAnchor == null)
            {
                musicAnchor = FindFirstObjectByType<MusicAnchorController>();
            }

            if (batteryTimer == null)
            {
                batteryTimer = FindFirstObjectByType<BatteryTimer>();
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

            if (revealGroup != null)
            {
                revealGroup.SetActive(false);
            }
        }

        private void Start()
        {
            subtitles?.ShowLine("Deadline rendue. Trouve le courant. Reste avec la musique.", 4f);
        }

        private void Update()
        {
            if (!endingTriggered)
            {
                UpdateObjective();
            }

            pcScreenRefreshTimer -= Time.deltaTime;
            if (pcScreenRefreshTimer <= 0f)
            {
                pcScreenRefreshTimer = 0.25f;
                UpdatePcScreen();
            }
        }

        public void TriggerEnding()
        {
            if (endingTriggered)
            {
                return;
            }

            endingTriggered = true;
            currentObjective = "Sortie ouverte";
            revealGroup?.SetActive(true);

            if (revealLights != null)
            {
                for (int i = 0; i < revealLights.Length; i++)
                {
                    if (revealLights[i] != null)
                    {
                        revealLights[i].enabled = true;
                        revealLights[i].intensity = revealLightIntensity;
                        revealLights[i].range = revealLightRange;
                    }
                }
            }

            stressDirector?.Calm(100f, "ending");
            entityDirector?.RegisterPuzzleSolved();
            UpdatePcScreen();
            subtitles?.ShowLine("Le batiment n'etait pas ferme. Tu peux rentrer.", 6f);
        }

        private void UpdateObjective()
        {
            int solved = CountSolvedPuzzles();
            int total = CountRequiredPuzzles();

            if (solved <= 0)
            {
                currentObjective = "Mars : branche le BT";
            }
            else if (solved < total)
            {
                currentObjective = "Local serveur : BT " + (solved + 1) + "/" + total;
            }
            else
            {
                currentObjective = "Hall : ouvre a deux mains";
            }
        }

        private void UpdatePcScreen()
        {
            if (pcScreenText == null)
            {
                return;
            }

            int battery = batteryTimer != null ? Mathf.RoundToInt(batteryTimer.BatteryPercent) : 97;
            string music = musicAnchor != null && musicAnchor.IsMusicAwake ? "PLAYLIST ACTIVE" : "VEILLE";
            string stress = stressDirector != null ? stressDirector.Stage.ToString().ToUpperInvariant() : "ANCHORED";
            pcScreenText.text = "ISITECH PROTOCOLE ZERO\n" + music + " / " + battery + "%\n" + currentObjective + "\nEtat interne : " + stress;
        }

        private int CountRequiredPuzzles()
        {
            return requiredPuzzles != null ? requiredPuzzles.Length : 0;
        }

        private int CountSolvedPuzzles()
        {
            int solved = 0;
            int total = CountRequiredPuzzles();
            for (int i = 0; i < total; i++)
            {
                if (requiredPuzzles[i] != null && requiredPuzzles[i].IsSolved)
                {
                    solved++;
                }
            }

            return solved;
        }
    }
}
