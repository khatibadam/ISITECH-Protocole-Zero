using UnityEngine;

namespace ProtocoleZero
{
    public sealed class EntityDirector : MonoBehaviour
    {
        [SerializeField] private StressDirector stressDirector;
        [SerializeField] private Transform playerHead;
        [SerializeField] private GameObject entityVisual;
        [SerializeField] private EntityAnchor[] anchors;
        [SerializeField] private float reevaluateInterval = 1.5f;
        [SerializeField] private bool hideDuringPuzzleGrace = true;
        [Tooltip("Cri joue (spatialise 3D) quand l'entite apparait. Optionnel.")]
        [SerializeField] private AudioClip screamClip;
        [SerializeField, Range(0f, 1f)] private float screamVolume = 0.85f;
        [Tooltip("Delai minimum entre deux cris pour ne pas hurler a chaque apparition.")]
        [SerializeField] private float screamCooldown = 12f;

        [Header("Contact et deplacement")]
        [Tooltip("Vitesse de base a laquelle l'entite visible avance vers le joueur (0 = immobile).")]
        [SerializeField, Min(0f)] private float creepSpeed = 0.85f;
        [Tooltip("Bonus de vitesse par seconde de musique coupee : plus tu tardes a la relancer, plus il accelere.")]
        [SerializeField, Min(0f)] private float silenceAcceleration = 0.06f;
        [SerializeField] private MusicAnchorController musicAnchor;
        [Tooltip("Si l'entite touche le joueur : screamer + la partie redemarre.")]
        [SerializeField] private bool restartOnTouch = true;
        [SerializeField, Min(0.2f)] private float touchRestartDelay = 3.2f;

        [Header("Chasse finale (batterie du PC vide)")]
        [SerializeField] private BatteryTimer batteryTimer;
        [Tooltip("Vitesse de l'entite quand la batterie est vide : il faut avoir fini avant.")]
        [SerializeField, Min(0f)] private float huntCreepSpeed = 2.4f;
        [Tooltip("L'entite se re-teleporte beaucoup plus souvent pendant la chasse.")]
        [SerializeField, Min(0.2f)] private float huntReevaluateInterval = 0.6f;

        private float reevaluateTimer;
        private float visibleTimer;
        private EntityAnchor currentAnchor;
        private AudioSource screamSource;
        private float lastScreamTime = float.NegativeInfinity;

        private void Awake()
        {
            if (stressDirector == null)
            {
                stressDirector = FindFirstObjectByType<StressDirector>();
            }

            if (batteryTimer == null)
            {
                batteryTimer = FindFirstObjectByType<BatteryTimer>();
            }

            if (musicAnchor == null)
            {
                musicAnchor = FindFirstObjectByType<MusicAnchorController>();
            }

            if (anchors == null || anchors.Length == 0)
            {
                anchors = FindObjectsByType<EntityAnchor>(FindObjectsSortMode.None);
            }

            // L'entite devient tangible et mortelle : colliders + cri + defaite au contact.
            if (entityVisual != null)
            {
                EntityContactScare contact = entityVisual.GetComponent<EntityContactScare>();
                if (contact == null)
                {
                    contact = entityVisual.AddComponent<EntityContactScare>();
                }

                contact.Configure(screamClip, 1f, restartOnTouch, touchRestartDelay);
            }

            SetVisible(false);
        }

        private void Update()
        {
            // Sequence de defaite en cours : on ne touche plus a l'entite
            // (le runner la tient plaquee devant le visage du joueur).
            if (EntityContactScare.DeathSequenceActive)
            {
                return;
            }

            if (stressDirector == null || entityVisual == null)
            {
                return;
            }

            if (visibleTimer > 0f)
            {
                visibleTimer -= Time.deltaTime;
                if (visibleTimer <= 0f)
                {
                    SetVisible(false);
                }
            }

            reevaluateTimer -= Time.deltaTime;
            if (reevaluateTimer > 0f)
            {
                FacePlayerIfNeeded();
                CreepTowardPlayer();
                return;
            }

            reevaluateTimer = EffectiveReevaluateInterval;
            if (hideDuringPuzzleGrace && stressDirector.IsInGrace)
            {
                SetVisible(false);
                return;
            }

            EntityAnchor anchor = SelectAnchor(stressDirector.Stage);
            if (anchor == null)
            {
                SetVisible(false);
                return;
            }

            MoveToAnchor(anchor);
            visibleTimer = Mathf.Max(0.6f, anchor.VisibleSeconds);
            SetVisible(stressDirector.Stage != StressStage.Anchored);
        }

        public void RegisterPuzzleSolved()
        {
            visibleTimer = 0f;
            SetVisible(false);
        }

        public void ForceAnchor(string anchorId, float seconds)
        {
            EntityAnchor anchor = FindAnchor(anchorId);
            if (anchor == null)
            {
                return;
            }

            MoveToAnchor(anchor);
            visibleTimer = seconds;
            SetVisible(true);
        }

        private EntityAnchor SelectAnchor(StressStage stage)
        {
            EntityAnchor best = null;
            for (int i = 0; i < anchors.Length; i++)
            {
                EntityAnchor anchor = anchors[i];
                if (anchor == null || anchor.MinimumStage > stage)
                {
                    continue;
                }

                if (best == null || anchor.MinimumStage > best.MinimumStage)
                {
                    best = anchor;
                }
            }

            return best;
        }

        private EntityAnchor FindAnchor(string anchorId)
        {
            for (int i = 0; i < anchors.Length; i++)
            {
                if (anchors[i] != null && anchors[i].AnchorId == anchorId)
                {
                    return anchors[i];
                }
            }

            return null;
        }

        private void MoveToAnchor(EntityAnchor anchor)
        {
            if (currentAnchor != null && currentAnchor != anchor)
            {
                currentAnchor.ClearTell();
            }

            // Ne re-teleporte que si l'ancre change ou si l'entite etait cachee :
            // sinon on annulerait la progression du creep vers le joueur a chaque
            // reevaluation (effet yo-yo).
            bool reposition = currentAnchor != anchor || !entityVisual.activeSelf;
            currentAnchor = anchor;
            if (reposition)
            {
                entityVisual.transform.SetPositionAndRotation(anchor.transform.position, anchor.transform.rotation);
            }

            anchor.TriggerTell();
            FacePlayerIfNeeded();
        }

        private bool IsHunting => batteryTimer != null && batteryTimer.IsEmpty;

        // Se teleporte de plus en plus souvent quand le stress monte, et en continu
        // pendant la chasse finale (batterie vide).
        private float EffectiveReevaluateInterval
        {
            get
            {
                if (IsHunting)
                {
                    return huntReevaluateInterval;
                }

                float stageFactor = stressDirector != null ? (int)stressDirector.Stage / 3f : 0f;
                return Mathf.Lerp(reevaluateInterval, reevaluateInterval * 0.45f, stageFactor);
            }
        }

        // Un peu rapide des le depart, puis accelere tant que la musique reste
        // coupee ; vitesse maximale pendant la chasse finale (batterie vide).
        private float EffectiveCreepSpeed
        {
            get
            {
                if (IsHunting)
                {
                    return huntCreepSpeed;
                }

                float silenceBonus = musicAnchor != null ? musicAnchor.SleepingSeconds * silenceAcceleration : 0f;
                return Mathf.Min(huntCreepSpeed, creepSpeed + silenceBonus);
            }
        }

        // L'entite visible avance lentement vers le joueur, mais seulement si elle a
        // une ligne de vue directe : elle ne traverse jamais un mur.
        private void CreepTowardPlayer()
        {
            if (EffectiveCreepSpeed <= 0f || playerHead == null || entityVisual == null || !entityVisual.activeSelf)
            {
                return;
            }

            Vector3 position = entityVisual.transform.position;
            Vector3 toPlayer = playerHead.position - position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (distance < 0.05f)
            {
                return;
            }

            if (!HasLineOfSightToPlayer(position + Vector3.up * 1.2f))
            {
                return;
            }

            // Trajectoire ondulante plutot que ligne droite : demarche de monstre,
            // pas de glisse mecanique.
            Vector3 forward = toPlayer / distance;
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = (forward + side * (Mathf.Sin(Time.time * 6f) * 0.35f)).normalized;
            entityVisual.transform.position = position + direction * (EffectiveCreepSpeed * Time.deltaTime);
        }

        private bool HasLineOfSightToPlayer(Vector3 from)
        {
            Vector3 to = playerHead.position - from;
            float distance = to.magnitude;
            if (distance < 0.1f)
            {
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(from, to / distance, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i].collider;
                if (hit.transform.IsChildOf(entityVisual.transform))
                {
                    continue;
                }

                if (hit is CharacterController || hit.GetComponentInParent<SimplePlayerController>() != null)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void FacePlayerIfNeeded()
        {
            if (currentAnchor == null || playerHead == null || !currentAnchor.LookAtPlayer || entityVisual == null)
            {
                return;
            }

            Vector3 direction = entityVisual.transform.position - playerHead.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                entityVisual.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private void SetVisible(bool visible)
        {
            if (entityVisual != null && entityVisual.activeSelf != visible)
            {
                entityVisual.SetActive(visible);
                if (visible)
                {
                    PlayScream();
                }
            }

            if (!visible && currentAnchor != null)
            {
                currentAnchor.ClearTell();
            }
        }

        private void PlayScream()
        {
            if (screamClip == null || Time.time - lastScreamTime < screamCooldown)
            {
                return;
            }

            // Source sur un GameObject dedie (pas sur l'entite) pour que le cri
            // ne soit pas coupe quand l'entite redisparait.
            if (screamSource == null)
            {
                var go = new GameObject("EntityScreamAudio");
                screamSource = go.AddComponent<AudioSource>();
                screamSource.playOnAwake = false;
                screamSource.spatialBlend = 1f;
                screamSource.rolloffMode = AudioRolloffMode.Linear;
                screamSource.minDistance = 3.5f;
                screamSource.maxDistance = 30f;
                screamSource.priority = 0;
            }

            screamSource.transform.position = entityVisual.transform.position;
            lastScreamTime = Time.time;
            screamSource.PlayOneShot(screamClip, screamVolume);
        }
    }
}
