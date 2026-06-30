using UnityEngine;

namespace ProtocoleZero
{
    public sealed class ComfortSettingsManager : MonoBehaviour
    {
        [SerializeField] private bool teleportationEnabled = true;
        [SerializeField] private bool snapTurnEnabled = true;
        [SerializeField] private bool seatedMode;
        [SerializeField] private bool reducedFearMode;
        [SerializeField] private bool subtitlesEnabled = true;
        [SerializeField] private StressDirector stressDirector;

        public bool TeleportationEnabled => teleportationEnabled;
        public bool SnapTurnEnabled => snapTurnEnabled;
        public bool SeatedMode => seatedMode;
        public bool ReducedFearMode => reducedFearMode;
        public bool SubtitlesEnabled => subtitlesEnabled;

        private void Awake()
        {
            if (stressDirector == null)
            {
                stressDirector = FindFirstObjectByType<StressDirector>();
            }

            Apply();
        }

        public void SetReducedFear(bool enabled)
        {
            reducedFearMode = enabled;
            Apply();
        }

        public void Apply()
        {
            if (stressDirector != null)
            {
                stressDirector.SetReducedFear(reducedFearMode);
            }
        }
    }
}
