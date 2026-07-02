using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace ProtocoleZero
{
    /// <summary>
    /// Central switch for the player's locomotion style, so the player chooses instead
    /// of having everything active at once. Three modes:
    /// - Teleport (comfort, default): both sticks aim/teleport to the teal pads + snap turn.
    /// - Continuous: left stick walks, right stick snap turns, teleport fully off.
    /// - Both: left stick walks, right stick teleports (push forward) + snap turns.
    /// The choice is persisted in PlayerPrefs.
    /// On real hardware the controller GameObjects are toggled by XRInputModalityManager
    /// when devices (dis)connect, and each re-enable re-runs the ControllerInputActionManager
    /// lifecycle; the mode is therefore re-applied on device changes and shortly after.
    /// </summary>
    public sealed class LocomotionModeManager : MonoBehaviour
    {
        public enum Mode
        {
            Teleport = 0,
            Continuous = 1,
            Both = 2
        }

        [SerializeField] private DynamicMoveProvider moveProvider;
        [SerializeField] private ControllerInputActionManager leftHand;
        [SerializeField] private ControllerInputActionManager rightHand;
        [SerializeField] private XRRayInteractor leftTeleportInteractor;
        [SerializeField] private XRRayInteractor rightTeleportInteractor;
        [SerializeField] private Mode defaultMode = Mode.Teleport;

        private const string PrefKey = "pz_locomotion_mode";
        private float reapplyTimer;

        public Mode CurrentMode { get; private set; } = Mode.Both;

        private void Start()
        {
            int saved = PlayerPrefs.GetInt(PrefKey, (int)defaultMode);
            SetMode((Mode)Mathf.Clamp(saved, 0, 2));
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += HandleDeviceChange;
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;
        }

        // Controllers connecting/waking (or the modality manager flipping the hand
        // GameObjects) replays the ControllerInputActionManager enable flow, which can
        // stomp our per-hand configuration: re-assert the chosen mode right after.
        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected
                || change == InputDeviceChange.Enabled || change == InputDeviceChange.UsageChanged)
            {
                reapplyTimer = 0.5f;
            }
        }

        private void Update()
        {
            if (reapplyTimer > 0f)
            {
                reapplyTimer -= Time.unscaledDeltaTime;
                if (reapplyTimer <= 0f)
                {
                    SetMode(CurrentMode);
                }
            }
        }

        public void SetMode(Mode mode)
        {
            CurrentMode = mode;
            bool walk = mode != Mode.Teleport;

            if (moveProvider != null)
            {
                moveProvider.enabled = walk;
            }

            // Starter Assets contract: smoothMotionEnabled=true turns that hand's stick
            // into smooth movement and disables its teleport + turn actions.
            if (leftHand != null)
            {
                leftHand.smoothMotionEnabled = walk;
            }

            if (rightHand != null)
            {
                // Keep snap turn available on the right stick in every mode.
                rightHand.smoothMotionEnabled = false;
            }

            SetTeleportInteractorUsable(leftTeleportInteractor, mode == Mode.Teleport);
            SetTeleportInteractorUsable(rightTeleportInteractor, mode != Mode.Continuous);

            PlayerPrefs.SetInt(PrefKey, (int)mode);
        }

        private static void SetTeleportInteractorUsable(XRRayInteractor interactor, bool usable)
        {
            if (interactor == null)
            {
                return;
            }

            // The ControllerInputActionManager only toggles the GameObject active state on
            // stick input; with the component disabled the ray casts nothing and the line
            // visual children stay hidden, so teleport is truly off for that hand.
            interactor.enabled = usable;
            for (int i = 0; i < interactor.transform.childCount; i++)
            {
                interactor.transform.GetChild(i).gameObject.SetActive(usable);
            }
        }
    }
}
