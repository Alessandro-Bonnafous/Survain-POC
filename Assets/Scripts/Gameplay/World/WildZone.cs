using UnityEngine;
using Survain.Gameplay.Player;
using Survain.UI;

namespace Survain.Gameplay.World
{
    /// <summary>
    /// Délimite la zone sauvage (#18) via un Collider (en <c>isTrigger</c> pour ne pas bloquer le
    /// joueur) : quand le joueur entre/sort, on affiche une bannière d'ambiance
    /// (<see cref="WildZoneBanner"/>) et on (dé)active le voile.
    ///
    /// Détection par <b>polling</b> (test des bounds chaque frame) plutôt que via OnTriggerEnter :
    /// les événements de trigger ne sont pas fiables entre un CharacterController et un trigger
    /// statique sans Rigidbody. Le polling est robuste et sans dépendance physique.
    ///
    /// À poser sur le root <c>_WildZone</c> avec un BoxCollider (isTrigger) couvrant la zone.
    /// Self-contained → extractible en scène plus tard.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class WildZone : MonoBehaviour
    {
        [Tooltip("Message affiché à l'entrée dans la zone sauvage.")]
        [SerializeField] private string _enterMessage = "Vous entrez en zone sauvage";

        [Tooltip("Message affiché à la sortie de la zone sauvage.")]
        [SerializeField] private string _exitMessage = "Vous quittez la zone sauvage";

        private Collider _zone;
        private bool _playerInside;

        private void Awake() => _zone = GetComponent<Collider>();

        private void Reset()
        {
            // Confort éditeur : force le collider en trigger dès l'ajout (sinon il bloquerait le joueur).
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || _zone == null) return;

            bool inside = _zone.bounds.Contains(player.transform.position);
            if (inside == _playerInside) return;

            _playerInside = inside;
            WildZoneBanner.Instance.Announce(inside ? _enterMessage : _exitMessage);
            WildZoneBanner.Instance.SetInside(inside);
        }
    }
}
