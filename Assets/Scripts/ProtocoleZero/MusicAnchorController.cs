using UnityEngine;

namespace ProtocoleZero
{
    public sealed class MusicAnchorController : MonoBehaviour
    {
        [SerializeField] private StressDirector stressDirector;
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private AudioSource playlistSource;
        [Tooltip("Secours si aucun clip n'est assigne : sinon le compte a rebours = duree reelle du morceau.")]
        [SerializeField] private float sleepAfterSeconds = 150f;
        [SerializeField] private float sleepingStressPerSecond = 3.5f;
        [SerializeField] private bool musicAwake = true;

        private float timer;
        private float sleepingSeconds;
        private bool runStarted;
        private bool warned60;
        private bool warned25;
        private bool warned8;

        public bool IsMusicAwake => musicAwake;
        public float RemainingSeconds => timer;

        /// <summary>Duree cumulee depuis que la musique s'est arretee (0 quand elle joue).</summary>
        public float SleepingSeconds => sleepingSeconds;

        // Le PC reste eveille exactement le temps du morceau : quand la musique
        // finit, la veille tombe. Pas de duree arbitraire.
        private float AwakeSeconds => playlistSource != null && playlistSource.clip != null
            ? playlistSource.clip.length
            : sleepAfterSeconds;

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

            if (playlistSource != null)
            {
                // Un seul passage du morceau par relance : pas de boucle.
                playlistSource.loop = false;
            }
        }

        private void Start()
        {
            // La playlist ne demarre qu'au vrai debut de partie (bouton du tutoriel) :
            // avant, seule l'ambiance de fond tourne. Sans ecran tutoriel, on part direct.
            if (FindFirstObjectByType<TutorialStartScreen>() != null)
            {
                musicAwake = false;
                if (playlistSource != null)
                {
                    playlistSource.Stop();
                }

                // Pas de penalite de stress tant que la partie n'a pas commence.
                stressDirector?.SetMusicActive(true);
            }
            else
            {
                BeginRun();
            }
        }

        /// <summary>Lance la playlist a zero au demarrage effectif de la partie.</summary>
        public void BeginRun()
        {
            if (runStarted)
            {
                return;
            }

            runStarted = true;
            timer = AwakeSeconds;
            sleepingSeconds = 0f;
            warned60 = false;
            warned25 = false;
            warned8 = false;
            if (playlistSource != null)
            {
                playlistSource.Stop();
            }

            ApplyMusicState(true);
        }

        /// <summary>Coupe net la playlist (sequence de screamer : le cri domine).</summary>
        public void CutForScream()
        {
            if (playlistSource != null)
            {
                playlistSource.Stop();
            }
        }

        private void Update()
        {
            if (!runStarted)
            {
                return;
            }

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
                sleepingSeconds += Time.deltaTime;
                stressDirector?.AddStress(sleepingStressPerSecond * Time.deltaTime, "music sleeping");
            }
        }

        public void TouchKeyboard()
        {
            WakeMusic();
        }

        public void WakeMusic()
        {
            // Toucher le clavier avant le depart officiel lance simplement la partie.
            if (!runStarted)
            {
                BeginRun();
                return;
            }

            // Repart du debut du morceau (sinon la reprise serait plus courte
            // que le compte a rebours affiche).
            if (playlistSource != null)
            {
                playlistSource.Stop();
            }

            timer = AwakeSeconds;
            sleepingSeconds = 0f;
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
