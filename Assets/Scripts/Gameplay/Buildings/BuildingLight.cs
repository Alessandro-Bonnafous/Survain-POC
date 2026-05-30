using UnityEngine;

namespace Survain.Gameplay.Buildings
{
    /// <summary>
    /// Source lumineuse d'un bâtiment (feu de camp, torche…). Crée une Light ponctuelle en
    /// code à partir des paramètres de la BuildingData, avec un léger scintillement pour le
    /// feel « flamme ». Ajouté à la complétion du chantier quand BuildingData.EmitsLight.
    ///
    /// Prépare le cycle jour/nuit (Sprint 5) : la lumière est pour l'instant toujours allumée ;
    /// un futur système pourra la moduler selon l'heure (DayNightCycle).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingLight : MonoBehaviour
    {
        [Tooltip("Amplitude du scintillement (fraction de l'intensité de base).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _flickerAmplitude = 0.15f;

        [Tooltip("Vitesse du scintillement.")]
        [SerializeField] private float _flickerSpeed = 8f;

        private Light _light;
        private float _baseIntensity;
        private float _flickerSeed;

        /// <summary>
        /// Crée et configure la Light. Appelé juste après l'ajout du composant (à la
        /// complétion du chantier).
        /// </summary>
        public void Initialize(Color color, float range, float intensity, float height)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * height;

            _light = go.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = color;
            _light.range = range;
            _light.intensity = intensity;
            _light.shadows = LightShadows.None; // léger pour le POC

            _baseIntensity = intensity;
            // Graine de phase variée par instance (Random interdit ? non — runtime OK ici).
            _flickerSeed = Random.value * 100f;
        }

        private void Update()
        {
            if (_light == null) return;
            // Scintillement doux via bruit de Perlin autour de l'intensité de base.
            float noise = Mathf.PerlinNoise(_flickerSeed, Time.time * _flickerSpeed);
            float factor = 1f + (noise - 0.5f) * 2f * _flickerAmplitude;
            _light.intensity = _baseIntensity * factor;
        }
    }
}
