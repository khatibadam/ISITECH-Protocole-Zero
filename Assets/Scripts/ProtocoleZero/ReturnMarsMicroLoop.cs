using UnityEngine;

namespace ProtocoleZero
{
    public sealed class ReturnMarsMicroLoop : MonoBehaviour
    {
        [SerializeField] private ElectricalPanelPuzzle infoPuzzle;
        [SerializeField] private MusicAnchorController musicAnchor;
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private EntityDirector entityDirector;
        [SerializeField] private GameObject returnCueGroup;
        [SerializeField] private Light[] marsLights;
        [SerializeField] private Color returnLightColor = new Color(0.62f, 0.9f, 1f);
        [SerializeField] private float returnLightIntensity = 2.1f;
        [SerializeField] private string subtitleLine = "Le PC de Mars repond encore. Retourne a l'ancre.";

        private bool triggered;

        private void Awake()
        {
            if (musicAnchor == null)
            {
                musicAnchor = FindFirstObjectByType<MusicAnchorController>();
            }

            if (subtitles == null)
            {
                subtitles = FindFirstObjectByType<SubtitleManager>();
            }

            if (entityDirector == null)
            {
                entityDirector = FindFirstObjectByType<EntityDirector>();
            }

            if (returnCueGroup != null)
            {
                returnCueGroup.SetActive(false);
            }
        }

        private void Update()
        {
            if (triggered || infoPuzzle == null || !infoPuzzle.IsSolved)
            {
                return;
            }

            TriggerReturnCue();
        }

        public void TriggerReturnCue()
        {
            if (triggered)
            {
                return;
            }

            triggered = true;
            if (returnCueGroup != null)
            {
                returnCueGroup.SetActive(true);
            }

            for (int i = 0; i < marsLights.Length; i++)
            {
                Light marsLight = marsLights[i];
                if (marsLight == null)
                {
                    continue;
                }

                marsLight.color = returnLightColor;
                marsLight.intensity = Mathf.Max(marsLight.intensity, returnLightIntensity);
            }

            musicAnchor?.SleepMusic();
            entityDirector?.ForceAnchor("E1", 3f);
            subtitles?.ShowLine(subtitleLine, 4f);
        }
    }
}
