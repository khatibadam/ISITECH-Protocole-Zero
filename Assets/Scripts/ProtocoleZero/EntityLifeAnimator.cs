using UnityEngine;

namespace ProtocoleZero
{
    /// <summary>
    /// Cheap procedural "alive" motion for the unrigged entity mesh: slow breathing,
    /// subtle sway and occasional twitches. Lives on the visual child so the
    /// EntityDirector keeps full control of the root position/rotation. No physics,
    /// no skinning: Quest-friendly.
    /// </summary>
    public sealed class EntityLifeAnimator : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float breathAmplitude = 0.015f;
        [SerializeField, Min(0.01f)] private float breathSpeed = 0.5f;
        [SerializeField, Min(0f)] private float swayDegrees = 2.2f;
        [SerializeField, Min(0f)] private float twitchDegrees = 7f;
        [SerializeField] private Vector2 twitchIntervalRange = new Vector2(3.5f, 8f);

        private Vector3 baseScale;
        private Quaternion baseRotation;
        private float twitchTimer;
        private float twitchStrength;
        private float noiseSeed;

        private void Awake()
        {
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            noiseSeed = Random.value * 100f;
            ResetTwitchTimer();
        }

        private void OnEnable()
        {
            ResetTwitchTimer();
            // Convulsion immediate a l'apparition : l'entite "sursaute" en meme temps
            // que le cri au lieu de rester figee la premiere seconde.
            twitchStrength = 1f;
        }

        private void Update()
        {
            float t = Time.time;

            // Breathing: slow vertical swell, slight lateral squeeze to keep volume.
            float breath = Mathf.Sin(t * breathSpeed * Mathf.PI * 2f) * breathAmplitude;
            transform.localScale = new Vector3(
                baseScale.x * (1f - breath * 0.4f),
                baseScale.y * (1f + breath),
                baseScale.z * (1f - breath * 0.4f));

            // Sway: low-frequency Perlin drift + a fast decaying jitter burst (twitch).
            twitchTimer -= Time.deltaTime;
            if (twitchTimer <= 0f)
            {
                twitchStrength = 1f;
                ResetTwitchTimer();
            }

            twitchStrength = Mathf.Max(0f, twitchStrength - Time.deltaTime * 4f);
            float yaw = (Mathf.PerlinNoise(noiseSeed, t * 0.35f) - 0.5f) * 2f * swayDegrees
                + (Mathf.PerlinNoise(noiseSeed + 7f, t * 9f) - 0.5f) * 2f * twitchDegrees * twitchStrength;
            float pitch = (Mathf.PerlinNoise(noiseSeed + 3f, t * 0.27f) - 0.5f) * swayDegrees;
            transform.localRotation = baseRotation * Quaternion.Euler(pitch, yaw, 0f);
        }

        private void ResetTwitchTimer()
        {
            twitchTimer = Random.Range(twitchIntervalRange.x, twitchIntervalRange.y);
        }
    }
}
