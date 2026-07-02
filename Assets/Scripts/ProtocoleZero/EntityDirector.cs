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

            if (anchors == null || anchors.Length == 0)
            {
                anchors = FindObjectsByType<EntityAnchor>(FindObjectsSortMode.None);
            }

            SetVisible(false);
        }

        private void Update()
        {
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
                return;
            }

            reevaluateTimer = reevaluateInterval;
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

            currentAnchor = anchor;
            entityVisual.transform.SetPositionAndRotation(anchor.transform.position, anchor.transform.rotation);
            anchor.TriggerTell();
            FacePlayerIfNeeded();
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
                screamSource.minDistance = 2f;
                screamSource.maxDistance = 25f;
            }

            screamSource.transform.position = entityVisual.transform.position;
            lastScreamTime = Time.time;
            screamSource.PlayOneShot(screamClip, screamVolume);
        }
    }
}
