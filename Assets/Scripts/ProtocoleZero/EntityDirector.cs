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

        private float reevaluateTimer;
        private float visibleTimer;
        private EntityAnchor currentAnchor;

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
            }

            if (!visible && currentAnchor != null)
            {
                currentAnchor.ClearTell();
            }
        }
    }
}
