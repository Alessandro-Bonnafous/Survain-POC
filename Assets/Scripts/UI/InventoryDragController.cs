using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Survain.Core;
using Survain.Gameplay.Inventories;
using Survain.Items;

namespace Survain.UI
{
    /// <summary>
    /// Pilote l'état du drag&drop d'un slot d'inventaire vers un autre slot ou vers le monde.
    ///
    /// Singleton lazy avec instance unique posée sur le Canvas UI. Le ghost visuel suit la
    /// souris pendant le drag. Au lâcher :
    ///   - sur un autre InventorySlotView : Swap (même inventaire) ou SwapAcross (inter-conteneur)
    ///   - hors UI : spawn d'un WorldItem près du joueur avec la quantité entière du stack source
    ///
    /// Convention : le ghost est posé en enfant du Canvas pour rester en overlay, et son Image
    /// doit avoir Raycast Target décoché pour ne pas bloquer les drops sur les slots dessous.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryDragController : MonoBehaviour
    {
        public static InventoryDragController Instance { get; private set; }

        [Header("Ghost (suit la souris pendant le drag)")]
        [Tooltip("RectTransform racine du ghost. Activé/désactivé selon le drag en cours.")]
        [SerializeField] private RectTransform _ghostRoot;

        [Tooltip("Image principale du ghost. Reçoit le sprite ou la couleur fallback.")]
        [SerializeField] private Image _ghostIcon;

        [Tooltip("Texte fallback du ghost (3 lettres après le dernier tiret de l'Id).")]
        [SerializeField] private Text _ghostLabel;

        [Tooltip("Texte de quantité du ghost (visible si stack > 1).")]
        [SerializeField] private Text _ghostQuantity;

        [Header("Drop hors UI")]
        [Tooltip("Transform du joueur pour calculer la position du WorldItem drop manuel.")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("Distance devant le joueur où apparaît le drop (mètres).")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _dropDistance = 1.5f;

        private InventorySlotView _sourceSlotView;
        private bool _droppedOnSlot;
        private bool _dragging;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                SurvainLog.Warn(SurvainLog.Category.UI,
                    "InventoryDragController : instance multiple détectée. Destruction du doublon.", this);
                Destroy(this);
                return;
            }
            Instance = this;

            if (_ghostRoot != null) _ghostRoot.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_dragging && _ghostRoot != null && Mouse.current != null)
            {
                _ghostRoot.position = Mouse.current.position.ReadValue();
            }
        }

        /// <summary>
        /// Appelé par InventorySlotView.OnBeginDrag avec le slot source non-vide.
        /// </summary>
        public void BeginDrag(InventorySlotView source)
        {
            if (source == null) return;
            var slot = source.Inventory.Get(source.SlotIndex);
            if (slot.IsEmpty) return;

            _sourceSlotView = source;
            _droppedOnSlot = false;
            _dragging = true;

            UpdateGhost(slot.Item, slot.Quantity);
            if (_ghostRoot != null)
            {
                _ghostRoot.gameObject.SetActive(true);
                if (Mouse.current != null) _ghostRoot.position = Mouse.current.position.ReadValue();
            }
        }

        /// <summary>
        /// Appelé par InventorySlotView.OnDrop. Le slot cible reçoit ici la fin du drag.
        /// Fusionne si même item stackable, sinon échange (MergeOrSwap / MergeOrSwapAcross).
        /// </summary>
        public void OnDropOnSlot(InventorySlotView target)
        {
            if (!_dragging || _sourceSlotView == null || target == null) return;
            _droppedOnSlot = true;

            var sourceInv = _sourceSlotView.Inventory;
            var targetInv = target.Inventory;
            int sourceIdx = _sourceSlotView.SlotIndex;
            int targetIdx = target.SlotIndex;

            if (sourceInv == null || targetInv == null) return;

            // Même item stackable → fusion (cumul jusqu'à MaxStackSize, reliquat conservé) ;
            // sinon échange. Cf. Inventory.MergeOrSwap / MergeOrSwapAcross.
            if (sourceInv == targetInv)
            {
                sourceInv.MergeOrSwap(sourceIdx, targetIdx);
            }
            else
            {
                sourceInv.MergeOrSwapAcross(sourceIdx, targetInv, targetIdx);
            }
        }

        /// <summary>
        /// Appelé par InventorySlotView.OnEndDrag. Si aucun OnDrop n'a été reçu pendant
        /// le drag, on est hors UI → drop le stack source dans le monde près du joueur.
        /// </summary>
        public void EndDrag()
        {
            if (!_dragging) return;

            if (!_droppedOnSlot && _sourceSlotView != null && _playerTransform != null)
            {
                var sourceInv = _sourceSlotView.Inventory;
                int sourceIdx = _sourceSlotView.SlotIndex;
                var slot = sourceInv != null ? sourceInv.Get(sourceIdx) : InventorySlot.Empty;

                if (!slot.IsEmpty)
                {
                    Vector3 spawnPos = _playerTransform.position
                                       + _playerTransform.forward * _dropDistance
                                       + Vector3.up * 1f;
                    WorldItemSpawner.Spawn(slot.Item, slot.Quantity, spawnPos);
                    sourceInv.TryRemove(slot.Item, slot.Quantity);

                    SurvainLog.Info(SurvainLog.Category.UI,
                        $"Drop : {slot.Quantity}x '{slot.Item.Id}' largué au monde.", this);
                }
            }

            HideGhost();
        }

        private void UpdateGhost(ItemData item, int quantity)
        {
            if (item == null) return;
            bool hasIcon = item.Icon != null;

            if (_ghostIcon != null)
            {
                _ghostIcon.enabled = true;
                if (hasIcon)
                {
                    _ghostIcon.sprite = item.Icon;
                    _ghostIcon.color = Color.white;
                }
                else
                {
                    _ghostIcon.sprite = null;
                    _ghostIcon.color = DeriveFallbackColor(item.Id);
                }
            }

            if (_ghostLabel != null)
            {
                _ghostLabel.enabled = !hasIcon;
                if (!hasIcon) _ghostLabel.text = DeriveFallbackText(item.Id);
            }

            if (_ghostQuantity != null)
            {
                bool showQty = quantity > 1;
                _ghostQuantity.enabled = showQty;
                if (showQty) _ghostQuantity.text = $"x{quantity}";
            }
        }

        private void HideGhost()
        {
            _dragging = false;
            _sourceSlotView = null;
            _droppedOnSlot = false;
            if (_ghostRoot != null) _ghostRoot.gameObject.SetActive(false);
        }

        // Helpers identiques à InventorySlotView (duplication assumée pour découpler les deux composants).

        private static Color DeriveFallbackColor(string id)
        {
            if (string.IsNullOrEmpty(id)) return Color.gray;
            int hash = id.GetHashCode();
            float hue = Mathf.Repeat((hash & 0xFFFF) / 65535f, 1f);
            return Color.HSVToRGB(hue, 0.55f, 0.8f);
        }

        private static string DeriveFallbackText(string id)
        {
            if (string.IsNullOrEmpty(id)) return "??";
            string source = id;
            int dash = id.LastIndexOf('-');
            if (dash >= 0 && dash < id.Length - 1) source = id.Substring(dash + 1);
            string upper = source.ToUpperInvariant();
            int take = Mathf.Min(3, upper.Length);
            return upper.Substring(0, take);
        }
    }
}
