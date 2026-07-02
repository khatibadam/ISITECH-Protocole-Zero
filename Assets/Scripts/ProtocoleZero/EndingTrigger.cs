using Unity.XR.CoreUtils;
using UnityEngine;

namespace ProtocoleZero
{
    /// <summary>
    /// Volume placed just past the final exit door: when the player walks through the
    /// opened door, the clean ending sequence starts. Gated on the door being open so
    /// it can never fire through a closed door.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class EndingTrigger : MonoBehaviour
    {
        [SerializeField] private GameEnding ending;
        [SerializeField] private TwoHandDoor requiredOpenDoor;
        [SerializeField] private string exitKind = "porte";

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;

            if (ending == null)
            {
                ending = FindFirstObjectByType<GameEnding>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ending == null || ending.EndingStarted)
            {
                return;
            }

            if (requiredOpenDoor != null && !requiredOpenDoor.IsOpened)
            {
                return;
            }

            if (other.GetComponentInParent<XROrigin>() != null)
            {
                ending.BeginEnding(exitKind);
            }
        }
    }
}
