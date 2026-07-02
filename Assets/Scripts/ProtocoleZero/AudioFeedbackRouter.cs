using UnityEngine;

namespace ProtocoleZero
{
    public sealed class AudioFeedbackRouter : MonoBehaviour
    {
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioClip cableGrabClip;
        [SerializeField] private AudioClip socketCorrectClip;
        [SerializeField] private AudioClip socketWrongClip;
        [SerializeField] private AudioClip pcWakeClip;
        [SerializeField] private AudioClip finalDoorOpenClip;
        [SerializeField] private AudioClip braceletPulseClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.65f;

        private void Awake()
        {
            if (oneShotSource == null)
            {
                oneShotSource = GetComponent<AudioSource>();
            }

            if (oneShotSource == null)
            {
                oneShotSource = gameObject.AddComponent<AudioSource>();
            }

            oneShotSource.playOnAwake = false;
            oneShotSource.spatialBlend = 0f;
        }

        public void PlayCableGrab()
        {
            Play(cableGrabClip, 0.45f);
        }

        public void PlayCableGrab(Vector3 position)
        {
            PlayAt(cableGrabClip, 0.5f, position);
        }

        public void PlaySocketCorrect()
        {
            Play(socketCorrectClip, 0.75f);
        }

        public void PlaySocketCorrect(Vector3 position)
        {
            PlayAt(socketCorrectClip, 0.8f, position);
        }

        public void PlaySocketWrong()
        {
            Play(socketWrongClip, 0.8f);
        }

        public void PlaySocketWrong(Vector3 position)
        {
            PlayAt(socketWrongClip, 0.85f, position);
        }

        public void PlayPcWake()
        {
            Play(pcWakeClip, 0.8f);
        }

        public void PlayFinalDoorOpen()
        {
            Play(finalDoorOpenClip, 0.95f);
        }

        public void PlayFinalDoorOpen(Vector3 position)
        {
            PlayAt(finalDoorOpenClip, 1f, position);
        }

        public void PlayBraceletPulse()
        {
            Play(braceletPulseClip, 0.45f);
        }

        private void Play(AudioClip clip, float gain)
        {
            if (clip == null || oneShotSource == null)
            {
                return;
            }

            oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume * gain));
        }

        // Physical interactions (cables, sockets, door) play from their world position
        // so the sound is localizable in the headset. Small round-robin pool: no GC.
        private AudioSource[] spatialPool;
        private int nextSpatial;

        private void PlayAt(AudioClip clip, float gain, Vector3 position)
        {
            if (clip == null)
            {
                return;
            }

            if (spatialPool == null)
            {
                spatialPool = new AudioSource[3];
                for (int i = 0; i < spatialPool.Length; i++)
                {
                    var go = new GameObject("SpatialOneShot_" + i);
                    go.transform.SetParent(transform, false);
                    var src = go.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.spatialBlend = 1f;
                    src.rolloffMode = AudioRolloffMode.Linear;
                    src.minDistance = 0.4f;
                    src.maxDistance = 8f;
                    spatialPool[i] = src;
                }
            }

            AudioSource s = spatialPool[nextSpatial];
            nextSpatial = (nextSpatial + 1) % spatialPool.Length;
            s.transform.position = position;
            s.PlayOneShot(clip, Mathf.Clamp01(volume * gain));
        }
    }
}
