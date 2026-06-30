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

        public void PlaySocketCorrect()
        {
            Play(socketCorrectClip, 0.75f);
        }

        public void PlaySocketWrong()
        {
            Play(socketWrongClip, 0.8f);
        }

        public void PlayPcWake()
        {
            Play(pcWakeClip, 0.8f);
        }

        public void PlayFinalDoorOpen()
        {
            Play(finalDoorOpenClip, 0.95f);
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
    }
}
