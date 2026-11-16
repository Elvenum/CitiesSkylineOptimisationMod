using System;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Vehicles;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace DiagnosticMod
{
    // Logs one line per minute: fps, memory, CPU/GPU frame times, entity counts, errors.
    // Read-only, changes nothing in the game.
    public partial class DiagnosticSystem : GameSystemBase
    {
        private const float PeriodSeconds = 60f;
        private const double MB = 1024.0 * 1024.0;

        private EntityQuery _buildings, _abandoned, _citizens, _vehicles;
        private ProfilerRecorder _sysMem, _mainThread;
        private readonly FrameTiming[] _ft = new FrameTiming[1];

        private float _lastTime = -1f; // -1 = not started: the first window includes loading, so it is skipped
        private int _lastFrame;
        private int _samples, _ftSamples;
        private double _cpu, _main, _render, _gpu, _mainProfiler;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 64;

        protected override void OnCreate()
        {
            base.OnCreate();
            _buildings = GetEntityQuery(ComponentType.ReadOnly<Building>());
            _abandoned = GetEntityQuery(ComponentType.ReadOnly<Abandoned>());
            _citizens = GetEntityQuery(ComponentType.ReadOnly<Citizen>());
            _vehicles = GetEntityQuery(ComponentType.ReadOnly<Vehicle>());
            // Process.WorkingSet64 returned 0 under this runtime (v1), so ask the Unity profiler instead.
            _sysMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
        }

        protected override void OnDestroy()
        {
            _sysMem.Dispose();
            _mainThread.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (_lastTime < 0f)
            {
                _lastTime = now;
                _lastFrame = UnityEngine.Time.frameCount;
                return;
            }

            Sample();

            float dt = now - _lastTime;
            if (dt < PeriodSeconds) return;

            float fps = (UnityEngine.Time.frameCount - _lastFrame) / dt;
            _lastTime = now;
            _lastFrame = UnityEngine.Time.frameCount;

            double ft = Math.Max(_ftSamples, 1);
            double sm = Math.Max(_samples, 1);
            Mod.Log.Info(
                $"t={now:F0}s fps={fps:F1} " +
                $"cpu={_cpu / ft:F1}ms main={_main / ft:F1}ms render={_render / ft:F1}ms gpu={_gpu / ft:F1}ms " +
                $"mainprof={_mainProfiler / sm / 1e6:F1}ms ftsamples={_ftSamples}/{_samples} " +
                $"sysmem={_sysMem.LastValue / MB:F0}MB managed={GC.GetTotalMemory(false) / MB:F0}MB " +
                $"unity={Profiler.GetTotalAllocatedMemoryLong() / MB:F0}MB " +
                $"gc={GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)} " +
                $"entities={EntityManager.UniversalQuery.CalculateEntityCount()} " +
                $"buildings={_buildings.CalculateEntityCount()} abandoned={_abandoned.CalculateEntityCount()} " +
                $"citizens={_citizens.CalculateEntityCount()} vehicles={_vehicles.CalculateEntityCount()} " +
                $"errors={Mod.ErrorCount}");

            _samples = _ftSamples = 0;
            _cpu = _main = _render = _gpu = _mainProfiler = 0;
        }

        // Called every 64 sim frames; the log line prints the average of these samples.
        private void Sample()
        {
            _samples++;
            _mainProfiler += _mainThread.LastValue; // ns, fallback if FrameTimingManager reports nothing

            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _ft) == 0) return;

            _ftSamples++;
            _cpu += _ft[0].cpuFrameTime;
            _main += _ft[0].cpuMainThreadFrameTime;
            _render += _ft[0].cpuRenderThreadFrameTime;
            _gpu += _ft[0].gpuFrameTime;
        }
    }
}
