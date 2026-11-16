using System;
using System.Collections.Generic;
using System.Text;
using Colossal.Logging;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;

namespace DiagnosticMod
{
    // Per-system CPU time. We don't know how this game version names its profiler markers, so we
    // attach to every time marker named "Game.*System*" and dump the names once to
    // DiagnosticMod.markers.log, so the next version can be adjusted from real data.
    public partial class DiagnosticSystem
    {
        // shortcut: 1024 samples per marker, so the average is right only below ~17 fps over 60 s; raise if FPS gets higher.
        private const int Capacity = 1024;
        private const int MaxMarkers = 1000;
        private const int TopCount = 15;

        private static readonly ILog MarkerLog = LogManager.GetLogger("DiagnosticMod.markers").SetShowsErrorsInUI(false);

        private List<string> _names;
        private List<ProfilerRecorder> _recorders;

        // Markers appear lazily when systems first run, so they are attached at the end of the first window.
        private void LogTopSystems(float now, int frames)
        {
            if (_recorders == null)
            {
                AttachRecorders();
                return;
            }

            int used = Math.Max(1, Math.Min(frames, Capacity));
            var totals = new List<KeyValuePair<double, int>>(_recorders.Count);
            for (int i = 0; i < _recorders.Count; i++)
            {
                var r = _recorders[i];
                int n = Math.Min(r.Count, used);
                double sum = 0;
                for (int s = r.Count - n; s < r.Count; s++) sum += r.GetSample(s).Value;
                totals.Add(new KeyValuePair<double, int>(sum / used / 1e6, i)); // ns/frame -> ms/frame
            }
            totals.Sort((a, b) => b.Key.CompareTo(a.Key));

            var sb = new StringBuilder($"TOP t={now:F0}s (ms per frame):");
            for (int k = 0; k < Math.Min(TopCount, totals.Count); k++)
                sb.Append($"\n  {totals[k].Key,7:F2}  {_names[totals[k].Value]}");
            Mod.Log.Info(sb.ToString());
        }

        private void AttachRecorders()
        {
            _names = new List<string>();
            _recorders = new List<ProfilerRecorder>();

            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);

            var dump = new StringBuilder();
            foreach (var h in handles)
            {
                var d = ProfilerRecorderHandle.GetDescription(h);
                if (d.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds) continue;
                string name = d.Name;
                if (!name.StartsWith("Game.", StringComparison.Ordinal) || name.IndexOf("System", StringComparison.Ordinal) < 0) continue;

                dump.AppendLine($"{d.Category.Name} | {name}");
                if (_recorders.Count >= MaxMarkers) continue;
                _names.Add(name);
                _recorders.Add(new ProfilerRecorder(h, Capacity, ProfilerRecorderOptions.Default));
            }

            Mod.Log.Info($"System timing: {handles.Count} markers available, {_recorders.Count} attached (see DiagnosticMod.markers.log)");
            MarkerLog.Info(dump.ToString());
        }

        private void DisposeSystemRecorders()
        {
            if (_recorders == null) return;
            foreach (var r in _recorders) r.Dispose();
            _recorders = null;
        }
    }
}
