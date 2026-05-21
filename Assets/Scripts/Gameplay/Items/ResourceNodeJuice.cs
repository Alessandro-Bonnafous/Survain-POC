using UnityEngine;

namespace Survain.Gameplay.Items
{
    /// <summary>
    /// Feedback "juice" d'un ResourceNode : shake + scale punch + scale décroissant
    /// proportionnel aux HP + particules colorées + son. S'abonne aux events OnHit
    /// et OnDepleted du ResourceNode co-located.
    ///
    /// Architecture : composant séparé du ResourceNode (SRP). On peut retirer ou
    /// remplacer la couche juice sans toucher au core. Le tuning par nœud passe par
    /// ResourceNodeData (couleur, counts, son, échelle min) ; les paramètres communs
    /// à tous (durées, amplitudes) sont sur ce composant.
    ///
    /// Les particules de coup vivent en enfant du nœud (disparaîtront avec lui).
    /// Les particules de destruction sont spawnées dans un GameObject standalone
    /// pour survivre à SetActive(false) du nœud.
    /// </summary>
    [RequireComponent(typeof(ResourceNode))]
    [DisallowMultipleComponent]
    public sealed class ResourceNodeJuice : MonoBehaviour
    {
        [Header("Shake & scale punch (à chaque coup)")]
        [Tooltip("Amplitude max du shake position (mètres).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _shakeAmplitude = 0.05f;

        [Tooltip("Durée du shake après un coup (secondes).")]
        [Range(0f, 1f)]
        [SerializeField] private float _shakeDuration = 0.15f;

        [Tooltip("Boost de scale instantané à chaque coup (1.08 = +8% pic).")]
        [Range(1f, 1.5f)]
        [SerializeField] private float _scalePunchPeak = 1.08f;

        [Tooltip("Vitesse de retour à l'échelle nominale (rate exponentiel).")]
        [Range(5f, 50f)]
        [SerializeField] private float _scaleReturnRate = 25f;

        [Header("Particules")]
        [Tooltip("Taille initiale des cubelets (mètres).")]
        [Range(0.02f, 0.3f)]
        [SerializeField] private float _particleSize = 0.08f;

        [Tooltip("Vitesse initiale aléatoire des cubelets (m/s).")]
        [Range(1f, 10f)]
        [SerializeField] private float _particleSpeed = 3.5f;

        [Tooltip("Multiplicateur de vitesse pour le burst de destruction.")]
        [Range(1f, 5f)]
        [SerializeField] private float _depleteSpeedMultiplier = 2f;

        // ─── État runtime ───────────────────────────────────────────────────

        private ResourceNode _node;
        private Transform _visual;
        private Vector3 _baseLocalPos;
        private Vector3 _baseScale;

        private ParticleSystem _hitParticles;
        private AudioSource _audio;

        // Shake animé manuellement (durée courte, frame-based)
        private float _shakeEndAt;

        // Scale punch : facteur multiplicatif courant ; revient vers 1 via decay exponentiel.
        private float _scalePunchFactor = 1f;

        // Scale par HP : facteur cible calculé à chaque hit, smooth-lerp dans Update.
        private float _targetHpScale = 1f;
        private float _currentHpScale = 1f;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            _node = GetComponent<ResourceNode>();
        }

        private void OnEnable()
        {
            if (_node != null)
            {
                _node.OnHit += HandleHit;
                _node.OnDepleted += HandleDepleted;
            }
        }

        private void OnDisable()
        {
            if (_node != null)
            {
                _node.OnHit -= HandleHit;
                _node.OnDepleted -= HandleDepleted;
            }
        }

        private void Start()
        {
            // Le visuel est instancié par ResourceNode.Awake (qui passe avant Start ici).
            _visual = _node.VisualInstance;
            if (_visual != null)
            {
                _baseLocalPos = _visual.localPosition;
                _baseScale = _visual.localScale;
            }

            BuildParticleSystem();
            BuildAudioSource();
        }

        private void Update()
        {
            if (_visual == null) return;

            // Shake position : aléatoire dans une sphère, intensité décroissante linéaire.
            Vector3 offset = Vector3.zero;
            if (Time.time < _shakeEndAt)
            {
                float t = (_shakeEndAt - Time.time) / _shakeDuration; // 1 → 0
                offset = Random.insideUnitSphere * (_shakeAmplitude * t);
                offset.y *= 0.3f; // moins de vertical pour rester crédible
            }

            // Scale punch decay (exponentiel vers 1)
            _scalePunchFactor = Mathf.Lerp(_scalePunchFactor, 1f, _scaleReturnRate * Time.deltaTime);

            // Scale par HP smooth
            _currentHpScale = Mathf.Lerp(_currentHpScale, _targetHpScale, 8f * Time.deltaTime);

            _visual.localPosition = _baseLocalPos + offset;
            _visual.localScale = _baseScale * (_currentHpScale * _scalePunchFactor);
        }

        // ─── Event handlers ─────────────────────────────────────────────────

        private void HandleHit()
        {
            _shakeEndAt = Time.time + _shakeDuration;
            _scalePunchFactor = _scalePunchPeak;

            // Échelle cible proportionnelle aux HP restants : 1.0 (plein) → minScale (1 HP)
            float t = (float)_node.CurrentHits / Mathf.Max(1, _node.Data.Hits); // 1 → 0
            _targetHpScale = Mathf.Lerp(_node.Data.MinScaleAtLastHit, 1f, t);

            EmitParticles(_node.Data.HitParticleCount, _particleSpeed);
            PlaySound(volumeScale: 1f);
        }

        private void HandleDepleted()
        {
            // Particules en standalone pour survivre à la désactivation du nœud.
            SpawnStandaloneDepletionParticles();
            PlaySound(volumeScale: 1.4f);
        }

        // ─── Particules ─────────────────────────────────────────────────────

        private void BuildParticleSystem()
        {
            // Le ParticleSystem en sortie est en mode emit-on-demand : on appellera Emit(n) à chaque coup.
            var go = new GameObject("HitParticles");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 1f; // émission depuis ~1m de hauteur

            _hitParticles = go.AddComponent<ParticleSystem>();
            var main = _hitParticles.main;
            main.startLifetime = 0.8f;
            main.startSpeed = _particleSpeed;
            main.startSize = _particleSize;
            main.startColor = _node.Data.HitColor;
            main.gravityModifier = 1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = _hitParticles.emission;
            emission.enabled = false; // on émet via Emit() manuel

            var shape = _hitParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            // Renderer : utilise un material par défaut adapté URP (sinon rose).
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.material = CreateDefaultParticleMaterial();
            rend.renderMode = ParticleSystemRenderMode.Mesh;
            rend.mesh = GetDefaultCubeMesh();
        }

        private void EmitParticles(int count, float speed)
        {
            if (_hitParticles == null) return;

            var emitParams = new ParticleSystem.EmitParams
            {
                startColor = _node.Data.HitColor,
                startSize = _particleSize,
            };

            // Override la vitesse via une boucle (Emit single-param ne prend pas de speed)
            for (int i = 0; i < count; i++)
            {
                emitParams.velocity = Random.insideUnitSphere * speed + Vector3.up * (speed * 0.3f);
                _hitParticles.Emit(emitParams, 1);
            }
        }

        private void SpawnStandaloneDepletionParticles()
        {
            // GameObject indépendant à la position du nœud, qui s'auto-détruit après émission.
            var go = new GameObject($"DepletionFx_{_node.Data.Id}");
            go.transform.position = transform.position + Vector3.up * 1f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.2f;
            main.startSpeed = _particleSpeed * _depleteSpeedMultiplier;
            main.startSize = _particleSize;
            main.startColor = _node.Data.HitColor;
            main.gravityModifier = 1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.material = CreateDefaultParticleMaterial();
            rend.renderMode = ParticleSystemRenderMode.Mesh;
            rend.mesh = GetDefaultCubeMesh();

            // Émission unique
            ps.Emit(_node.Data.DepleteParticleCount);

            // Sécurité : auto-destruction si stopAction ne suffit pas.
            Destroy(go, 5f);
        }

        // ─── Audio ──────────────────────────────────────────────────────────

        private void BuildAudioSource()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f; // son 3D
            _audio.minDistance = 2f;
            _audio.maxDistance = 25f;
            _audio.rolloffMode = AudioRolloffMode.Linear;
        }

        private void PlaySound(float volumeScale)
        {
            if (_audio == null || _node.Data.HitSound == null) return;
            _audio.PlayOneShot(_node.Data.HitSound, volumeScale);
        }

        // ─── Helpers ────────────────────────────────────────────────────────

        private static Mesh _cachedCubeMesh;
        private static Material _cachedParticleMaterial;

        private static Mesh GetDefaultCubeMesh()
        {
            if (_cachedCubeMesh != null) return _cachedCubeMesh;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cachedCubeMesh = go.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            return _cachedCubeMesh;
        }

        private static Material CreateDefaultParticleMaterial()
        {
            if (_cachedParticleMaterial != null) return _cachedParticleMaterial;

            // Cherche un shader URP unlit qui ne dépendra pas de la couleur d'asset par défaut.
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // fallback safe
            _cachedParticleMaterial = new Material(shader);
            _cachedParticleMaterial.enableInstancing = true;
            return _cachedParticleMaterial;
        }
    }
}
