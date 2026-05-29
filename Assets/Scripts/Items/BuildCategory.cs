namespace Survain.Items
{
    /// <summary>
    /// Famille d'une structure constructible. Sert au filtrage, au choix de la couleur
    /// du placeholder, et plus tard aux règles de snap (un mur snappe sur une fondation,
    /// un toit sur un mur, etc. — affiné au-delà du POC).
    /// </summary>
    public enum BuildCategory
    {
        Shelter,    // bâtiment d'habitation/abri entier (hutte, cabane...)
        Storage,    // bâtiment de stockage entier
        Functional, // coffre, feu de camp, atelier... (structures à fonction de jeu)
        Foundation, // pièces modulaires (conservées pour usage futur éventuel)
        Wall,
        Roof,
        Door,
        Window,
    }
}
