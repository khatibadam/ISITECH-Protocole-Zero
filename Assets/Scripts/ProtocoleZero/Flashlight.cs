using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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
        [SerializeField] private bool enableGlobalInput = true;
        [SerializeField] private XRGrabInteractable grabInteractable;
        [SerializeField] private Renderer stateRenderer;
        [SerializeField] private HapticFeedbackRouter haptics;

        private bool isOn;
        private InputAction activeAction;
        private bool ownsAction;
        private MaterialPropertyBlock block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (spotLight == null)
            {
                spotLight = GetComponent<Light>();
            }

            if (grabInteractable == null)
            {
                grabInteractable = GetComponent<XRGrabInteractable>();
            }

            if (haptics == null)
            {
                haptics = FindFirstObjectByType<HapticFeedbackRouter>();
            }

            block = new MaterialPropertyBlock();
            isOn = startsOn;
            ApplyState();
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.activated.AddListener(OnGrabActivated);
                grabInteractable.selectEntered.AddListener(OnSelected);
            }

            if (!enableGlobalInput)
            {
                return;
            }

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
            if (grabInteractable != null)
            {
                grabInteractable.activated.RemoveListener(OnGrabActivated);
                grabInteractable.selectEntered.RemoveListener(OnSelected);
            }

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

        private void OnGrabActivated(ActivateEventArgs args)
        {
            Toggle();
            haptics?.PulseMedium("flashlight activate");
        }

        private void OnSelected(SelectEnterEventArgs args)
        {
            haptics?.PulseLight("flashlight grab");
        }

        private void OnTogglePerformed(InputAction.CallbackContext context)
        {
            Toggle();
        }

        private void Update()
        {
            if (!enableGlobalInput)
            {
                return;
            }

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

            if (stateRenderer != null)
            {
                stateRenderer.GetPropertyBlock(block);
                Color c = isOn ? new Color(0.2f, 1f, 0.85f, 1f) : new Color(0.08f, 0.1f, 0.11f, 1f);
                block.SetColor(BaseColorId, c);
                block.SetColor(ColorId, c);
                stateRenderer.SetPropertyBlock(block);
            }
        }
    }
}
