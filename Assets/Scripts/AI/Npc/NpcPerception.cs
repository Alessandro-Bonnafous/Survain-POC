using UnityEngine;
using Survain.Core;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Perception basique d'un PNJ : détecte la menace la plus proche dans un rayon (sur un
    /// LayerMask configurable) via un scan OverlapSphere throttlé. NpcController lit HasThreat
    /// pour forcer la fuite (FleeingState) en interruption de n'importe quel autre état.
    ///
    /// Le LayerMask "menaces" est vide au POC (pas encore d'ennemis : Sprint 4). Pour tester la
    /// fuite, placer un objet (avec collider) sur le layer de menace et l'approcher d'un PNJ.
    /// Composant optionnel sur le PNJ : absent = aucune fuite (pas d'erreur).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcPerception : MonoBehaviour
    {
        [Tooltip("Layers considérés comme menace (ennemis du Sprint 4). Vide = aucune menace.")]
        [SerializeField] private LayerMask _threatLayers;

        [Tooltip("Rayon d'acquisition d'une menace (mètres).")]
        [Min(0.5f)]
        [SerializeField] private float _detectionRadius = 8f;

        [Tooltip("Intervalle entre deux scans (secondes). Throttle pour la perf (pas chaque frame).")]
        [Min(0.02f)]
        [SerializeField] private float _scanInterval = 0.2f;

        private readonly Collider[] _hits = new Collider[8];
        private Transform _threat;
        private float _nextScanAt;

        /// <summary>Vrai si une menace est actuellement perçue.</summary>
        public bool HasThreat => _threat != null;

        /// <summary>Position de la menace courante (ou la position du PNJ si aucune).</summary>
        public Vector3 ThreatPosition => _threat != null ? _threat.position : transform.position;

        private void Update()
        {
            if (Time.time < _nextScanAt) return;
            _nextScanAt = Time.time + _scanInterval;
            Scan();
        }

        private void Scan()
        {
            Transform prev = _threat;

            // Hystérésis : on garde la menace courante tant qu'elle reste dans 1.25× le rayon
            // d'acquisition → évite le flicker fuite/idle quand la menace longe le bord du rayon.
            if (_threat != null)
            {
                float keep = _detectionRadius * 1.25f;
                if ((_threat.position - transform.position).sqrMagnitude > keep * keep)
                    _threat = null;
            }

            if (_threat == null)
            {
                int n = Physics.OverlapSphereNonAlloc(
                    transform.position, _detectionRadius, _hits, _threatLayers, QueryTriggerInteraction.Ignore);

                float best = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    float d = (_hits[i].transform.position - transform.position).sqrMagnitude;
                    if (d < best)
                    {
                        best = d;
                        _threat = _hits[i].transform;
                    }
                }
            }

            // DEBUG (#12) : trace les transitions d'acquisition/perte de menace.
            if (prev == null && _threat != null)
                SurvainLog.Info(SurvainLog.Category.AI, $"Menace détectée : {_threat.name}", this);
            else if (prev != null && _threat == null)
                SurvainLog.Info(SurvainLog.Category.AI, "Menace perdue.", this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = HasThreat ? Color.red : new Color(1f, 0.6f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
            if (HasThreat) Gizmos.DrawLine(transform.position, _threat.position);
        }
#endif
    }
}
