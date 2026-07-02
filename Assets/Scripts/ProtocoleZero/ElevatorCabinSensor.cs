using Unity.XR.CoreUtils;
using UnityEngine;

namespace ProtocoleZero
{
    /// <summary>
    /// Trigger volume inside the exit elevator cabin: when the player steps in with
    /// the door open, tells the elevator to close and depart.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ElevatorCabinSensor : MonoBehaviour
    {
        [SerializeField] private ElevatorExit elevator;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;

            if (elevator == null)
            {
                elevator = GetComponentInParent<ElevatorExit>();
            }

            if (elevator == null)
            {
                elevator = FindFirstObjectByType<ElevatorExit>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (elevator != null && other.GetComponentInParent<XROrigin>() != null)
            {
                elevator.PlayerEnteredCabin();
            }
        }
    }
}
