using UnityEngine;

namespace Survain.Items
{
    /// <summary>
    /// Item consommable (nourriture, potion, etc.). Squelette POC — le modèle d'effets
    /// (faim, soif, soin, buffs) sera ajouté quand le système de besoins arrivera.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ConsumableData",
        menuName = "Survain/Items/Consumable",
        order = 55)]
    public sealed class ConsumableData : ItemData
    {
        [Header("Consommation")]
        [Tooltip("Temps de consommation en secondes (animation de boire/manger).")]
        [Range(0f, 10f)]
        [SerializeField] private float _consumeSeconds = 1f;

        public float ConsumeSeconds => _consumeSeconds;

        public override ItemType Type => ItemType.Consumable;
    }
}
