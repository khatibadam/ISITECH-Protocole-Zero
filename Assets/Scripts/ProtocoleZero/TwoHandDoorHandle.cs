using UnityEngine;

namespace ProtocoleZero
{
    [RequireComponent(typeof(Collider))]
    public sealed class TwoHandDoorHandle : MonoBehaviour
    {
        [SerializeField] private TwoHandDoor door;
        [SerializeField] private bool leftHandle = true;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (door == null)
            {
                door = GetComponentInParent<TwoHandDoor>();
            }
        }

        private void OnMouseDown()
        {
            Pulse();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<SimplePlayerController>() != null || other.name.Contains("Hand") || other.name.Contains("Controller"))
            {
                Pulse();
            }
        }

        public void Pulse()
        {
            if (leftHandle)
            {
                door?.PulseLeftHandle();
            }
            else
            {
                door?.PulseRightHandle();
            }
        }
    }
}
