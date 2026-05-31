using UnityEngine;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Hashes des paramètres Animator des PNJ (NpcAvatar.controller), centralisés pour être
    /// partagés entre NpcController (locomotion, paramètre "speed") et les états qui pilotent
    /// une anim dédiée (WorkingState → "isWorking"). Pattern static readonly int (cf. #33).
    /// </summary>
    public static class NpcAnimParams
    {
        /// <summary>Magnitude de la vitesse de l'agent (float) → blend tree de locomotion.</summary>
        public static readonly int Speed = Animator.StringToHash("speed");

        /// <summary>Le PNJ est en train de travailler (bool) → état Work (anim de travail).</summary>
        public static readonly int Working = Animator.StringToHash("isWorking");
    }
}
