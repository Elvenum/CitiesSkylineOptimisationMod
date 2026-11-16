using Game;
using Game.Buildings;
using Game.Common;
using Unity.Entities;

namespace CleanupMod
{
    // Demolishes abandoned buildings so their entities (and the sim work they generate) go away.
    public partial class CleanupSystem : GameSystemBase
    {
        private EntityQuery _abandoned;

        // shortcut: fixed interval, no settings UI; add options when someone asks.
        public override int GetUpdateInterval(SystemUpdatePhase phase) => 512; // ~ every 512 sim frames

        protected override void OnCreate()
        {
            base.OnCreate();
            _abandoned = GetEntityQuery(
                ComponentType.ReadOnly<Abandoned>(),
                ComponentType.ReadOnly<Building>(),
                ComponentType.Exclude<Deleted>());
        }

        protected override void OnUpdate()
        {
            int count = _abandoned.CalculateEntityCount();
            if (count == 0) return;

            EntityManager.AddComponent<Deleted>(_abandoned);
            Mod.Log.Info($"Demolished {count} abandoned building(s)");
        }
    }
}
