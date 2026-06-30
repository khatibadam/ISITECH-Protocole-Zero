using UnityEngine;

namespace ProtocoleZero
{
    public sealed class EntityAnchor : MonoBehaviour
    {
        [SerializeField] private string anchorId = "E0";
        [SerializeField] private StressStage minimumStage = StressStage.Anchored;
        [SerializeField] private bool lookAtPlayer = true;
        [SerializeField] private float visibleSeconds = 3f;
        [SerializeField] private GameObject tellVisual;
        [SerializeField] private AudioSource tellAudio;

        public string AnchorId => anchorId;
        public StressStage MinimumStage => minimumStage;
        public bool LookAtPlayer => lookAtPlayer;
        public float VisibleSeconds => visibleSeconds;

        public void TriggerTell()
        {
            if (tellVisual != null)
            {
                tellVisual.SetActive(true);
            }

            if (tellAudio != null)
            {
                tellAudio.Stop();
                tellAudio.Play();
            }
        }

        public void ClearTell()
        {
            if (tellAudio != null)
            {
                tellAudio.Stop();
            }
        }
    }
}
