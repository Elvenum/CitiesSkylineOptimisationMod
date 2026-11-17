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
            Log.Info("RenderTrimMod v2.1 | F8 = toggle effects (VFXSystem), F9 = toggle overlays (OverlayRenderSystem), " +
                     "F10 = toggle half-rate bone/light upload (experimental)");
            try { UploadThrottle.Install(); }
            catch (System.Exception e) { Log.Warn($"F10 upload throttle disabled: {e}"); } // F8/F9 still work
            updateSystem.UpdateAt<TrimSystem>(SystemUpdatePhase.MainLoop); // every frame, also while paused
        }

        public void OnDispose() => UploadThrottle.Uninstall();
    }
}
