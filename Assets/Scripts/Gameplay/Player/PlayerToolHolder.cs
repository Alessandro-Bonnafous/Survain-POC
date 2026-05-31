using UnityEngine;
using Survain.Core;
using Survain.Items;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Attache le visuel de l'outil équipé dans la main de l'avatar.
    ///
    /// S'abonne à PlayerEquipment.OnCurrentToolChanged et instancie le HeldPrefab du
    /// ToolData courant, parenté à l'os de la main du rig Humanoid — récupéré via
    /// Animator.GetBoneTransform(HumanBodyBones.RightHand), sans hardcoder de nom de bone
    /// (robuste au changement de perso, cf. décision 2026-05-22 sur le retargeting Humanoid).
    /// Le visuel précédent est détruit à chaque changement.
    ///
    /// Convention satellite (cf. PlayerVisualAnimator) : composant sur _Player, piloté par
    /// events, désactivable sans toucher au gameplay. La pose de prise (position/rotation/
    /// échelle locales) est portée par le ToolData et réglée dans l'inspecteur.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerToolHolder : MonoBehaviour
    {
        [Tooltip("Équipement joueur, source de l'outil courant. Auto-récupéré sur le même GameObject si non assigné.")]
        [SerializeField] private PlayerEquipment _equipment;

        [Tooltip("Animator de l'avatar Humanoid. Auto-récupéré dans les enfants si non assigné.")]
        [SerializeField] private Animator _animator;

        [Tooltip("Os de la main qui porte l'outil.")]
        [SerializeField] private HumanBodyBones _handBone = HumanBodyBones.RightHand;

        [Tooltip("Override optionnel du point d'attache. Si assigné, remplace l'os Humanoid " +
                 "(utile pour un rig non-Humanoid ou un socket positionné à la main).")]
        [SerializeField] private Transform _attachPointOverride;

        [Header("Main fermée (grip)")]
        [Tooltip("Nom du layer Animator (override, masqué sur la main droite) qui joue la pose " +
                 "de prise. Son poids passe à 1 quand un outil est en main, 0 sinon. " +
                 "Laisser vide (ou layer absent) = pas de fermeture de main.")]
        [SerializeField] private string _gripLayerName = "RightHandGrip";

        private Transform _hand;
        private GameObject _currentVisual;
        private int _gripLayerIndex = -1;

        private void Awake()
        {
            if (_equipment == null) _equipment = GetComponent<PlayerEquipment>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();

            if (_equipment == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerToolHolder : PlayerEquipment introuvable sur le même GameObject.", this);
                enabled = false;
                return;
            }

            _hand = ResolveHand();
            if (_hand == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerToolHolder : point d'attache de la main introuvable. " +
                    "Vérifier que l'avatar a un Animator Humanoid (avec l'os mappé), " +
                    "ou assigner _attachPointOverride.", this);
                enabled = false;
                return;
            }

            // -1 si le layer n'existe pas encore : le grip est alors simplement inactif (no-op).
            if (_animator != null && !string.IsNullOrEmpty(_gripLayerName))
                _gripLayerIndex = _animator.GetLayerIndex(_gripLayerName);
        }

        private Transform ResolveHand()
        {
            if (_attachPointOverride != null) return _attachPointOverride;
            if (_animator != null && _animator.isHuman) return _animator.GetBoneTransform(_handBone);
            return null;
        }

        private void OnEnable()
        {
            _equipment.OnCurrentToolChanged += OnToolChanged;
            Refresh(_equipment.CurrentTool);
        }

        private void OnDisable()
        {
            _equipment.OnCurrentToolChanged -= OnToolChanged;
            ClearVisual();
            ApplyGrip(false);
        }

        private void OnToolChanged(ToolData previous, ToolData current) => Refresh(current);

        private void Refresh(ToolData tool)
        {
            ClearVisual();

            if (tool == null || tool.HeldPrefab == null || _hand == null)
            {
                ApplyGrip(false);
                return;
            }

            _currentVisual = Instantiate(tool.HeldPrefab, _hand);
            var t = _currentVisual.transform;
            t.localPosition = tool.GripLocalPosition;
            t.localRotation = Quaternion.Euler(tool.GripLocalEuler);
            t.localScale = tool.GripLocalScale;

            StripPhysics(_currentVisual);
            ApplyGrip(true);
        }

        // Ferme la main (poids du layer de grip à 1) tant qu'un visuel est tenu, l'ouvre sinon.
        private void ApplyGrip(bool holding)
        {
            if (_animator == null || _gripLayerIndex < 0) return;
            _animator.SetLayerWeight(_gripLayerIndex, holding ? 1f : 0f);
        }

        private void ClearVisual()
        {
            if (_currentVisual == null) return;
            if (Application.isPlaying) Destroy(_currentVisual);
            else DestroyImmediate(_currentVisual);
            _currentVisual = null;
        }

        // Visuel pur : on neutralise la physique éventuelle du prefab du pack (colliders,
        // rigidbodies) pour qu'un outil en main ne pousse pas le joueur ni ne déclenche de triggers.
        private static void StripPhysics(GameObject root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
        }
    }
}
