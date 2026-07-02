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
        [SerializeField] private SubtitleManager subtitles;
        [SerializeField] private Transform cameraOffset;
        [SerializeField, Min(0f)] private float seatedHeightBoost = 0.4f;

        private float baseCameraOffsetY;
        private bool baseCaptured;

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

            if (subtitles == null)
            {
                subtitles = FindFirstObjectByType<SubtitleManager>();
            }

            if (cameraOffset == null)
            {
                var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (origin != null && origin.CameraFloorOffsetObject != null)
                {
                    cameraOffset = origin.CameraFloorOffsetObject.transform;
                }
            }

            if (cameraOffset != null)
            {
                baseCameraOffsetY = cameraOffset.localPosition.y;
                baseCaptured = true;
            }

            Apply();
        }

        public void SetReducedFear(bool enabled)
        {
            reducedFearMode = enabled;
            Apply();
        }

        // Seated players get a rig height boost so standing-height interactions
        // (sockets at 1.45 m, door handles) stay in comfortable reach.
        public void SetSeated(bool enabled)
        {
            seatedMode = enabled;
            Apply();
        }

        public void SetSubtitles(bool enabled)
        {
            subtitlesEnabled = enabled;
            Apply();
        }

        public void Apply()
        {
            if (stressDirector != null)
            {
                stressDirector.SetReducedFear(reducedFearMode);
            }

            if (subtitles != null)
            {
                subtitles.SetEnabled(subtitlesEnabled);
            }

            if (cameraOffset != null && baseCaptured)
            {
                Vector3 p = cameraOffset.localPosition;
                p.y = baseCameraOffsetY + (seatedMode ? seatedHeightBoost : 0f);
                cameraOffset.localPosition = p;
            }
        }
    }
}
