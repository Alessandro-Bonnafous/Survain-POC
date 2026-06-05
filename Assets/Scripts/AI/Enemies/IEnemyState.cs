namespace Survain.AI.Enemies
{
    /// <summary>
    /// État de la machine à états d'un ennemi (#17). Implémentation polymorphe (une classe par
    /// état : Patrol / Chase / Attack / Return), même pattern que <c>INpcState</c> — chaque état
    /// isole son entrée/sortie et sa logique de tick.
    /// </summary>
    public interface IEnemyState
    {
        void Enter(EnemyController enemy);
        void Tick(EnemyController enemy);
        void Exit(EnemyController enemy);
    }
}
