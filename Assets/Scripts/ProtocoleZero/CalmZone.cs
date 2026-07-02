using Unity.XR.CoreUtils;
using UnityEngine;

namespace ProtocoleZero
{
    /// <summary>
    /// Hiding spot (GDD "cachette"): while the player stands inside, stress drains
    /// steadily and a short grounding line plays on first entry. Purely a trigger
    /// volume: no physics, no cost outside the player's visit.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class CalmZone : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float calmPerSecond = 9f;
        [SerializeField] private string enterLine = "Tu es a couvert. Respire.";
        [SerializeField] private StressDirector stressDirector;
        [SerializeField] private SubtitleManager subtitles;

        private bool everEntered;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;

            if (stressDirector == null)
            {
                stressDirector = FindFirstObjectByType<StressDirector>();
            }

            if (subtitles == null)
            {
                subtitles = FindFirstObjectByType<SubtitleManager>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Accepts(other))
            {
                return;
            }

            if (!everEntered)
            {
                everEntered = true;
                subtitles?.ShowLine(enterLine, 3f);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (Accepts(other))
            {
                stressDirector?.Calm(calmPerSecond * Time.deltaTime, "calm zone");
            }
        }

        private bool Accepts(Collider other)
        {
            return other.GetComponentInParent<XROrigin>() != null;
        }
    }
}
