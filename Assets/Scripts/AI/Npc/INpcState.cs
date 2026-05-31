namespace Survain.AI.Npc
{
    /// <summary>
    /// État de la machine à états d'un PNJ. Implémentation polymorphe (une classe par état)
    /// pour rester facilement extensible (critère #12) : chaque état isole sa logique
    /// d'entrée/sortie et de transition.
    ///
    /// Phase 1 (#12) : Idle, Wander. À venir : Working, Eating, Sleeping, Fleeing (#13/#14/#15).
    /// </summary>
    public interface INpcState
    {
        /// <summary>Appelé une fois quand le PNJ entre dans cet état.</summary>
        void Enter(NpcController npc);

        /// <summary>Appelé chaque frame tant que le PNJ est dans cet état.</summary>
        void Tick(NpcController npc);

        /// <summary>Appelé une fois quand le PNJ quitte cet état.</summary>
        void Exit(NpcController npc);
    }
}
