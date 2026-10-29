using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>
    /// Cosmetic flicker for torch lights. Does NOT use any game RNG (dice / cards):
    /// the noise is driven by time and a phase derived from the light position (deterministic).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class TorchFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.5f)] private float amplitude = 0.15f;
        [SerializeField] private float speed = 5f;

        private Light torchLight;
        private float baseIntensity;
        private float phase;

        private void Awake()
        {
            torchLight = GetComponent<Light>();
            baseIntensity = torchLight.intensity;   // AddLight sets the intensity BEFORE adding this component
            Vector3 p = transform.position;
            phase = Mathf.Abs(p.x * 12.9898f + p.z * 78.233f) % 100f;
        }

        private void Update()
        {
            float n = Mathf.PerlinNoise(Time.time * speed, phase) - 0.5f;   // -0.5 .. 0.5
            torchLight.intensity = baseIntensity * (1f + n * 2f * amplitude);
        }
    }
}