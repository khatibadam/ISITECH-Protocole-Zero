using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProtocoleZero
{
    /// <summary>
    /// Rend une entite tangible et dangereuse. Au premier reveil, genere sur la racine
    /// un Rigidbody cinematique et deux capsules dimensionnees sur le mesh : une solide
    /// (le joueur ne peut plus la traverser) et une declencheuse legerement plus large
    /// (zone de contact). Au contact du joueur : cri, callback optionnel, et si demande
    /// la partie redemarre apres un court delai (defaite).
    /// Configure a l'execution par EntityDirector (errant) et ElevatorJumpscare (gardien).
    /// </summary>
    public sealed class EntityContactScare : MonoBehaviour
    {
        [Tooltip("Delai apres apparition avant que le contact compte (evite une mort instantanee si l'entite surgit collee au joueur).")]
        [SerializeField, Min(0f)] private float armDelay = 0.75f;
        [SerializeField, Min(0.5f)] private float touchCooldown = 3f;

        private AudioClip screamClip;
        private float screamVolume = 1f;
        private bool restartGame;
        private float restartDelay = 1.6f;
        private System.Action onTouched;

        private bool collidersReady;
        private float enabledAt;
        private float lastTouchTime = float.NegativeInfinity;
        private bool restarting;
        private Vector3 headLocalOffset = Vector3.up * 1.6f;

        /// <summary>Vrai pendant la sequence de defaite : les directeurs d'entites doivent se figer.</summary>
        public static bool DeathSequenceActive { get; private set; }

        internal static void SetDeathSequenceActive(bool active)
        {
            DeathSequenceActive = active;
        }

        /// <summary>Active/desactive la defaite au contact (ex : chasse finale).</summary>
        public void SetRestartOnTouch(bool restart)
        {
            restartGame = restart;
        }

        public void Configure(AudioClip clip, float volume, bool restart, float delay, System.Action touchedCallback = null)
        {
            screamClip = clip;
            screamVolume = Mathf.Clamp01(volume);
            restartGame = restart;
            restartDelay = Mathf.Max(0.2f, delay);
            onTouched = touchedCallback;
        }

        private void Awake()
        {
            // Statique : on repart propre apres le rechargement de la scene.
            DeathSequenceActive = false;
            EnsureColliders();
        }

        private void OnEnable()
        {
            enabledAt = Time.time;
        }

        // Capsules construites sur les bounds des renderers enfants pour coller a la
        // vraie taille du monstre, quel que soit le pivot de l'objet racine.
        private void EnsureColliders()
        {
            if (collidersReady)
            {
                return;
            }

            collidersReady = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;

            Vector3 localCenter = Vector3.up;
            float height = 2f;
            float radius = 0.35f;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                localCenter = transform.InverseTransformPoint(bounds.center);
                height = Mathf.Max(1f, bounds.size.y);
                radius = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z) * 0.3f, 0.22f, 0.6f);
            }

            headLocalOffset = localCenter + Vector3.up * (height * 0.35f);

            CapsuleCollider solid = gameObject.AddComponent<CapsuleCollider>();
            solid.direction = 1;
            solid.center = localCenter;
            solid.height = height;
            solid.radius = radius;

            CapsuleCollider touch = gameObject.AddComponent<CapsuleCollider>();
            touch.direction = 1;
            touch.center = localCenter;
            touch.height = height + 0.25f;
            touch.radius = radius + 0.2f;
            touch.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            TryTouch(other);
        }

        // Filet de securite : si le joueur etait deja dans la zone au moment de
        // l'armement, Enter ne refire pas, Stay si.
        private void OnTriggerStay(Collider other)
        {
            TryTouch(other);
        }

        private void TryTouch(Collider other)
        {
            if (restarting
                || Time.time - enabledAt < armDelay
                || Time.time - lastTouchTime < touchCooldown
                || !IsPlayer(other))
            {
                return;
            }

            lastTouchTime = Time.time;
            onTouched?.Invoke();

            if (restartGame)
            {
                restarting = true;
                // Runner independant : survit meme si le directeur cache l'entite
                // (SetActive(false)) pendant la sequence.
                SetDeathSequenceActive(true);
                var runner = new GameObject("ProtocoleZero_GameOver").AddComponent<DeathJumpscareRunner>();
                runner.Begin(transform, headLocalOffset, screamClip, screamVolume, restartDelay);
            }
            else
            {
                PlayScream();
            }
        }

        private static bool IsPlayer(Collider other)
        {
            return other is CharacterController
                || other.GetComponentInParent<SimplePlayerController>() != null
                || other.transform.root.CompareTag("Player");
        }

        private void PlayScream()
        {
            if (screamClip == null)
            {
                return;
            }

            // Source detachee pour que le cri ne soit pas coupe si l'entite disparait.
            var go = new GameObject("EntityContactScream");
            go.transform.position = transform.position;
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.3f;
            source.priority = 0;
            source.PlayOneShot(screamClip, screamVolume);
            source.PlayOneShot(screamClip, screamVolume * 0.7f);
            Destroy(go, screamClip.length + 0.5f);
        }
    }

    /// <summary>
    /// Sequence de defaite : cri plein volume dans les oreilles, monstre plaque
    /// devant le visage du joueur avec convulsions et rapprochement progressif,
    /// zoom camera rapide (PC uniquement), puis rechargement de la scene.
    /// Objet independant : survit meme si un directeur desactive l'entite.
    /// </summary>
    internal sealed class DeathJumpscareRunner : MonoBehaviour
    {
        private Transform entity;
        private Vector3 headLocalOffset;
        private float delay;
        private float elapsed;
        private Camera cam;
        private bool desktop;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private float noiseSeed;

        public void Begin(Transform entityRoot, Vector3 headOffset, AudioClip scream, float volume, float seconds)
        {
            entity = entityRoot;
            headLocalOffset = headOffset;
            delay = Mathf.Max(1f, seconds);
            noiseSeed = Random.value * 100f;

            SimplePlayerController.CameraOverrideActive = true;
            SimplePlayerController.ControlsLocked = true;

            cam = Camera.main;
            desktop = cam != null && !UnityEngine.XR.XRSettings.isDeviceActive;

            AudioSource screamSource = null;
            AudioSource screamLayer = null;
            if (scream != null)
            {
                // Double couche : le meme cri plein volume + une copie legerement
                // ralentie (plus grave, plus longue) : plus epais, plus effrayant.
                screamSource = gameObject.AddComponent<AudioSource>();
                screamSource.playOnAwake = false;
                screamSource.spatialBlend = 0f;
                screamSource.priority = 0;
                screamSource.PlayOneShot(scream, Mathf.Clamp01(volume));

                screamLayer = gameObject.AddComponent<AudioSource>();
                screamLayer.playOnAwake = false;
                screamLayer.spatialBlend = 0f;
                screamLayer.priority = 0;
                screamLayer.pitch = 0.82f;
                screamLayer.PlayOneShot(scream, Mathf.Clamp01(volume) * 0.85f);
            }

            // Musique coupee net et sons du monde ecrases : le cri prend toute la
            // place. Pas de restauration a prevoir, la scene est rechargee juste apres.
            var music = Object.FindFirstObjectByType<MusicAnchorController>();
            if (music != null)
            {
                music.CutForScream();
            }

            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && sources[i] != screamSource && sources[i] != screamLayer)
                {
                    sources[i].volume *= 0.15f;
                }
            }

            PlaceEntityInFace();
        }

        // Plaque le monstre pour que sa tete soit droit devant les yeux du joueur.
        private void PlaceEntityInFace()
        {
            if (entity == null || cam == null)
            {
                return;
            }

            entity.gameObject.SetActive(true);
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;

            // Convention du projet : la face visible du mesh est du cote -Z.
            baseRotation = Quaternion.LookRotation(forward, Vector3.up);
            Vector3 headTarget = cam.transform.position + forward * 0.9f;
            basePosition = headTarget - baseRotation * headLocalOffset;
            entity.SetPositionAndRotation(basePosition, baseRotation);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            if (entity != null && cam != null)
            {
                // La tete continue de se rapprocher pendant toute la sequence.
                Vector3 headNow = basePosition + baseRotation * headLocalOffset;
                if (Vector3.Distance(headNow, cam.transform.position) > 0.5f)
                {
                    basePosition += (cam.transform.position - headNow).normalized * (0.3f * Time.deltaTime);
                }

                float yaw = (Mathf.PerlinNoise(noiseSeed, elapsed * 16f) - 0.5f) * 24f;
                float tilt = (Mathf.PerlinNoise(noiseSeed + 4f, elapsed * 13f) - 0.5f) * 18f;
                entity.SetPositionAndRotation(basePosition, baseRotation * Quaternion.Euler(tilt, yaw, 0f));
            }

            if (desktop)
            {
                // Zoom progressif rapide sur la tete, regard verrouille.
                Vector3 focus = entity != null
                    ? entity.position + baseRotation * headLocalOffset
                    : cam.transform.position + cam.transform.forward;
                Vector3 toFocus = focus - cam.transform.position;
                if (toFocus.sqrMagnitude > 0.0004f)
                {
                    Quaternion look = Quaternion.LookRotation(toFocus.normalized, Vector3.up);
                    cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, look, 1f - Mathf.Exp(-14f * Time.deltaTime));
                }

                // Secousse violente au depart qui s'attenue : le choc est physique.
                float shakeAmp = 3.2f * Mathf.Clamp01(1f - elapsed / Mathf.Max(1f, delay));
                float shakeYaw = (Mathf.PerlinNoise(noiseSeed + 17f, elapsed * 30f) - 0.5f) * 2f * shakeAmp;
                float shakePitch = (Mathf.PerlinNoise(noiseSeed + 29f, elapsed * 27f) - 0.5f) * 2f * shakeAmp;
                cam.transform.rotation *= Quaternion.Euler(shakePitch, shakeYaw, 0f);

                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 26f, 1f - Mathf.Exp(-10f * Time.deltaTime));
            }

            if (elapsed >= delay)
            {
                enabled = false;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
