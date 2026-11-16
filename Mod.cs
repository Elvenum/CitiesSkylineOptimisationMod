using Colossal.Logging;
using Game;
using Game.Modding;

namespace CleanupMod
{
    public class Mod : IMod
    {
        public static readonly ILog Log = LogManager.GetLogger(nameof(CleanupMod)).SetShowsErrorsInUI(false);

        public void OnLoad(UpdateSystem updateSystem)
        {
            updateSystem.UpdateAt<CleanupSystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose() { }
    }
}
