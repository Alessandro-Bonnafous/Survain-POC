namespace Survain.Items
{
    /// <summary>
    /// Tier de qualité d'un item dans la progression du craft.
    /// Référence vision : 3 niveaux — basique (gris), sauvage (vert), supérieur (bleu).
    ///
    /// Les ressources brutes (bois, pierre, fibre) ont vocation à rester Basic.
    /// Tools/Weapons/Armors craftés progressent Basic → Wild → Superior.
    /// </summary>
    public enum ItemTier
    {
        Basic = 0,
        Wild = 1,
        Superior = 2,
    }
}
