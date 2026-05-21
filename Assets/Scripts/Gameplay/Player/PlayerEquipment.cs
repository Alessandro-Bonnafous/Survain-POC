using UnityEngine;
using Survain.Core;
using Survain.Items;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Système d'équipement provisoire pour le POC (phase 1 de #6, en attendant
    /// l'inventaire #7). Expose un ToolData courant que les systèmes de récolte
    /// peuvent lire. Sera piloté plus tard par l'inventaire — l'API publique
    /// (CurrentTool, OnCurrentToolChanged) reste stable.
    ///
    /// API d'écriture : SetTool(int slotIndex) pour basculer via le PlayerController
    /// qui consomme les actions Previous/Next de l'InputActionAsset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [Header("Outils disponibles (POC)")]
        [Tooltip("Liste ordonnée des outils accessibles. Slots 0..N-1 correspondent aux touches 1..N.")]
        [SerializeField] private ToolData[] _toolSlots;

        [Tooltip("Index de l'outil équipé au démarrage. -1 = aucun.")]
        [SerializeField] private int _initialSlotIndex = 0;

        public ToolData CurrentTool { get; private set; }
        public int CurrentSlotIndex { get; private set; } = -1;

        /// <summary>
        /// Émis quand l'outil courant change. Signature : (previous, current).
        /// </summary>
        public event System.Action<ToolData, ToolData> OnCurrentToolChanged;

        private void Awake()
        {
            if (_toolSlots == null || _toolSlots.Length == 0)
            {
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    "PlayerEquipment : aucun outil configuré. La récolte sera limitée à ToolType.None.",
                    this);
                return;
            }

            if (_initialSlotIndex >= 0 && _initialSlotIndex < _toolSlots.Length)
            {
                SetTool(_initialSlotIndex);
            }
        }

        /// <summary>
        /// Équipe l'outil au slot donné. Index hors plage = unequip (CurrentTool=null).
        /// </summary>
        public void SetTool(int slotIndex)
        {
            ToolData target = null;
            if (_toolSlots != null && slotIndex >= 0 && slotIndex < _toolSlots.Length)
            {
                target = _toolSlots[slotIndex];
            }

            if (target == CurrentTool) return;

            var previous = CurrentTool;
            CurrentTool = target;
            CurrentSlotIndex = target != null ? slotIndex : -1;

            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Outil équipé : '{(target != null ? target.Id : "aucun")}' (slot {CurrentSlotIndex}).",
                this);

            OnCurrentToolChanged?.Invoke(previous, CurrentTool);
        }

        /// <summary>
        /// Nombre de slots d'outils configurés.
        /// </summary>
        public int SlotCount => _toolSlots != null ? _toolSlots.Length : 0;
    }
}
