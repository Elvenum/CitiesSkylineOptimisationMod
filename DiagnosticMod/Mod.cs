using Colossal.Logging;
using Game;
using Game.Modding;
using UnityEngine;

namespace DiagnosticMod
{
    public class Mod : IMod
    {
        // Own logger name => own file: Logs\DiagnosticMod.log in the game's user data folder.
        public static readonly ILog Log = LogManager.GetLogger(nameof(DiagnosticMod)).SetShowsErrorsInUI(false);

        public static int ErrorCount;
        private const int MaxDetailed = 20; // shortcut: only the first 20 errors get a stack trace, raise if you need more

        public void OnLoad(UpdateSystem updateSystem)
        {
            updateSystem.UpdateAt<DiagnosticSystem>(SystemUpdatePhase.GameSimulation);
            Application.logMessageReceived += OnUnityLog;
        }

        public void OnDispose() => Application.logMessageReceived -= OnUnityLog;

        private static void OnUnityLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;
            if (++ErrorCount <= MaxDetailed) Log.Warn($"[{type}] {message}\n{stack}");
        }
    }
}
