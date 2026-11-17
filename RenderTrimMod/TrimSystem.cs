using System.Collections.Generic;
using Game;
using Unity.Entities;
using UnityEngine.InputSystem;

namespace RenderTrimMod
{
    public partial class TrimSystem : GameSystemBase
    {
        private const string Vfx = "Game.Effects.VFXSystem";
        private const string Overlay = "Game.Rendering.OverlayRenderSystem";

        // These two systems also empty buffers/queues that other systems fill every frame (found in the decompiled
        // game code), so leaving them off makes memory grow. They are switched back on automatically after a short test.
        private const float MaxOffSeconds = 20f;

        private readonly Dictionary<string, float> _off = new Dictionary<string, float>();

        protected override void OnUpdate()
        {
            ReEnableExpired();
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f8Key.wasPressedThisFrame) Toggle(Vfx);
            if (kb.f9Key.wasPressedThisFrame) Toggle(Overlay);
            if (kb.f10Key.wasPressedThisFrame)
            {
                UploadThrottle.Enabled = !UploadThrottle.Enabled;
                Mod.Log.Info($"half-rate upload enabled={UploadThrottle.Enabled}");
            }
        }

        // Looked up by name so the mod doesn't depend on these types being public.
        private void Toggle(string typeName)
        {
            foreach (var system in World.Systems)
            {
                if (system.GetType().FullName != typeName) continue;
                system.Enabled = !system.Enabled;
                if (system.Enabled) _off.Remove(typeName); else _off[typeName] = UnityEngine.Time.realtimeSinceStartup;
                Mod.Log.Info($"{typeName} enabled={system.Enabled}" + (system.Enabled ? "" : $" (back on automatically after {MaxOffSeconds:F0}s)"));
                return;
            }
            Mod.Log.Warn($"{typeName} not found");
        }

        private void ReEnableExpired()
        {
            if (_off.Count == 0) return;
            float now = UnityEngine.Time.realtimeSinceStartup;
            foreach (var system in World.Systems)
            {
                string name = system.GetType().FullName;
                if (!_off.TryGetValue(name, out float since) || now - since < MaxOffSeconds) continue;
                system.Enabled = true;
                _off.Remove(name);
                Mod.Log.Info($"{name} enabled=True (automatic)");
                return; // dictionary changed; the next frame handles any other
            }
        }

        protected override void OnDestroy()
        {
            foreach (var system in World.Systems)
                if (_off.ContainsKey(system.GetType().FullName)) system.Enabled = true;
            base.OnDestroy();
        }
    }
}
