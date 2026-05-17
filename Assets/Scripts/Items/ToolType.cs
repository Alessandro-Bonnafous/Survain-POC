namespace Survain.Items
{
    /// <summary>
    /// Famille d'outil. Utilisé pour matcher un ToolData avec un ResourceNodeData
    /// (un arbre exige un Axe, un rocher exige un Pickaxe, etc.).
    ///
    /// None = outil non typé / récolte à mains nues (cas fibre par exemple).
    /// </summary>
    public enum ToolType
    {
        None = 0,
        Axe = 1,
        Pickaxe = 2,
        Shovel = 3,
        Sickle = 4,
    }
}
