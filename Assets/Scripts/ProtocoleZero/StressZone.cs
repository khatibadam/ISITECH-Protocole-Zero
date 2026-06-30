using UnityEngine;

namespace ProtocoleZero
{
    [RequireComponent(typeof(Collider))]
    public sealed class StressZone : MonoBehaviour
    {
        [SerializeField] private float stressPerSecond = 4f;
        [SerializeField] private bool marksDarkZone = true;
        [SerializeField] private string requiredTag = "Player";
        [SerializeField] private StressDirector stressDirector;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (stressDirector == null)
            {
                stressDirector = FindFirstObjectByType<StressDirector>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Accepts(other) && marksDarkZone)
            {
                stressDirector?.SetDarkZone(true);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (Accepts(other))
            {
                stressDirector?.AddStress(stressPerSecond * Time.deltaTime, "stress zone");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (Accepts(other) && marksDarkZone)
            {
                stressDirector?.SetDarkZone(false);
            }
        }

        private bool Accepts(Collider other)
        {
            return string.IsNullOrEmpty(requiredTag) || other.CompareTag(requiredTag) || other.GetComponentInParent<SimplePlayerController>() != null;
        }
    }
}
