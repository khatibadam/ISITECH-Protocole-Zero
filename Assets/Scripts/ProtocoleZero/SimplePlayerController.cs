using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

namespace ProtocoleZero
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SimplePlayerController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private float mouseSensitivity = 1.4f;
        [SerializeField] private float interactDistance = 3f;
        [SerializeField] private bool lockCursorInPlay;

        private CharacterController controller;
        private XROrigin xrOrigin;
        private float pitch;
        private Vector3 lastMovedPosition;
        private bool hasMoved;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            xrOrigin = GetComponent<XROrigin>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        // Returns the transform whose horizontal facing should drive movement/turn.
        // Prefer the camera (head) so movement follows where the player looks.
        private Transform Head => playerCamera != null ? playerCamera.transform : transform;

        private void Start()
        {
            if (lockCursorInPlay)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private void Update()
        {
            SyncAfterExternalTeleport();
            Move();
            Look();
            SnapTurn();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                TryInteract();
            }
        }

        // XRI teleportation (XRBodyGroundPosition) writes the rig position directly on the
        // Transform, but the CharacterController keeps its own cached physics position: our
        // per-frame controller.Move() would snap the rig right back, silently cancelling every
        // teleport. Push the external Transform change into the physics engine first.
        private void SyncAfterExternalTeleport()
        {
            if (hasMoved && (transform.position - lastMovedPosition).sqrMagnitude > 0.0004f)
            {
                Physics.SyncTransforms();
            }
        }

        private void Move()
        {
            Keyboard keyboard = Keyboard.current;
            Vector3 input = Vector3.zero;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed)
                {
                    input.x -= 1f;
                }
                if (keyboard.dKey.isPressed)
                {
                    input.x += 1f;
                }
                if (keyboard.sKey.isPressed)
                {
                    input.z -= 1f;
                }
                if (keyboard.wKey.isPressed)
                {
                    input.z += 1f;
                }
            }

            input = Vector3.ClampMagnitude(input, 1f);

            // Camera-relative horizontal movement: walk where you look, not where the rig root points.
            Vector3 fwd = Head.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = Head.right; right.y = 0f; right.Normalize();
            Vector3 motion = (fwd * input.z + right * input.x) * moveSpeed;
            motion.y = Physics.gravity.y * 0.15f;
            controller.Move(motion * Time.deltaTime);
            lastMovedPosition = transform.position;
            hasMoved = true;
        }

        // Yaw the rig WITHOUT translating it: rotate around the camera position so the
        // offset head does not orbit the pivot (which previously caused drift into the void).
        private void YawAroundHead(float degrees)
        {
            if (Mathf.Abs(degrees) < 0.0001f)
            {
                return;
            }

            if (xrOrigin != null)
            {
                xrOrigin.RotateAroundCameraUsingOriginUp(degrees);
            }
            else
            {
                transform.RotateAround(Head.position, Vector3.up, degrees);
            }
        }

        private void Look()
        {
            Mouse mouse = Mouse.current;
            if (playerCamera == null || mouse == null || !mouse.rightButton.isPressed)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue() * (mouseSensitivity * 0.08f);
            float yaw = delta.x;
            float lookY = delta.y;
            YawAroundHead(yaw);
            pitch = Mathf.Clamp(pitch - lookY, -75f, 75f);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void SnapTurn()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                YawAroundHead(-45f);
            }
            else if (keyboard.eKey.wasPressedThisFrame)
            {
                YawAroundHead(45f);
            }
        }

        private void TryInteract()
        {
            if (playerCamera == null)
            {
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            {
                InteractableButton button = hit.collider.GetComponentInParent<InteractableButton>();
                if (button != null)
                {
                    button.Activate();
                    return;
                }

                TutorialOptionButton tutorialButton = hit.collider.GetComponentInParent<TutorialOptionButton>();
                if (tutorialButton != null)
                {
                    tutorialButton.Press();
                    return;
                }

                ElevatorCallButton elevatorButton = hit.collider.GetComponentInParent<ElevatorCallButton>();
                if (elevatorButton != null)
                {
                    elevatorButton.Press();
                    return;
                }

                RestartGameButton restartButton = hit.collider.GetComponentInParent<RestartGameButton>();
                if (restartButton != null)
                {
                    restartButton.Press();
                    return;
                }

                TwoHandDoorHandle handle = hit.collider.GetComponentInParent<TwoHandDoorHandle>();
                if (handle != null)
                {
                    handle.Pulse();
                }
            }
        }
    }
}
