using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using Colossal.Logging;
using HarmonyLib;
using Unity.Entities;
using Unity.Profiling.LowLevel.Unsafe;

namespace DiagnosticMod
{
    // The game exposes no per-system profiler markers (v3 found none), so every SystemBase.Update is timed with a
    // Harmony patch. Time inside Update includes waiting for worker jobs, which is what blocks the main thread.
    // Systems call other systems, so each one is timed twice: total (with children) and self (children removed).
    internal static class SystemTimer
    {
        public sealed class Acc { public long TotalTicks; public long SelfTicks; public int Calls; }

        public static readonly Dictionary<Type, Acc> Data = new Dictionary<Type, Acc>();

        // shortcut: fixed depth 64 and main thread only; deeper nesting is skipped, raise if the game ever nests more.
        private const int MaxDepth = 64;
        private static readonly long[] Start = new long[MaxDepth];
        private static readonly long[] Children = new long[MaxDepth];
        private static int _depth;
        private static int _mainThread;
        private static Harmony _harmony;

        public static void Install()
        {
            _mainThread = Thread.CurrentThread.ManagedThreadId;
            _harmony = new Harmony("DiagnosticMod.timing");
            var target = typeof(SystemBase).GetMethod(nameof(SystemBase.Update),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            _harmony.Patch(target,
                prefix: new HarmonyMethod(typeof(SystemTimer), nameof(Prefix)),
                finalizer: new HarmonyMethod(typeof(SystemTimer), nameof(Finalizer)));
        }

        public static void Uninstall() => _harmony?.UnpatchAll(_harmony.Id);

        private static void Prefix()
        {
            if (Thread.CurrentThread.ManagedThreadId != _mainThread || _depth >= MaxDepth - 1) return;
            _depth++;
            Start[_depth] = Stopwatch.GetTimestamp();
            Children[_depth] = 0;
        }

        // A finalizer runs even when Update throws, so the stack always stays balanced.
        private static Exception Finalizer(SystemBase __instance, Exception __exception)
        {
            if (Thread.CurrentThread.ManagedThreadId != _mainThread || _depth <= 0) return __exception;

            long total = Stopwatch.GetTimestamp() - Start[_depth];
            long self = total - Children[_depth];
            _depth--;
            if (_depth > 0) Children[_depth] += total;

            var type = __instance.GetType();
            if (!Data.TryGetValue(type, out var acc)) Data[type] = acc = new Acc();
            acc.TotalTicks += total;
            acc.SelfTicks += self;
            acc.Calls++;
            return __exception;
        }
    }

    public partial class DiagnosticSystem
    {
        private const int TopCount = 25;
        private static readonly ILog MarkerLog = LogManager.GetLogger("DiagnosticMod.markers").SetShowsErrorsInUI(false);
        private bool _dumped;

        private static double Ms(long ticks, int frames) => ticks * 1000.0 / Stopwatch.Frequency / Math.Max(1, frames);

        private void LogTopSystems(float now, int frames, float seconds)
        {
            if (!_dumped) { DumpMarkerNames(); _dumped = true; }

            var rows = new List<KeyValuePair<Type, SystemTimer.Acc>>(SystemTimer.Data);
            rows.Sort((a, b) => b.Value.SelfTicks.CompareTo(a.Value.SelfTicks));

            long selfSum = 0;
            foreach (var r in rows) selfSum += r.Value.SelfTicks;

            var sb = new StringBuilder(
                $"TOP t={now:F0}s systems={rows.Count} frame={seconds * 1000f / Math.Max(1, frames):F1}ms " +
                $"self_total={Ms(selfSum, frames):F1}ms  (per render frame; columns: self, total incl. children, calls, ms/call self)");
            for (int i = 0; i < Math.Min(TopCount, rows.Count); i++)
            {
                var a = rows[i].Value;
                double perCall = a.SelfTicks * 1000.0 / Stopwatch.Frequency / Math.Max(1, a.Calls);
                sb.Append($"\n  {Ms(a.SelfTicks, frames),7:F2} {Ms(a.TotalTicks, frames),7:F2}  calls={a.Calls,6} {perCall,7:F3}  {rows[i].Key.FullName}");
            }
            Mod.Log.Info(sb.ToString());
            SystemTimer.Data.Clear();
        }

        // All marker names, once, to check that the game really has no per-system markers.
        private void DumpMarkerNames()
        {
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var sb = new StringBuilder();
            foreach (var h in handles)
            {
                var d = ProfilerRecorderHandle.GetDescription(h);
                sb.AppendLine($"{d.Category.Name} | {d.Name}");
            }
            MarkerLog.Info(sb.ToString());
        }
    }
}
