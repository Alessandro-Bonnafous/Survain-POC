using UnityEngine;
using Survain.Core;
using Survain.Gameplay.Buildings;
using Survain.Gameplay.Inventories;
using Survain.UI;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Séquence de mort du joueur (#19) : abonné à <see cref="PlayerHealth.Died"/>, il déverse tout
    /// le stuff (sac + hotbar) dans une <see cref="Grave"/> lootable, gèle le joueur, affiche le
    /// <see cref="DeathScreen"/> avec décompte, puis fait réapparaître le joueur au feu de camp le
    /// plus proche du village (fallback : position de spawn initiale).
    ///
    /// Gel : on désactive PlayerController (stoppe le déplacement ; effet de bord voulu — son
    /// Instance passe à null, donc les ennemis se désengagent du cadavre) et on pousse UiMode
    /// (neutralise récolte/frappe via clic, fige l'orbite caméra). Tout est rendu au respawn.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDeath : MonoBehaviour
    {
        [Header("Références (même GameObject sauf inventaires)")]
        [Tooltip("Vie du joueur. Si null = résolu sur ce GameObject.")]
        [SerializeField] private PlayerHealth _health;

        [Tooltip("Contrôleur du joueur. Si null = résolu sur ce GameObject.")]
        [SerializeField] private PlayerController _controller;

        [Tooltip("Sac à dos (perdu à la mort).")]
        [SerializeField] private Inventory _backpack;

        [Tooltip("Hotbar (perdue à la mort).")]
        [SerializeField] private Inventory _hotbar;

        [Header("Réglages")]
        [Tooltip("Délai avant réapparition (secondes).")]
        [Min(0f)]
        [SerializeField] private float _respawnDelaySeconds = 5f;

        [Tooltip("Durée de vie de la tombe avant disparition du loot (secondes).")]
        [Min(0f)]
        [SerializeField] private float _graveDespawnSeconds = 300f;

        [Tooltip("Décalage horizontal du respawn par rapport au feu de camp (évite d'apparaître dessus).")]
        [Min(0f)]
        [SerializeField] private float _respawnOffset = 1.5f;

        private Vector3 _initialSpawn;
        private bool _dying;
        private float _respawnAt;

        private void Awake()
        {
            if (_health == null) _health = GetComponent<PlayerHealth>();
            if (_controller == null) _controller = GetComponent<PlayerController>();

            if (_health == null || _backpack == null || _hotbar == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerDeath : health, backpack ou hotbar non assigné.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            _initialSpawn = transform.position;
            if (_health != null) _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Died -= OnDied;
        }

        private void OnDied()
        {
            DropStuffToGrave();

            UiMode.Push();
            if (_controller != null) _controller.enabled = false;

            DeathScreen.Instance.Show();
            _dying = true;
            _respawnAt = Time.time + _respawnDelaySeconds;
        }

        private void Update()
        {
            if (!_dying) return;
            float remaining = _respawnAt - Time.time;
            DeathScreen.Instance.SetCountdown(Mathf.Max(0f, remaining));
            if (remaining <= 0f) Respawn();
        }

        private void Respawn()
        {
            _dying = false;
            Vector3 point = ResolveRespawnPoint();

            if (_controller != null)
            {
                _controller.enabled = true; // ré-active → PlayerController.Instance de nouveau valide
                _controller.Teleport(point);
            }
            else
            {
                transform.position = point;
            }

            if (_health != null) _health.ResetToFull();

            DeathScreen.Instance.Hide();
            UiMode.Pop();
            SurvainLog.Info(SurvainLog.Category.Gameplay, "Joueur réapparu.", this);
        }

        /// <summary>Priorité au lit « maison » activé (RespawnPoint.Active) ; sinon feu de camp le
        /// plus proche du village (proxy : spawn initial) ; sinon le spawn initial.</summary>
        private Vector3 ResolveRespawnPoint()
        {
            if (RespawnPoint.Active != null) return RespawnPoint.Active.RespawnPosition;

            var campfire = Building.FindNearest(_initialSpawn, b => b.Data != null && b.Data.EmitsLight);
            if (campfire == null) return _initialSpawn;
            return campfire.transform.position + campfire.transform.forward * _respawnOffset;
        }

        private void DropStuffToGrave()
        {
            int capacity = (_backpack != null ? _backpack.Capacity : 0)
                         + (_hotbar != null ? _hotbar.Capacity : 0);
            if (capacity <= 0 || (!HasAnyItems(_backpack) && !HasAnyItems(_hotbar))) return;

            var grave = Grave.Create(transform.position, capacity, _graveDespawnSeconds);
            MoveAllInto(_backpack, grave.Inventory);
            MoveAllInto(_hotbar, grave.Inventory);
            SurvainLog.Info(SurvainLog.Category.Gameplay, "Stuff déversé dans la tombe.", this);
        }

        private static bool HasAnyItems(Inventory inv)
        {
            if (inv == null) return false;
            for (int i = 0; i < inv.Capacity; i++)
                if (!inv.Get(i).IsEmpty) return true;
            return false;
        }

        private static void MoveAllInto(Inventory src, Inventory dst)
        {
            if (src == null || dst == null) return;
            for (int i = 0; i < src.Capacity; i++)
            {
                var slot = src.Get(i);
                if (slot.IsEmpty) continue;
                int leftover = dst.TryAdd(slot.Item, slot.Quantity);
                int moved = slot.Quantity - leftover;
                if (moved > 0) src.TryRemove(slot.Item, moved);
            }
        }
    }
}
