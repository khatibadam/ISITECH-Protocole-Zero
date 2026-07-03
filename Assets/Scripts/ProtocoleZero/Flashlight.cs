using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProtocoleZero
{
    /// <summary>
    /// Handheld flashlight. Grab it with either hand (XRGrabInteractable on the same
    /// GameObject), pull the trigger (Activate) to toggle the beam. The lens swaps to
    /// a warm emissive color while on. Desktop fallback: aim and press F
    /// (SimplePlayerController routes its interact ray here).
    /// </summary>
    public sealed class Flashlight : MonoBehaviour
    {
        [SerializeField] private Light spotLight;
        [SerializeField] private Renderer lensRenderer;
        [SerializeField] private AudioSource clickSource;
        [SerializeField] private XRGrabInteractable grabInteractable;
        [SerializeField] private HapticFeedbackRouter haptics;
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private bool startsOn;
        [SerializeField] private Color lensOnColor = new Color(1f, 0.95f, 0.75f);
        [SerializeField] private Color lensOffColor = new Color(0.12f, 0.13f, 0.14f);
        [SerializeField, Min(0f)] private float lensOnEmission = 2.2f;

        private bool isOn;
        private bool desktopCarried;
        private Rigidbody body;
        private Collider[] allColliders;
        private MaterialPropertyBlock block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public bool IsOn => isOn;
        public bool IsHeld => grabInteractable != null && grabInteractable.isSelected;

        /// <summary>La lampe ne s'allume qu'en main : saisie VR ou portee clavier.</summary>
        public bool CanUse => IsHeld || desktopCarried;

        private void Awake()
        {
            if (spotLight == null)
            {
                spotLight = GetComponentInChildren<Light>(true);
            }

            if (grabInteractable == null)
            {
                grabInteractable = GetComponent<XRGrabInteractable>();
            }

            if (clickSource == null)
            {
                clickSource = GetComponent<AudioSource>();
            }

            if (haptics == null)
            {
                haptics = FindFirstObjectByType<HapticFeedbackRouter>();
            }

            if (subtitles == null)
            {
                subtitles = FindFirstObjectByType<SubtitleManager>();
            }

            body = GetComponent<Rigidbody>();
            allColliders = GetComponentsInChildren<Collider>(true);
            block = new MaterialPropertyBlock();
            isOn = startsOn;
            ApplyState();
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.activated.AddListener(HandleActivated);
                grabInteractable.selectEntered.AddListener(HandleSelected);
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.activated.RemoveListener(HandleActivated);
                grabInteractable.selectEntered.RemoveListener(HandleSelected);
            }
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            Toggle();
        }

        private void HandleSelected(SelectEnterEventArgs args)
        {
            haptics?.PulseLight("flashlight grab");
        }

        public void Toggle()
        {
            if (!CanUse)
            {
                subtitles?.ShowLine("Prends d'abord la lampe en main.", 2f);
                return;
            }

            isOn = !isOn;
            ApplyState();

            if (clickSource != null)
            {
                clickSource.Stop();
                clickSource.Play();
            }

            haptics?.PulseLight("flashlight toggle");
        }

        /// <summary>
        /// Prise en main clavier/souris (SimplePlayerController) : gele la physique et
        /// coupe les colliders pour que la lampe portee devant la camera ne bloque ni
        /// la marche ni le rayon d'interaction.
        /// </summary>
        public void BeginDesktopCarry()
        {
            if (grabInteractable != null)
            {
                grabInteractable.enabled = false;
            }

            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = true;
            }

            SetCollidersEnabled(false);
            desktopCarried = true;
            haptics?.PulseLight("flashlight desktop grab");
        }

        /// <summary>Repose la lampe a l'endroit donne et restaure sa physique.</summary>
        public void EndDesktopCarry(Vector3 dropPosition, Quaternion dropRotation)
        {
            desktopCarried = false;
            transform.SetPositionAndRotation(dropPosition, dropRotation);
            SetCollidersEnabled(true);

            if (grabInteractable != null)
            {
                grabInteractable.enabled = true;
            }

            if (body != null)
            {
                body.isKinematic = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void SetCollidersEnabled(bool value)
        {
            if (allColliders == null)
            {
                return;
            }

            for (int i = 0; i < allColliders.Length; i++)
            {
                if (allColliders[i] != null)
                {
                    allColliders[i].enabled = value;
                }
            }
        }

        private void ApplyState()
        {
            if (spotLight != null)
            {
                spotLight.enabled = isOn;
            }

            if (lensRenderer != null)
            {
                Color baseColor = isOn ? lensOnColor : lensOffColor;
                lensRenderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, baseColor);
                block.SetColor(ColorId, baseColor);
                block.SetColor(EmissionColorId, isOn ? lensOnColor * lensOnEmission : Color.black);
                lensRenderer.SetPropertyBlock(block);
            }
        }
    }
}
