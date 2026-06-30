using UnityEngine;

namespace ProtocoleZero
{
    /// <summary>
    /// Subtle horror flicker for a Light: continuous Perlin noise plus occasional dropouts.
    /// Captures the initial intensity at Awake and modulates around it.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class LightFlicker : MonoBehaviour
    {
        [SerializeField] private float minMultiplier = 0.45f;
        [SerializeField] private float maxMultiplier = 1.05f;
        [SerializeField] private float noiseSpeed = 2.5f;
        [SerializeField] private float responsiveness = 9f;
        [Tooltip("Probability per second of a brief brown-out dropout.")]
        [SerializeField] private float dropoutPerSecond = 1.2f;

        private Light targetLight;
        private float baseIntensity;
        private float seed;
        private float dropoutTimer;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            baseIntensity = targetLight.intensity;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            if (targetLight == null)
            {
                return;
            }

            float noise = Mathf.PerlinNoise(seed, Time.time * noiseSpeed);
            float multiplier = Mathf.Lerp(minMultiplier, maxMultiplier, noise);

            dropoutTimer -= Time.deltaTime;
            if (dropoutTimer <= 0f && Random.value < dropoutPerSecond * Time.deltaTime)
            {
                multiplier *= Random.Range(0.1f, 0.4f);
                dropoutTimer = Random.Range(0.05f, 0.2f);
            }

            targetLight.intensity = Mathf.Lerp(targetLight.intensity, baseIntensity * multiplier, Time.deltaTime * responsiveness);
        }
    }
}
