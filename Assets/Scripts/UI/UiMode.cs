using UnityEngine;
using Survain.Gameplay.Player;

namespace Survain.UI
{
    /// <summary>
    /// Mode « UI ouverte » centralisé, avec comptage de références : plusieurs panneaux peuvent
    /// être ouverts simultanément (inventaire, coffre, gestion du village…). Tant qu'au moins un
    /// est ouvert → curseur libéré + orbite caméra figée ; quand tous sont fermés → curseur
    /// reverrouillé + caméra rendue au joueur.
    ///
    /// Résout le conflit « dernier qui écrit gagne » : un panneau qui se ferme ne reverrouille
    /// plus le curseur si un autre panneau est encore ouvert (#14). Chaque panneau appelle
    /// Push() à l'ouverture et Pop() à la fermeture (transitions équilibrées).
    /// </summary>
    public static class UiMode
    {
        private static int _open;
        private static PlayerCameraRig _rig;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _open = 0;
            _rig = null;
        }

        /// <summary>Un panneau s'ouvre. Au premier panneau, bascule en mode UI.</summary>
        public static void Push()
        {
            _open++;
            if (_open == 1) Apply(true);
        }

        /// <summary>Un panneau se ferme. Au dernier panneau fermé, rend la main au joueur.</summary>
        public static void Pop()
        {
            if (_open == 0) return;
            _open--;
            if (_open == 0) Apply(false);
        }

        private static void Apply(bool uiOpen)
        {
            Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = uiOpen;

            if (_rig == null && Camera.main != null) _rig = Camera.main.GetComponent<PlayerCameraRig>();
            if (_rig != null) _rig.RotationLocked = uiOpen;
        }
    }
}
