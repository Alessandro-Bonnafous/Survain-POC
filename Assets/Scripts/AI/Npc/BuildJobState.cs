using UnityEngine;
using Survain.Gameplay.Buildings;
using Survain.Gameplay.Inventories;
using Survain.Items;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Métier de constructeur (#14 phase 2B) : alimente les chantiers du village. Boucle :
    /// trouver le chantier actif le plus proche → s'il manque des ressources requises, aller au
    /// coffre le plus proche en puiser ce qui manque → porter au chantier → déposer
    /// (ConstructionSite.Deposit) → recommencer. Attend si rien à construire ou coffre à sec.
    ///
    /// État persistant (comme GatherJobState) : ne sort de lui-même que via une interruption
    /// prioritaire du NpcController (fuite/désertion/faim).
    /// </summary>
    public sealed class BuildJobState : INpcState
    {
        private enum Phase { Search, GoToChest, GoToSite, Wait }

        private const float ArriveDistance = 2.5f;
        private const float RescanDelay = 1.5f;

        private Phase _phase;
        private ConstructionSite _site;
        private StorageContainer _chest;
        private float _rescanAt;

        public void Enter(NpcController npc)
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = false;
            _phase = Phase.Search;
        }

        public void Tick(NpcController npc)
        {
            switch (_phase)
            {
                case Phase.Search: TickSearch(npc); break;
                case Phase.GoToChest: TickGoToChest(npc); break;
                case Phase.GoToSite: TickGoToSite(npc); break;
                case Phase.Wait: TickWait(npc); break;
            }
        }

        public void Exit(NpcController npc)
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = false;
        }

        private void TickSearch(NpcController npc)
        {
            if (!ConstructionSite.HasActive(npc.transform.position, out _site))
            {
                _rescanAt = Time.time + RescanDelay;
                _phase = Phase.Wait;
                return;
            }

            // Si on porte déjà des ressources utiles au chantier → aller déposer.
            if (CarriesNeeded(npc, _site))
            {
                GoTo(npc, _site.transform.position);
                _phase = Phase.GoToSite;
            }
            else if (GatherJobState.HasStorage(npc.transform.position, out _chest))
            {
                GoTo(npc, _chest.transform.position);
                _phase = Phase.GoToChest;
            }
            else
            {
                // Pas de coffre où puiser → on attend.
                _rescanAt = Time.time + RescanDelay;
                _phase = Phase.Wait;
            }
        }

        private void TickGoToChest(NpcController npc)
        {
            if (_chest == null || _site == null || _site.IsComplete) { _phase = Phase.Search; return; }

            if (Arrived(npc, _chest.transform.position))
            {
                Stop(npc);
                WithdrawNeeded(npc, _site, _chest);

                if (CarriesNeeded(npc, _site))
                {
                    GoTo(npc, _site.transform.position);
                    _phase = Phase.GoToSite;
                }
                else
                {
                    // Le coffre n'a pas (encore) les ressources requises → on patiente.
                    _rescanAt = Time.time + RescanDelay;
                    _phase = Phase.Wait;
                }
            }
        }

        private void TickGoToSite(NpcController npc)
        {
            if (_site == null || _site.IsComplete) { _phase = Phase.Search; return; }

            if (Arrived(npc, _site.transform.position))
            {
                Stop(npc);
                _site.Deposit(npc.Carried);
                _phase = Phase.Search; // chantier complété ? sinon on refait un tour (coffre)
            }
        }

        private void TickWait(NpcController npc)
        {
            if (Time.time >= _rescanAt) _phase = Phase.Search;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        /// <summary>Le PNJ porte-t-il au moins une ressource encore requise par le chantier ?</summary>
        private static bool CarriesNeeded(NpcController npc, ConstructionSite site)
        {
            var carried = npc.Carried;
            for (int i = 0; i < carried.Capacity; i++)
            {
                var slot = carried.Get(i);
                if (!slot.IsEmpty && site.RemainingFor(slot.Item) > 0) return true;
            }
            return false;
        }

        /// <summary>Transfère du coffre vers le sac les ressources encore requises par le chantier.</summary>
        private static void WithdrawNeeded(NpcController npc, ConstructionSite site, StorageContainer chest)
        {
            var cost = site.Target != null ? site.Target.Cost : null;
            if (cost == null) return;

            var carried = npc.Carried;
            var box = chest.Inventory;
            if (box == null) return;

            for (int i = 0; i < cost.Length; i++)
            {
                var item = cost[i].Item;
                if (item == null) continue;

                int need = site.RemainingFor(item);
                if (need <= 0) continue;

                int want = Mathf.Min(need, box.Count(item));
                if (want <= 0) continue;

                int removed = box.TryRemove(item, want);
                int leftover = carried.TryAdd(item, removed); // ce qui ne tient pas dans le sac
                if (leftover > 0) box.TryAdd(item, leftover);  // remis au coffre
            }
        }

        private static void GoTo(NpcController npc, Vector3 pos)
        {
            if (!npc.Agent.isOnNavMesh) return;
            npc.Agent.isStopped = false;
            npc.Agent.SetDestination(pos);
        }

        private static void Stop(NpcController npc)
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = true;
        }

        private static bool Arrived(NpcController npc, Vector3 pos)
        {
            var agent = npc.Agent;
            if (agent.pathPending) return false;

            Vector3 a = npc.transform.position; a.y = 0f;
            Vector3 b = pos; b.y = 0f;
            if ((a - b).sqrMagnitude <= ArriveDistance * ArriveDistance) return true;

            return agent.remainingDistance <= agent.stoppingDistance + 0.2f
                   && agent.velocity.sqrMagnitude < 0.04f;
        }
    }
}
