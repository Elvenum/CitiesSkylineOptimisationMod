using Game.Rendering;
using HarmonyLib;

namespace RenderTrimMod
{
    // EXPERIMENT: when on, the per-frame bone/light upload (Prepare + Upload systems) runs every other frame.
    // The game's own two systems form a pair: Prepare allocates a buffer that Upload frees. The decision is made
    // once in Prepare and Upload follows it, so the pair is never half-run (that would leak the buffer).
    // Skipped frames leave the "updated" flags set, so the data goes out on the next frame instead.
    internal static class UploadThrottle
    {
        public static bool Enabled;

        private static bool _flip, _skip;
        private static Harmony _harmony;

        public static void Install()
        {
            _harmony = new Harmony("RenderTrimMod.upload");
            _harmony.Patch(AccessTools.DeclaredMethod(typeof(ProceduralUploadSystem.Prepare), "OnUpdate"),
                prefix: new HarmonyMethod(typeof(UploadThrottle), nameof(PrepareOnUpdate)));
            _harmony.Patch(AccessTools.DeclaredMethod(typeof(ProceduralUploadSystem), "OnUpdate"),
                prefix: new HarmonyMethod(typeof(UploadThrottle), nameof(UploadOnUpdate)));
        }

        public static void Uninstall()
        {
            Enabled = false;
            _harmony?.UnpatchAll(_harmony.Id);
        }

        private static bool PrepareOnUpdate()
        {
            _flip = !_flip;
            _skip = Enabled && _flip;
            return !_skip; // false = skip the original method
        }

        private static bool UploadOnUpdate() => !_skip;
    }
}
