using UnityEngine;

namespace ProtocoleZero
{
    [RequireComponent(typeof(Collider))]
    public sealed class ElectricalSocket : MonoBehaviour
    {
        [SerializeField] private string targetPlugId = "A";
        [SerializeField] private Transform snapPoint;
        [SerializeField] private ElectricalPanelPuzzle puzzle;
        [SerializeField] private Renderer feedbackRenderer;
        [SerializeField] private Light feedbackLight;
        [SerializeField] private Color idleColor = Color.yellow;
        [SerializeField] private Color solvedColor = Color.green;
        [SerializeField] private Color wrongColor = Color.red;

        private bool solved;
        private MaterialPropertyBlock feedbackBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public string TargetPlugId => targetPlugId;
        public bool IsSolved => solved;
        public Vector3 SnapPosition => snapPoint != null ? snapPoint.position : transform.position;
        public Quaternion SnapRotation => snapPoint != null ? snapPoint.rotation : transform.rotation;

        private void Awake()
        {
            feedbackBlock = new MaterialPropertyBlock();

            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;

            if (puzzle == null)
            {
                puzzle = GetComponentInParent<ElectricalPanelPuzzle>();
            }

            SetFeedback(idleColor);
        }

        private void OnTriggerEnter(Collider other)
        {
            CablePlug plug = other.GetComponentInParent<CablePlug>();
            TryAccept(plug);
        }

        public bool TryAccept(CablePlug plug)
        {
            if (solved || plug == null || plug.IsLocked)
            {
                return false;
            }

            if (plug.PlugId == targetPlugId)
            {
                solved = true;
                plug.AttachTo(this);
                SetFeedback(solvedColor);
                puzzle?.NotifySocketSolved(this);
                return true;
            }

            SetFeedback(wrongColor);
            puzzle?.NotifyWrongSocket(this);
            return false;
        }

        public void ForceSolved(CablePlug plug)
        {
            if (solved)
            {
                return;
            }

            solved = true;
            if (plug != null)
            {
                plug.AttachTo(this);
            }

            SetFeedback(solvedColor);
            puzzle?.NotifySocketSolved(this);
        }

        public void ResetSocket()
        {
            solved = false;
            SetFeedback(idleColor);
        }

        private void SetFeedback(Color color)
        {
            if (feedbackRenderer != null)
            {
                feedbackRenderer.GetPropertyBlock(feedbackBlock);
                feedbackBlock.SetColor(BaseColorId, color);
                feedbackBlock.SetColor(ColorId, color);
                feedbackRenderer.SetPropertyBlock(feedbackBlock);
            }

            if (feedbackLight != null)
            {
                feedbackLight.color = color;
                feedbackLight.enabled = true;
            }
        }
    }
}
