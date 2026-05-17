namespace Survain.Items
{
    /// <summary>
    /// Rôle métier d'un item. Exposé sur la base ItemData pour permettre
    /// filtrage rapide (UI, requêtes) sans cast vers la sous-classe concrète.
    ///
    /// Le type est une indication de rôle — la sous-classe concrète
    /// (ResourceData, ToolData, etc.) reste l'autorité sur les champs.
    /// </summary>
    public enum ItemType
    {
        Resource = 0,
        Tool = 1,
        Weapon = 2,
        Armor = 3,
        Building = 4,
        Consumable = 5,
    }
}
