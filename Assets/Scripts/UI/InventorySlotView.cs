using UnityEngine;
using UnityEngine.UI;
using Survain.Core;
using Survain.Gameplay.Inventories;
using Survain.Items;

namespace Survain.UI
{
    /// <summary>
    /// Vue d'un slot unique d'inventaire (hotbar ou backpack).
    /// S'abonne à l'event OnSlotChanged de l'Inventory cible et filtre sur son index.
    ///
    /// Composants UI gérés :
    ///  - _iconImage : sprite de l'item (visible si ItemData.Icon non-null)
    ///  - _fallbackBg : couleur unie déterministe par item.Id (visible si pas d'icône)
    ///  - _fallbackLabel : 3 premières lettres de l'Id (visible si pas d'icône)
    ///  - _quantityLabel : "xN" visible si quantité > 1
    ///  - _selectionFrame : encadre le slot quand SetSelected(true) — facultatif (pour la hotbar)
    ///
    /// Le slot vide n'affiche rien (toutes les sous-images cachées). Le _fallbackBg sert
    /// aussi de fond visuel quand le slot a un item mais pas d'icône — quand l'icône arrive,
    /// elle masque le bg.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventorySlotView : MonoBehaviour
    {
        [Header("Icône & fallback")]
        [Tooltip("Image qui affiche l'icône de l'item (Sprite). Cachée si l'item n'a pas d'icône.")]
        [SerializeField] private Image _iconImage;

        [Tooltip("Fond coloré du slot quand l'item n'a pas d'icône. Couleur dérivée du hash de l'Id.")]
        [SerializeField] private Image _fallbackBg;

        [Tooltip("Texte fallback (3 premières lettres de l'Id) affiché par-dessus le fond coloré.")]
        [SerializeField] private Text _fallbackLabel;

        [Header("Quantité")]
        [Tooltip("Texte de quantité (format 'xN'). Visible si quantité > 1.")]
        [SerializeField] private Text _quantityLabel;

        [Header("Sélection (hotbar uniquement, facultatif)")]
        [Tooltip("Image d'encadrement du slot quand sélectionné. Laisser null pour un slot non-sélectionnable (backpack).")]
        [SerializeField] private Image _selectionFrame;

        private Inventory _inventory;
        private int _slotIndex = -1;

        /// <summary>
        /// Associe cette vue à un slot précis d'un Inventory. S'abonne aux changements
        /// et affiche immédiatement l'état courant.
        /// </summary>
        public void Bind(Inventory inventory, int slotIndex)
        {
            UnbindIfBound();

            _inventory = inventory;
            _slotIndex = slotIndex;

            if (_inventory == null)
            {
                SurvainLog.Warn(SurvainLog.Category.UI,
                    $"InventorySlotView : Bind avec inventory null sur slot {slotIndex}.", this);
                RefreshFromSlot(InventorySlot.Empty);
                return;
            }

            _inventory.OnSlotChanged += OnSlotChanged;
            RefreshFromSlot(_inventory.Get(_slotIndex));
        }

        /// <summary>
        /// Encadre (ou pas) le slot. Utilisé par la hotbar pour matérialiser le slot équipé.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_selectionFrame != null) _selectionFrame.enabled = selected;
        }

        private void OnDestroy()
        {
            // Désabonnement à la destruction uniquement. PAS sur OnDisable : sinon, fermer
            // le panel d'inventaire (SetActive(false)) désabonnerait tous les slots, et le
            // Bind n'étant ré-appelé qu'au Start, ils ne recevraient plus les changements
            // au prochain re-open. En l'état, les slots restent à jour même panel fermé
            // (le RefreshFromSlot tourne sur GameObject désactivé sans souci).
            UnbindIfBound();
        }

        private void UnbindIfBound()
        {
            if (_inventory != null)
            {
                _inventory.OnSlotChanged -= OnSlotChanged;
                _inventory = null;
            }
        }

        private void OnSlotChanged(int index, InventorySlot before, InventorySlot after)
        {
            if (index != _slotIndex) return;
            RefreshFromSlot(after);
        }

        private void RefreshFromSlot(InventorySlot slot)
        {
            if (slot.IsEmpty)
            {
                if (_iconImage != null) _iconImage.enabled = false;
                if (_fallbackBg != null) _fallbackBg.enabled = false;
                if (_fallbackLabel != null) _fallbackLabel.enabled = false;
                if (_quantityLabel != null) _quantityLabel.enabled = false;
                return;
            }

            var item = slot.Item;
            bool hasIcon = item.Icon != null;

            if (_iconImage != null)
            {
                _iconImage.enabled = hasIcon;
                if (hasIcon) _iconImage.sprite = item.Icon;
            }

            if (_fallbackBg != null)
            {
                _fallbackBg.enabled = !hasIcon;
                if (!hasIcon) _fallbackBg.color = DeriveFallbackColor(item.Id);
            }

            if (_fallbackLabel != null)
            {
                _fallbackLabel.enabled = !hasIcon;
                if (!hasIcon) _fallbackLabel.text = DeriveFallbackText(item.Id);
            }

            if (_quantityLabel != null)
            {
                bool showQty = slot.Quantity > 1;
                _quantityLabel.enabled = showQty;
                if (showQty) _quantityLabel.text = $"x{slot.Quantity}";
            }
        }

        /// <summary>
        /// Couleur déterministe dérivée du hash de l'Id. Saturation et luminosité fixes
        /// pour rester lisibles, teinte distribuée sur tout le cercle.
        /// </summary>
        private static Color DeriveFallbackColor(string id)
        {
            if (string.IsNullOrEmpty(id)) return Color.gray;
            int hash = id.GetHashCode();
            float hue = Mathf.Repeat((hash & 0xFFFF) / 65535f, 1f);
            return Color.HSVToRGB(hue, 0.55f, 0.8f);
        }

        /// <summary>
        /// Texte fallback : 3 premières lettres de la partie après le dernier tiret de l'Id, en majuscules.
        /// "raw-wood" → "WOO", "stone-axe" → "AXE", "stone-pickaxe" → "PIC".
        /// Cohérent avec la convention kebab-case "domaine-nom" où le nom est plus discriminant.
        /// </summary>
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
