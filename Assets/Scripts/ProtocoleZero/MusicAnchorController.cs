using UnityEngine;

namespace ProtocoleZero
{
    public sealed class MusicAnchorController : MonoBehaviour
    {
        [SerializeField] private StressDirector stressDirector;
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private AudioSource playlistSource;
        [SerializeField] private float sleepAfterSeconds = 150f;
        [SerializeField] private float sleepingStressPerSecond = 3.5f;
        [SerializeField] private bool musicAwake = true;

        private float timer;
        private bool warned60;
        private bool warned25;
        private bool warned8;

        public bool IsMusicAwake => musicAwake;
        public float RemainingSeconds => timer;

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

            timer = sleepAfterSeconds;
            ApplyMusicState(true);
        }

        private void Update()
        {
            if (musicAwake)
            {
                timer -= Time.deltaTime;
                if (!warned60 && timer <= 60f)
                {
                    warned60 = true;
                    subtitles?.ShowLine("Le PC va se mettre en veille.", 3f);
                }

                if (!warned25 && timer <= 25f)
                {
                    warned25 = true;
                    subtitles?.ShowLine("La musique saute. Retourne a Mars si besoin.", 3f);
                }

                if (!warned8 && timer <= 8f)
                {
                    warned8 = true;
                    subtitles?.ShowLine("Encore quelques secondes avant la veille.", 3f);
                }

                if (timer <= 0f)
                {
                    SleepMusic();
                }
            }
            else
            {
                stressDirector?.AddStress(sleepingStressPerSecond * Time.deltaTime, "music sleeping");
            }
        }

        public void TouchKeyboard()
        {
            WakeMusic();
        }

        public void WakeMusic()
        {
            timer = sleepAfterSeconds;
            warned60 = false;
            warned25 = false;
            warned8 = false;
            ApplyMusicState(true);
            subtitles?.ShowLine("Playlist relancee. Respire.", 2.5f);
        }

        public void SleepMusic()
        {
            ApplyMusicState(false);
            subtitles?.ShowLine("La musique s'est arretee.", 3f);
        }

        private void ApplyMusicState(bool awake)
        {
            musicAwake = awake;
            stressDirector?.SetMusicActive(awake);

            if (playlistSource == null)
            {
                return;
            }

            if (awake)
            {
                if (!playlistSource.isPlaying)
                {
                    playlistSource.Play();
                }
            }
            else
            {
                playlistSource.Pause();
            }
        }
    }
}
