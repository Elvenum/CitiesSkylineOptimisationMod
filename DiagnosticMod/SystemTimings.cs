using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Colossal.Logging;
using HarmonyLib;
using Unity.Entities;
using Unity.Profiling.LowLevel.Unsafe;

namespace DiagnosticMod
{
    // The game exposes no per-system profiler markers (v3 found none), so every SystemBase.Update is timed with a
    // Harmony patch. Time inside Update includes waiting for worker jobs, which is what blocks the main thread.
    internal static class SystemTimer
    {
        public sealed class Acc { public long Ticks; public int Calls; }

        public static readonly Dictionary<Type, Acc> Data = new Dictionary<Type, Acc>();
        private static Harmony _harmony;

        public static void Install()
        {
            _harmony = new Harmony("DiagnosticMod.timing");
            var target = typeof(SystemBase).GetMethod(nameof(SystemBase.Update),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            _harmony.Patch(target,
                prefix: new HarmonyMethod(typeof(SystemTimer), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(SystemTimer), nameof(Postfix)));
        }

        public static void Uninstall() => _harmony?.UnpatchAll(_harmony.Id);

        private static void Prefix(out long __state) => __state = Stopwatch.GetTimestamp();

        private static void Postfix(SystemBase __instance, long __state)
        {
            long d = Stopwatch.GetTimestamp() - __state;
            var type = __instance.GetType();
            if (!Data.TryGetValue(type, out var acc)) Data[type] = acc = new Acc();
            acc.Ticks += d;
            acc.Calls++;
        }
    }

    public partial class DiagnosticSystem
    {
        private const int TopCount = 20;
        private static readonly ILog MarkerLog = LogManager.GetLogger("DiagnosticMod.markers").SetShowsErrorsInUI(false);
        private bool _dumped;

        private void LogTopSystems(float now, int frames)
        {
            if (!_dumped) { DumpMarkerNames(); _dumped = true; }

            var rows = new List<KeyValuePair<double, KeyValuePair<Type, SystemTimer.Acc>>>(SystemTimer.Data.Count);
            double total = 0;
            foreach (var kv in SystemTimer.Data)
            {
                double ms = kv.Value.Ticks * 1000.0 / Stopwatch.Frequency / Math.Max(1, frames); // ms per render frame
                total += ms;
                rows.Add(new KeyValuePair<double, KeyValuePair<Type, SystemTimer.Acc>>(ms, kv));
            }
            rows.Sort((a, b) => b.Key.CompareTo(a.Key));

            var sb = new StringBuilder($"TOP t={now:F0}s systems={rows.Count} total={total:F1}ms/frame (ms per render frame, calls, ms per call):");
            for (int i = 0; i < Math.Min(TopCount, rows.Count); i++)
            {
                var acc = rows[i].Value.Value;
                double perCall = acc.Ticks * 1000.0 / Stopwatch.Frequency / Math.Max(1, acc.Calls);
                sb.Append($"\n  {rows[i].Key,7:F2}  calls={acc.Calls,6}  {perCall,7:F3}  {rows[i].Value.Key.FullName}");
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
