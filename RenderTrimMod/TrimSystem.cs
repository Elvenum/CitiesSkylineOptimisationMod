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

        private readonly HashSet<string> _off = new HashSet<string>();

        protected override void OnUpdate()
        {
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
                if (system.Enabled) _off.Remove(typeName); else _off.Add(typeName);
                Mod.Log.Info($"{typeName} enabled={system.Enabled}");
                return;
            }
            Mod.Log.Warn($"{typeName} not found");
        }

        protected override void OnDestroy()
        {
            foreach (var system in World.Systems)
                if (_off.Contains(system.GetType().FullName)) system.Enabled = true;
            base.OnDestroy();
        }
    }
}
