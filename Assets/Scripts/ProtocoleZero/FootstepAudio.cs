using UnityEngine;

namespace ProtocoleZero
{
    // Bruits de pas : joue la boucle de pas tant que le rig se deplace a l'horizontale
    // (clavier ou locomotion VR). A poser sur la racine du rig joueur (XR Origin).
    // L'AudioSource est cree automatiquement, pas besoin d'en ajouter un.
    public sealed class FootstepAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip footstepClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
        [Tooltip("Vitesse horizontale (m/s) en dessous de laquelle on est considere immobile.")]
        [SerializeField] private float minSpeed = 0.3f;
        [Tooltip("Deplacement (m) en une frame au-dela duquel c'est un teleport : pas de bruit de pas.")]
        [SerializeField] private float teleportDelta = 0.75f;
        [SerializeField] private float fadeSpeed = 6f;

        private AudioSource source;
        private Vector3 lastPosition;
        private float currentGain;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = footstepClip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            lastPosition = transform.position;
        }

        private void Update()
        {
            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;
            delta.y = 0f;

            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            bool moving = delta.magnitude < teleportDelta && delta.magnitude / dt >= minSpeed;

            currentGain = Mathf.MoveTowards(currentGain, moving ? 1f : 0f, fadeSpeed * dt);
            source.volume = currentGain * volume;

            if (currentGain > 0f)
            {
                if (!source.isPlaying && footstepClip != null)
                {
                    // UnPause reprend la boucle la ou elle en etait ; Play si jamais demarree.
                    source.UnPause();
                    if (!source.isPlaying)
                    {
                        source.Play();
                    }
                }
            }
            else if (source.isPlaying)
            {
                source.Pause();
            }
        }
    }
}
