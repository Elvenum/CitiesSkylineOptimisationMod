using System;
using System.Diagnostics;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;

namespace DiagnosticMod
{
    // Logs one line per minute: fps, memory, entity counts, errors. Read-only, changes nothing in the game.
    public partial class DiagnosticSystem : GameSystemBase
    {
        private const float PeriodSeconds = 60f;

        private EntityQuery _buildings, _abandoned, _citizens, _vehicles;
        private float _lastTime;
        private int _lastFrame;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 64;

        protected override void OnCreate()
        {
            base.OnCreate();
            _buildings = GetEntityQuery(ComponentType.ReadOnly<Building>());
            _abandoned = GetEntityQuery(ComponentType.ReadOnly<Abandoned>());
            _citizens = GetEntityQuery(ComponentType.ReadOnly<Citizen>());
            _vehicles = GetEntityQuery(ComponentType.ReadOnly<Vehicle>());
            _lastTime = UnityEngine.Time.realtimeSinceStartup;
            _lastFrame = UnityEngine.Time.frameCount;
        }

        protected override void OnUpdate()
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            float dt = now - _lastTime;
            if (dt < PeriodSeconds) return;

            float fps = (UnityEngine.Time.frameCount - _lastFrame) / dt;
            _lastTime = now;
            _lastFrame = UnityEngine.Time.frameCount;

            const double MB = 1024.0 * 1024.0;
            double ram = Process.GetCurrentProcess().WorkingSet64 / MB;
            double managed = GC.GetTotalMemory(false) / MB;
            double unity = Profiler.GetTotalAllocatedMemoryLong() / MB;

            Mod.Log.Info(
                $"t={now:F0}s fps={fps:F1} ram={ram:F0}MB managed={managed:F0}MB unity={unity:F0}MB " +
                $"gc={GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)} " +
                $"entities={EntityManager.UniversalQuery.CalculateEntityCount()} " +
                $"buildings={_buildings.CalculateEntityCount()} abandoned={_abandoned.CalculateEntityCount()} " +
                $"citizens={_citizens.CalculateEntityCount()} vehicles={_vehicles.CalculateEntityCount()} " +
                $"errors={Mod.ErrorCount}");
        }
    }
}
