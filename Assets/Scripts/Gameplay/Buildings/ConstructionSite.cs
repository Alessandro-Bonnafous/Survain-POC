using System.Text;
using UnityEngine;
using Survain.Core;
using Survain.Gameplay.Inventories;
using Survain.Items;

namespace Survain.Gameplay.Buildings
{
    /// <summary>
    /// Chantier de construction : matérialise un bâtiment « en cours ». Posé par le
    /// BuildModeController, il réserve l'emplacement (collider) et affiche un fantôme
    /// translucide du bâtiment cible. Le joueur y dépose les ressources requises (via
    /// PlayerConstructionInteractor / touche E) ; quand le coût est entièrement couvert,
    /// le chantier se transforme en bâtiment fini (« paf »).
    ///
    /// C'est le point d'extension prévu pour le Sprint 3 : un PNJ « bâtisseur » appellera
    /// Deposit(...) depuis un stock de village pour compléter les chantiers tout seul.
    ///
    /// Le fantôme se teinte du rouge (0 %) vers le vert (100 %) au fil des dépôts pour
    /// donner un retour de progression immédiat.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConstructionSite : MonoBehaviour
    {
        [Tooltip("Couleur émissive ajoutée quand le joueur vise le chantier.")]
        [SerializeField] private Color _highlightEmission = new Color(1f, 0.9f, 0.5f) * 0.5f;

        private BuildingData _target;
        private int[] _deposited;          // unités déposées, en parallèle de _target.Cost
        private GameObject _blueprintVisual;
        private Renderer[] _renderers;
        private Material _material;
        private bool _highlighted;

        public BuildingData Target => _target;
        public bool IsComplete { get; private set; }

        /// <summary>Fraction de complétion [0..1] en unités totales déposées / requises.</summary>
        public float Progress
        {
            get
            {
                if (_target == null || _target.Cost == null) return 1f;
                int need = 0, have = 0;
                var cost = _target.Cost;
                for (int i = 0; i < cost.Length; i++)
                {
                    if (cost[i].Item == null) continue;
                    need += cost[i].Amount;
                    have += _deposited[i];
                }
                return need <= 0 ? 1f : Mathf.Clamp01((float)have / need);
            }
        }

        /// <summary>
        /// Initialise le chantier sur un bâtiment cible. Appelé par BuildModeController
        /// juste après l'instanciation, avant le premier rendu.
        /// </summary>
        public void Initialize(BuildingData target, Quaternion rotation)
        {
            _target = target;
            transform.rotation = rotation;
            int lines = target != null && target.Cost != null ? target.Cost.Length : 0;
            _deposited = new int[lines];
            name = $"ConstructionSite_{(target != null ? target.Id : "null")}";

            BuildBlueprint();
            RefreshVisualProgress();

            // Coût nul (ex. structure gratuite/debug) → complétion immédiate.
            CheckComplete();
        }

        /// <summary>
        /// Dépose dans le chantier toutes les ressources correspondantes disponibles dans
        /// <paramref name="from"/>, dans la limite du coût restant. Retourne le nombre
        /// total d'unités déposées (0 si rien n'a pu l'être).
        /// </summary>
        public int Deposit(Inventory from)
        {
            if (from == null || IsComplete || _target == null || _target.Cost == null) return 0;

            int total = 0;
            var cost = _target.Cost;
            for (int i = 0; i < cost.Length; i++)
            {
                var line = cost[i];
                if (line.Item == null) continue;

                int need = line.Amount - _deposited[i];
                if (need <= 0) continue;

                int available = from.Count(line.Item);
                int take = Mathf.Min(need, available);
                if (take <= 0) continue;

                int removed = from.TryRemove(line.Item, take);
                _deposited[i] += removed;
                total += removed;
            }

            if (total > 0)
            {
                RefreshVisualProgress();
                CheckComplete();
            }
            return total;
        }

        /// <summary>Libellé de progression pour le prompt, ex : "Hutte — 4/8 Bois brut · 0/4 Pierre brute".</summary>
        public string ProgressLabel()
        {
            if (_target == null) return string.Empty;
            var sb = new StringBuilder();
            sb.Append(_target.DisplayName);
            var cost = _target.Cost;
            if (cost != null && cost.Length > 0)
            {
                sb.Append(" — ");
                bool first = true;
                for (int i = 0; i < cost.Length; i++)
                {
                    if (cost[i].Item == null) continue;
                    if (!first) sb.Append(" · ");
                    first = false;
                    string label = string.IsNullOrEmpty(cost[i].Item.DisplayName)
                        ? cost[i].Item.Id : cost[i].Item.DisplayName;
                    sb.Append($"{_deposited[i]}/{cost[i].Amount} {label}");
                }
            }
            return sb.ToString();
        }

        /// <summary>Active/désactive la surbrillance émissive (visée par le joueur).</summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted || _material == null) return;
            _highlighted = highlighted;

            if (highlighted)
            {
                _material.EnableKeyword("_EMISSION");
                _material.SetColor("_EmissionColor", _highlightEmission);
            }
            else
            {
                _material.SetColor("_EmissionColor", Color.black);
                _material.DisableKeyword("_EMISSION");
            }
        }

        // ─── Internals ──────────────────────────────────────────────────────

        private void CheckComplete()
        {
            if (_target == null) return;
            var cost = _target.Cost;
            if (cost != null)
            {
                for (int i = 0; i < cost.Length; i++)
                {
                    if (cost[i].Item != null && _deposited[i] < cost[i].Amount) return;
                }
            }
            Complete();
        }

        private void Complete()
        {
            IsComplete = true;

            var go = new GameObject($"Building_{_target.Id}");
            go.transform.SetPositionAndRotation(transform.position, transform.rotation);
            var building = go.AddComponent<Building>();
            building.Initialize(_target);
            BuildingVisualFactory.Create(_target, go.transform);

            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Bâtiment '{_target.Id}' construit en {transform.position}.", go);

            Destroy(gameObject);
        }

        private void BuildBlueprint()
        {
            _blueprintVisual = BuildingVisualFactory.Create(_target, transform);

            // Le chantier garde son collider (réservation de l'emplacement + raycast de visée).
            _renderers = _blueprintVisual.GetComponentsInChildren<Renderer>(includeInactive: true);
            _material = CreateBlueprintMaterial();
            foreach (var rend in _renderers)
            {
                if (rend != null) rend.sharedMaterial = _material;
            }
        }

        private void RefreshVisualProgress()
        {
            if (_material == null) return;
            // Bleu blueprint (0 %) → vert (100 %), translucide. Le bleu lit « plan en cours »
            // plutôt que « invalide » (le rouge est réservé au ghost de placement infaisable).
            Color todo = new Color(0.3f, 0.6f, 1f, 0.4f);
            Color done = new Color(0.35f, 1f, 0.45f, 0.55f);
            Color c = Color.Lerp(todo, done, Progress);
            _material.SetColor("_BaseColor", c);
            _material.color = c;
        }

        private static Material CreateBlueprintMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);

            mat.SetFloat("_Surface", 1f); // transparent
            mat.SetFloat("_Blend", 0f);   // alpha blend
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return mat;
        }
    }
}
