namespace Survain.AI.Npc
{
    /// <summary>
    /// Métier d'un PNJ (#14). Assigné via le contremaître (point d'interaction unique du village,
    /// cf. journal CLAUDE.md). Le comportement de travail associé (récolte, dépôt, construction)
    /// arrive en phase 2 ; l'assignation en jeu via le panneau du contremaître en phase 3.
    /// </summary>
    public enum NpcJob
    {
        /// <summary>Aucun métier : le PNJ erre (Idle/Wander).</summary>
        SansEmploi = 0,

        /// <summary>Récolte les arbres → dépose au coffre le plus proche.</summary>
        Bucheron = 1,

        /// <summary>Récolte les roches → dépose au coffre le plus proche.</summary>
        Mineur = 2,

        /// <summary>Alimente les chantiers (ConstructionSite.Deposit) depuis le stock du village.</summary>
        Constructeur = 3,

        /// <summary>Manager du village : point d'interaction unique, ne déserte pas. Assigné au démarrage.</summary>
        Contremaitre = 4,
    }
}
