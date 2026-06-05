using UnityEngine;
using Survain.Gameplay.Buildings;
using Survain.Gameplay.Inventories;
using Survain.Gameplay.Items;
using Survain.Items;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Métier de récolte (#14 phase 2A) : bûcheron (arbres) ou mineur (roches) selon le type
    /// d'outil ciblé. Boucle : trouver le nœud compatible le plus proche → s'en approcher →
    /// le récolter (crédit direct dans l'inventaire porté) → porter au coffre le plus proche →
    /// déposer → recommencer. Quand il n'y a rien à récolter, attend puis re-scanne.
    ///
    /// État persistant : il ne sort de lui-même que via une interruption prioritaire du
    /// NpcController (fuite/désertion/faim). La cadence de récolte est modulée par le moral
    /// (WorkSpeedMultiplier, #13). L'anim de travail (isWorking) est jouée pendant la récolte.
    /// </summary>
    public sealed class GatherJobState : INpcState
    {
        private enum Phase { Search, GoToNode, Harvest, GoToChest, Wait }

        private const float ArriveDistance = 2f;
        private const float RescanDelay = 1.5f;
        private const float MoveTimeout = 12f; // abandon d'une cible non atteinte → anti-blocage

        private readonly ToolType _toolType;

        private Phase _phase;
        private ResourceNode _node;
        private StorageContainer _chest;
        private float _nextHitAt;
        private float _rescanAt;
        private float _deadline;

        public GatherJobState(ToolType toolType) => _toolType = toolType;

        /// <summary>Coffre (StorageContainer) le plus proche. Centralise la recherche de lieu de stockage.</summary>
        public static bool HasStorage(Vector3 from, out StorageContainer chest)
        {
            var building = Building.FindNearest(from, b => b.GetComponent<StorageContainer>() != null);
            chest = building != null ? building.GetComponent<StorageContainer>() : null;
            return chest != null;
        }

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
                case Phase.GoToNode: TickGoToNode(npc); break;
                case Phase.Harvest: TickHarvest(npc); break;
                case Phase.GoToChest: TickGoToChest(npc); break;
                case Phase.Wait: TickWait(npc); break;
            }
        }

        public void Exit(NpcController npc)
        {
            SetWorking(npc, false);
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = false;
        }

        private void TickSearch(NpcController npc)
        {
            SetWorking(npc, false);

            _node = ResourceNode.FindNearest(npc.transform.position,
                n => !n.IsDepleted && n.Data != null && n.Data.RequiredTool == _toolType);

            if (_node != null)
            {
                GoTo(npc, _node.transform.position);
                _phase = Phase.GoToNode;
            }
            else if (!CarriedEmpty(npc))
            {
                BeginChest(npc);
            }
            else
            {
                _rescanAt = Time.time + RescanDelay;
                _phase = Phase.Wait;
            }
        }

        private void TickGoToNode(NpcController npc)
        {
            if (_node == null || _node.IsDepleted) { _phase = Phase.Search; return; }

            if (Time.time > _deadline) // nœud inatteignable → on lâche et on patiente
            {
                _node = null;
                _rescanAt = Time.time + RescanDelay;
                _phase = Phase.Wait;
                return;
            }

            if (Arrived(npc, _node.transform.position))
            {
                Stop(npc);
                SetWorking(npc, true);
                _nextHitAt = Time.time; // premier coup immédiat
                _phase = Phase.Harvest;
            }
        }

        private void TickHarvest(NpcController npc)
        {
            if (_node == null || _node.IsDepleted)
            {
                SetWorking(npc, false);
                if (!CarriedEmpty(npc)) BeginChest(npc);
                else _phase = Phase.Search;
                return;
            }

            if (Time.time < _nextHitAt) return;

            _node.HarvestHit(npc.Carried);

            float mult = npc.Needs != null ? Mathf.Max(0.1f, npc.Needs.WorkSpeedMultiplier) : 1f;
            _nextHitAt = Time.time + _node.Data.HarvestSeconds / mult;
        }

        private void TickGoToChest(NpcController npc)
        {
            if (_chest == null) { _phase = Phase.Search; return; } // coffre disparu

            if (Time.time > _deadline) // coffre inatteignable → on patiente puis on réessaie
            {
                _rescanAt = Time.time + RescanDelay;
                _phase = Phase.Wait;
                return;
            }

            if (Arrived(npc, _chest.transform.position))
            {
                Stop(npc);
                DepositAll(npc);
                _phase = Phase.Search;
            }
        }

        private void TickWait(NpcController npc)
        {
            if (Time.time >= _rescanAt) _phase = Phase.Search;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private void BeginChest(NpcController npc)
        {
            if (HasStorage(npc.transform.position, out _chest))
            {
                GoTo(npc, _chest.transform.position);
                _phase = Phase.GoToChest;
            }
            else
            {
                // Pas de coffre : on garde la récolte et on réessaiera plus tard.
                _rescanAt = Time.time + RescanDelay;
                _phase = Phase.Wait;
            }
        }

        private void DepositAll(NpcController npc)
        {
            var carried = npc.Carried;
            var chest = _chest.Inventory;
            if (chest == null) return;

            for (int i = 0; i < carried.Capacity; i++)
            {
                var slot = carried.Get(i);
                if (slot.IsEmpty) continue;
                // TryAdd retourne le reliquat non placé → on retire du sac ce qui est entré au coffre.
                int leftover = chest.TryAdd(slot.Item, slot.Quantity);
                int deposited = slot.Quantity - leftover;
                if (deposited > 0) carried.TryRemove(slot.Item, deposited);
            }
        }

        private static bool CarriedEmpty(NpcController npc)
        {
            var c = npc.Carried;
            for (int i = 0; i < c.Capacity; i++)
                if (!c.Get(i).IsEmpty) return false;
            return true;
        }

        private void GoTo(NpcController npc, Vector3 pos)
        {
            _deadline = Time.time + MoveTimeout; // arme l'anti-blocage pour ce trajet
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

            // L'agent s'est arrêté sans pouvoir s'approcher davantage (nœud carvé par son
            // NavMeshObstacle) → on considère qu'il est arrivé au plus près.
            return agent.remainingDistance <= agent.stoppingDistance + 0.2f
                   && agent.velocity.sqrMagnitude < 0.04f;
        }

        private static void SetWorking(NpcController npc, bool working)
        {
            if (npc.Animator != null) npc.Animator.SetBool(NpcAnimParams.Working, working);
        }
    }
}
