using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ProtocoleZero
{
    /// <summary>
    /// An electrical port. Uses the canonical XRI <see cref="XRSocketInteractor"/> so that,
    /// in VR, a grabbed cable snaps in on release with hover previews handled by XRI.
    /// This component observes the socket: a matching cable latches the port solved, a
    /// wrong cable is ejected back to its start with a stress/red-flash feedback.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ElectricalSocket : MonoBehaviour
    {
        [SerializeField] private string targetPlugId = "A";
        [SerializeField] private Transform snapPoint;
        [SerializeField] private ElectricalPanelPuzzle puzzle;
        [SerializeField] private XRSocketInteractor socketInteractor;
        [SerializeField] private Renderer feedbackRenderer;
        [SerializeField] private Light feedbackLight;
        [SerializeField] private Color idleColor = Color.yellow;
        [SerializeField] private Color solvedColor = Color.green;
        [SerializeField] private Color wrongColor = Color.red;

        private bool solved;
        private MaterialPropertyBlock feedbackBlock;
        private CablePlug pendingEject;
        private IXRSelectInteractable pendingReject;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public string TargetPlugId => targetPlugId;
        public bool IsSolved => solved;
        public Vector3 SnapPosition => snapPoint != null ? snapPoint.position : transform.position;
        public Quaternion SnapRotation => snapPoint != null ? snapPoint.rotation : transform.rotation;

        private void Awake()
        {
            feedbackBlock = new MaterialPropertyBlock();

            if (socketInteractor == null)
            {
                socketInteractor = GetComponent<XRSocketInteractor>();
            }

            if (socketInteractor != null && snapPoint != null)
            {
                socketInteractor.attachTransform = snapPoint;
            }

            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;

            if (puzzle == null)
            {
                puzzle = GetComponentInParent<ElectricalPanelPuzzle>();
            }

            SetFeedback(idleColor);
        }

        private void OnEnable()
        {
            if (socketInteractor != null)
            {
                socketInteractor.selectEntered.AddListener(HandleSocketed);
            }
        }

        private void OnDisable()
        {
            if (socketInteractor != null)
            {
                socketInteractor.selectEntered.RemoveListener(HandleSocketed);
            }
        }

        private void HandleSocketed(SelectEnterEventArgs args)
        {
            if (args.interactableObject == null)
            {
                return;
            }

            CablePlug plug = args.interactableObject.transform.GetComponentInParent<CablePlug>();
            if (plug == null)
            {
                pendingReject = args.interactableObject;
                SetFeedback(wrongColor);
                return;
            }

            if (solved)
            {
                return;
            }

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
                plug.Lock(this);
                SetFeedback(solvedColor);
                puzzle?.NotifySocketSolved(this);
                return true;
            }

            // Wrong cable: bounce it back out on the next frame so we don't mutate
            // interaction state from inside the select callback.
            SetFeedback(wrongColor);
            pendingEject = plug;
            puzzle?.NotifyWrongSocket(this);
            return false;
        }

        private void Update()
        {
            if (pendingReject != null)
            {
                IXRSelectInteractable interactable = pendingReject;
                pendingReject = null;

                if (socketInteractor != null && socketInteractor.IsSelecting(interactable))
                {
                    socketInteractor.interactionManager.SelectExit(
                        (IXRSelectInteractor)socketInteractor, interactable);
                }

                if (!solved)
                {
                    SetFeedback(idleColor);
                }
            }

            if (pendingEject != null)
            {
                CablePlug plug = pendingEject;
                pendingEject = null;

                // If the plug meanwhile found its correct home, leave it seated.
                if (!plug.IsLocked)
                {
                    if (socketInteractor != null && socketInteractor.IsSelecting(plug.Grab))
                    {
                        socketInteractor.interactionManager.SelectExit(
                            (IXRSelectInteractor)socketInteractor, plug.Grab);
                    }

                    plug.ResetPlug();
                }

                if (!solved)
                {
                    SetFeedback(idleColor);
                }
            }
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
                plug.Lock(this);
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
