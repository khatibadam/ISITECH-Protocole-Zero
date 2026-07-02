using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProtocoleZero
{
    /// <summary>
    /// A pressable option on the tutorial start board. Pressed through the
    /// XRSimpleInteractable on the same GameObject (ray or direct hand), with the
    /// mouse in the desktop fallback, or with SimplePlayerController's F-interact.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class TutorialOptionButton : MonoBehaviour
    {
        public enum Option
        {
            ModeTeleport = 0,
            ModeContinuous = 1,
            ModeBoth = 2,
            Start = 3
        }

        [SerializeField] private Option option;
        [SerializeField] private TutorialStartScreen screen;

        private XRSimpleInteractable interactable;

        public Option Kind => option;

        private void Awake()
        {
            if (screen == null)
            {
                screen = GetComponentInParent<TutorialStartScreen>();
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
            screen?.HandleOption(option);
        }
    }
}
