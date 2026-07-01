using UnityEngine;
using UnityEngine.InputSystem;

namespace ProtocoleZero
{
    // Lampe torche simple : une Spot Light qu'on allume/eteint avec une touche.
    // A poser sur un GameObject enfant de la camera (pour que le faisceau suive le regard),
    // portant un composant Light configure en "Spot".
    public sealed class Flashlight : MonoBehaviour
    {
        [SerializeField] private Light spotLight;
        [SerializeField] private Key toggleKey = Key.T;
        // Bouton de manette VR (ex: bouton A/X ou gachette). Se bind dans l'Inspector.
        [SerializeField] private InputActionProperty toggleAction;
        [SerializeField] private bool startsOn;
        [SerializeField] private AudioSource clickSource;

        private bool isOn;

        private void Awake()
        {
            if (spotLight == null)
            {
                spotLight = GetComponent<Light>();
            }

            isOn = startsOn;
            ApplyState();
        }

        private void OnEnable()
        {
            if (toggleAction.action != null)
            {
                toggleAction.action.performed += OnTogglePerformed;
                toggleAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (toggleAction.action != null)
            {
                toggleAction.action.performed -= OnTogglePerformed;
                toggleAction.action.Disable();
            }
        }

        private void OnTogglePerformed(InputAction.CallbackContext context)
        {
            Toggle();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            isOn = !isOn;
            ApplyState();
            if (clickSource != null)
            {
                clickSource.Play();
            }
        }

        private void ApplyState()
        {
            if (spotLight != null)
            {
                spotLight.enabled = isOn;
            }
        }
    }
}
