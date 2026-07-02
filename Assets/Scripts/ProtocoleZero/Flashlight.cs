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
        // Bouton de manette VR (optionnel). Si laisse vide, un binding par defaut
        // compatible Oculus (A/X, B/Y) ET HTC Vive (menu) est utilise automatiquement.
        [SerializeField] private InputActionProperty toggleAction;
        [SerializeField] private bool startsOn;
        [SerializeField] private AudioSource clickSource;

        private bool isOn;
        private InputAction activeAction;
        private bool ownsAction;

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
            InputAction userAction = toggleAction.action;
            if (userAction != null && userAction.bindings.Count > 0)
            {
                // L'utilisateur a defini un binding dans l'Inspector : on l'utilise.
                activeAction = userAction;
                ownsAction = false;
            }
            else
            {
                // Aucun binding : on cree un defaut qui marche sur les deux casques.
                // primaryButton/secondaryButton = A/X, B/Y (Oculus) ; menuButton = Vive.
                activeAction = new InputAction("FlashlightToggle", InputActionType.Button);
                activeAction.AddBinding("<XRController>/primaryButton");
                activeAction.AddBinding("<XRController>/secondaryButton");
                activeAction.AddBinding("<XRController>/menuButton");
                ownsAction = true;
            }

            activeAction.performed += OnTogglePerformed;
            activeAction.Enable();
        }

        private void OnDisable()
        {
            if (activeAction == null)
            {
                return;
            }

            activeAction.performed -= OnTogglePerformed;
            activeAction.Disable();
            if (ownsAction)
            {
                activeAction.Dispose();
            }

            activeAction = null;
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
