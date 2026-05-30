using Survain.Gameplay.Inventories;

namespace Survain.Gameplay.Interaction
{
    /// <summary>
    /// Élément du monde activable par le joueur via la touche d'interaction générique (E).
    /// Implémenté par les chantiers (déposer des ressources), les coffres (ouvrir le
    /// stockage), et plus tard les PNJ (dialogue), portes, etc.
    ///
    /// Un seul composant côté joueur (PlayerInteractor) vise via raycast caméra, affiche
    /// <see cref="GetInteractionPrompt"/> et appelle <see cref="Interact"/> sur E. Évite la
    /// multiplication d'interacteurs concurrents (un par type de cible).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Faux quand la cible ne doit plus être visée (ex. chantier terminé).</summary>
        bool IsInteractable { get; }

        /// <summary>Texte affiché dans le prompt d'interaction (inclut la touche, ex. "[E] Ouvrir Coffre").</summary>
        string GetInteractionPrompt();

        /// <summary>
        /// Déclenche l'interaction. <paramref name="actorInventory"/> est l'inventaire de
        /// l'acteur (sac à dos du joueur) — source/destination pour les dépôts et transferts.
        /// </summary>
        void Interact(Inventory actorInventory);

        /// <summary>Active/désactive la surbrillance (visée par le joueur).</summary>
        void SetHighlighted(bool highlighted);
    }
}
