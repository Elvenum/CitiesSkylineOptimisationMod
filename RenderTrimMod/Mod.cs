using Colossal.Logging;
using Game;
using Game.Modding;

namespace RenderTrimMod
{
    // EXPERIMENT: switches two cosmetic render systems on/off with a key, to measure what they cost.
    // Nothing is changed until a key is pressed, and everything is switched back on when the mod unloads.
    public class Mod : IMod
    {
        public static readonly ILog Log = LogManager.GetLogger(nameof(RenderTrimMod)).SetShowsErrorsInUI(false);

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info("RenderTrimMod v1 | F8 = toggle effects (VFXSystem), F9 = toggle overlays (OverlayRenderSystem)");
            updateSystem.UpdateAt<TrimSystem>(SystemUpdatePhase.MainLoop); // every frame, also while paused
        }

        public void OnDispose() { }
    }
}
