using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Survain.Core;
using Survain.Gameplay.Buildings;
using Survain.Gameplay.Inventories;
using Survain.Items;

namespace Survain.Gameplay.Items
{
    /// <summary>
    /// Composant runtime d'un nœud de ressource posé dans le monde. Consomme un
    /// ResourceNodeData SO pour ses paramètres (HP, item produit, outil requis, respawn).
    ///
    /// Mécanique POC (D2 clic discret) : chaque appel à TryHit() avec l'outil
    /// approprié consomme 1 HP. À HP=0, le nœud spawne le drop, cache son visuel
    /// et désactive son collider. Si RespawnSeconds > 0, une coroutine remet le
    /// nœud en état après le délai.
    ///
    /// Le visuel est soit le prefab référencé par data.VisualPrefab, soit un
    /// placeholder coloré généré au Awake (cube vert pour arbres, sphère grise sinon).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResourceNode : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("SO décrivant le type de nœud (item produit, outil requis, etc.).")]
        [SerializeField] private ResourceNodeData _data;

        [Tooltip("Prefab du drop spawné quand le nœud est épuisé. Si null, un WorldItem générique est créé en code.")]
        [SerializeField] private GameObject _dropPrefab;

        [Header("Visuel placeholder (fallback si data.VisualPrefab est null)")]
        [Tooltip("Couleur de la primitive placeholder si aucun prefab visuel n'est fourni.")]
        [SerializeField] private Color _placeholderColor = new Color(0.3f, 0.6f, 0.2f);

        [Header("Surbrillance")]
        [Tooltip("Couleur émissive appliquée quand le joueur vise le nœud avec son raycast caméra.")]
        [SerializeField] private Color _highlightEmission = new Color(1f, 0.85f, 0.4f) * 0.6f;

        // État runtime
        private int _currentHits;
        private GameObject _visualInstance;
        private Coroutine _respawnRoutine;
        private Renderer[] _visualRenderers;
        private bool _highlighted;

        // Intervalle de re-test quand le respawn est bloqué par une construction.
        private const float RespawnOccupiedRetrySeconds = 5f;

        // Obstacle NavMesh : creuse un trou pour que les PNJ contournent le nœud (#12).
        private NavMeshObstacle _navObstacle;

        public ResourceNodeData Data => _data;
        public int CurrentHits => _currentHits;
        public bool IsDepleted => _currentHits <= 0;
        public Transform VisualInstance => _visualInstance != null ? _visualInstance.transform : null;

        // Registre statique des nœuds en scène (alternative à FindObjectsOfType) : sert au
        // ciblage par les PNJ récolteurs (#14). Les nœuds épuisés restent listés (filtrer IsDepleted).
        private static readonly List<ResourceNode> _all = new List<ResourceNode>();
        public static IReadOnlyList<ResourceNode> All => _all;

        /// <summary>Nœud le plus proche de <paramref name="from"/> satisfaisant le filtre (ou null).</summary>
        public static ResourceNode FindNearest(Vector3 from, Predicate<ResourceNode> filter = null)
        {
            ResourceNode best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _all.Count; i++)
            {
                var n = _all[i];
                if (n == null || (filter != null && !filter(n))) continue;
                float d = (n.transform.position - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = n; }
            }
            return best;
        }

        private void OnEnable() => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        /// <summary>
        /// Émis à chaque coup réussi (avant la vérification d'épuisement).
        /// Consommé par ResourceNodeJuice pour le feedback visuel/audio.
        /// </summary>
        public event System.Action OnHit;

        /// <summary>
        /// Émis quand le nœud atteint 0 HP, juste avant que le visuel et le collider
        /// soient désactivés. Les abonnés qui veulent survivre (particules de fin)
        /// doivent spawner leurs effets dans des GameObjects standalone.
        /// </summary>
        public event System.Action OnDepleted;

        /// <summary>
        /// Émis quand le nœud reset ses HP et redevient harvestable (après respawn).
        /// Consommé par ResourceNodeJuice pour reset l'échelle du visuel.
        /// </summary>
        public event System.Action OnRespawned;

        /// <summary>
        /// Assigne la data du nœud. Doit être appelé AVANT que le GameObject ne devienne actif
        /// (typiquement par un spawner qui crée le GO inactif, configure, puis active).
        /// </summary>
        public void SetData(ResourceNodeData data)
        {
            _data = data;
        }

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_data == null)
            {
                SurvainLog.Error(SurvainLog.Category.World,
                    "ResourceNode : data non assignée.", this);
                enabled = false;
                return;
            }

            _currentHits = _data.Hits;
            SpawnVisual();
            CacheVisualRenderers();
            SetupNavObstacle();
        }

        /// <summary>
        /// Ajoute un NavMeshObstacle (carving) calé sur le CapsuleCollider du nœud, pour que
        /// les PNJ (NavMeshAgent) contournent arbres/rochers/buissons. Build-safe (forme
        /// primitive, pas de lecture de mesh — contrairement au bake des MeshColliders du pack).
        /// Désactivé à l'épuisement, réactivé au respawn (cf. Deplete / RespawnAfterDelay).
        /// </summary>
        private void SetupNavObstacle()
        {
            var capsule = GetComponent<CapsuleCollider>();
            if (capsule == null) return;

            _navObstacle = gameObject.AddComponent<NavMeshObstacle>();
            _navObstacle.shape = NavMeshObstacleShape.Capsule;
            _navObstacle.center = capsule.center;
            _navObstacle.radius = capsule.radius;
            _navObstacle.height = capsule.height;
            _navObstacle.carving = true;
            _navObstacle.carveOnlyStationary = true; // nœud statique : creuse une fois
        }

        /// <summary>
        /// Active ou désactive la surbrillance émissive sur tous les Renderers du visuel.
        /// Appelé par PlayerHarvester quand le joueur vise (ou cesse de viser) le nœud.
        ///
        /// Utilise <c>Renderer.material</c> (et non <c>sharedMaterial</c>) qui clone
        /// automatiquement le material au premier accès — chaque ResourceNode obtient une
        /// instance dédiée. Évite la corruption globale qu'on a quand on modifie un
        /// sharedMaterial partagé entre toutes les instances du même prefab Synty (cas
        /// rencontré pendant le dev : EnableKeyword sur sharedMaterial = tous les arbres
        /// deviennent blancs car la nouvelle variant du shader cherche une _EmissionMap absente).
        ///
        /// Coût : 1 material cloné par Renderer par node au premier survol. Pour les nœuds
        /// courants (1-3 Renderers chacun), c'est négligeable mémoire. Unity nettoie les
        /// clones automatiquement à la destruction du GameObject.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted) return;
            _highlighted = highlighted;
            if (_visualRenderers == null || _visualRenderers.Length == 0) return;

            Color emission = highlighted ? _highlightEmission : Color.black;
            for (int i = 0; i < _visualRenderers.Length; i++)
            {
                var rend = _visualRenderers[i];
                if (rend == null) continue;

                // rend.material clone le sharedMaterial au premier accès → instance dédiée.
                // Les accès suivants retournent toujours la même instance (pas de double clone).
                var mat = rend.material;
                if (mat == null || !mat.HasProperty("_EmissionColor")) continue;

                if (highlighted) mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
        }

        private void CacheVisualRenderers()
        {
            if (_visualInstance == null) return;
            _visualRenderers = _visualInstance.GetComponentsInChildren<Renderer>(includeInactive: true);
            // NB : on ne touche PAS aux materials ici. Le clonage est différé au premier
            // SetHighlighted via Renderer.material (cf. doc plus haut).
        }

        // ─── API publique ───────────────────────────────────────────────────

        /// <summary>
        /// Tente de frapper le nœud avec l'outil donné. Renvoie true si le coup a porté
        /// (outil compatible et nœud encore actif), false sinon. Le caller (PlayerController)
        /// est responsable de l'animation/feedback associé.
        /// </summary>
        public bool TryHit(ToolData tool)
        {
            if (IsDepleted) return false;

            if (!_data.CanHarvestWith(tool))
            {
                SurvainLog.Info(SurvainLog.Category.Gameplay,
                    $"Outil incompatible pour le nœud '{_data.Id}' (requis : {_data.RequiredTool}).",
                    this);
                return false;
            }

            _currentHits--;
            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Coup porté sur '{_data.Id}' — HP restant : {_currentHits}/{_data.Hits}.", this);

            OnHit?.Invoke();

            if (_currentHits <= 0)
            {
                Deplete(null); // drop au sol (récolte joueur)
            }

            return true;
        }

        /// <summary>
        /// Coup de récolte porté par un PNJ travailleur (#14) : pas d'outil requis (le PNJ ne
        /// cible que des nœuds compatibles avec son métier). À l'épuisement, l'item produit est
        /// crédité directement dans <paramref name="into"/> (pas de drop au sol) ; tout surplus
        /// qui n'entre pas est déversé au sol pour ne pas être perdu. Renvoie true si le coup a porté.
        /// </summary>
        public bool HarvestHit(Inventory into)
        {
            if (IsDepleted) return false;

            _currentHits--;
            OnHit?.Invoke();

            if (_currentHits <= 0)
            {
                Deplete(into);
            }
            return true;
        }

        // ─── Internals ──────────────────────────────────────────────────────

        private void SpawnVisual()
        {
            if (_data.VisualPrefab != null)
            {
                _visualInstance = Instantiate(_data.VisualPrefab, transform);
                _visualInstance.transform.localPosition = Vector3.zero;
                _visualInstance.transform.localRotation = Quaternion.identity;
                return;
            }

            // Fallback : primitive colorée. Cube pour les "verticaux" (arbres), sphère sinon.
            bool isVertical = _data.RequiredTool == ToolType.Axe;
            var primitive = isVertical
                ? GameObject.CreatePrimitive(PrimitiveType.Cube)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);

            primitive.name = $"{_data.Id}_Placeholder";
            primitive.transform.SetParent(transform, false);

            if (isVertical)
            {
                primitive.transform.localScale = new Vector3(0.8f, 3f, 0.8f);
                primitive.transform.localPosition = Vector3.up * 1.5f;
            }
            else
            {
                primitive.transform.localScale = Vector3.one * 1.2f;
                primitive.transform.localPosition = Vector3.up * 0.5f;
            }

            var rend = primitive.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                var mat = new Material(rend.sharedMaterial) { color = _placeholderColor };
                rend.sharedMaterial = mat;
            }

            _visualInstance = primitive;
        }

        private void Deplete(Inventory creditTo)
        {
            // Récolte PNJ : crédit direct dans l'inventaire porté (surplus au sol) ;
            // récolte joueur (creditTo == null) : drop au sol classique.
            if (creditTo != null)
            {
                // TryAdd retourne la quantité NON ajoutée (reliquat) → seul le surplus tombe au sol.
                int leftover = creditTo.TryAdd(_data.ProducedItem, _data.ProducedQuantity);
                if (leftover > 0)
                    WorldItemSpawner.Spawn(_data.ProducedItem, leftover, transform.position + Vector3.up * 1f);
            }
            else
            {
                SpawnDrop();
            }

            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Nœud '{_data.Id}' épuisé.", this);

            OnDepleted?.Invoke();

            // Cache le visuel et désactive le collider, mais GARDE le GameObject actif
            // pour que la coroutine de respawn puisse continuer à tourner.
            if (_visualInstance != null) _visualInstance.SetActive(false);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            if (_navObstacle != null) _navObstacle.enabled = false; // libère le passage

            if (_data.RespawnSeconds > 0f)
            {
                _respawnRoutine = StartCoroutine(RespawnAfterDelay(_data.RespawnSeconds));
            }
        }

        private IEnumerator RespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // Un nœud ne réapparaît pas à travers une construction posée entre-temps
            // (issue #9) : tant qu'un Building/ConstructionSite occupe l'emplacement, on
            // re-teste périodiquement. Le nœud finira par revenir si la structure est
            // détruite (#11).
            if (IsSpawnBlockedByBuilding())
            {
                SurvainLog.Info(SurvainLog.Category.Gameplay,
                    $"Nœud '{_data.Id}' : respawn différé, emplacement occupé par une construction.", this);
                while (IsSpawnBlockedByBuilding())
                {
                    yield return new WaitForSeconds(RespawnOccupiedRetrySeconds);
                }
            }

            _currentHits = _data.Hits;

            if (_visualInstance != null) _visualInstance.SetActive(true);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            if (_navObstacle != null) _navObstacle.enabled = true; // re-bloque le passage

            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Nœud '{_data.Id}' réapparu.", this);

            OnRespawned?.Invoke();
            _respawnRoutine = null;
        }

        /// <summary>
        /// Teste si l'emplacement du nœud est occupé par une construction (Building ou
        /// ConstructionSite). Utilisé pour différer le respawn et éviter qu'un arbre/rocher
        /// ne réapparaisse à travers un bâtiment posé pendant que le nœud était épuisé.
        /// </summary>
        private bool IsSpawnBlockedByBuilding()
        {
            Vector3 center = transform.position + Vector3.up * 1f;
            Vector3 halfExtents = new Vector3(0.6f, 1f, 0.6f);
            var overlaps = Physics.OverlapBox(
                center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlaps.Length; i++)
            {
                var t = overlaps[i].transform;
                if (t.GetComponentInParent<Building>() != null) return true;
                if (t.GetComponentInParent<ConstructionSite>() != null) return true;
            }
            return false;
        }

        private void SpawnDrop()
        {
            // Position de drop : légèrement au-dessus du sol pour que la gravité fasse le reste.
            Vector3 dropPos = transform.position + Vector3.up * 1f;

            if (_dropPrefab != null)
            {
                var drop = Instantiate(_dropPrefab, dropPos, Quaternion.identity);
                var worldItem = drop.GetComponent<WorldItem>();
                if (worldItem != null)
                {
                    worldItem.Configure(_data.ProducedItem, _data.ProducedQuantity);
                }
                return;
            }

            // Fallback : génération en code via le spawner partagé (cf. WorldItemSpawner).
            WorldItemSpawner.Spawn(_data.ProducedItem, _data.ProducedQuantity, dropPos);
        }
    }
}
