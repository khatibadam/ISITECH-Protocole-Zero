using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProtocoleZero
{
    /// <summary>
    /// REJOUER button in the end room: reloads the scene for a fresh run. Pressed
    /// through the XRSimpleInteractable on the same GameObject, with the mouse in the
    /// desktop fallback, or with SimplePlayerController's F-interact.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RestartGameButton : MonoBehaviour
    {
        private XRSimpleInteractable interactable;
        private bool restarting;

        private void Awake()
        {
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
            if (restarting)
            {
                return;
            }

            restarting = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
