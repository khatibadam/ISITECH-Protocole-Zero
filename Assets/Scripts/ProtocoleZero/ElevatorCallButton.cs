using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProtocoleZero
{
    /// <summary>
    /// Call button for the exit elevator. Pressed through the XRSimpleInteractable on
    /// the same GameObject (ray or direct hand), with the mouse in the desktop
    /// fallback, or with SimplePlayerController's F-interact.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ElevatorCallButton : MonoBehaviour
    {
        [SerializeField] private ElevatorExit elevator;

        private XRSimpleInteractable interactable;

        private void Awake()
        {
            if (elevator == null)
            {
                elevator = GetComponentInParent<ElevatorExit>();
            }

            if (elevator == null)
            {
                elevator = FindFirstObjectByType<ElevatorExit>();
            }

            interactable = GetComponent<XRSimpleInteractable>();
        }

        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.AddListener(HandleSelect);
            }
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(HandleSelect);
            }
        }

        private void HandleSelect(SelectEnterEventArgs args)
        {
            Press();
        }

        private void OnMouseDown()
        {
            Press();
        }

        public void Press()
        {
            elevator?.CallPressed();
        }
    }
}
