using System;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace Boltgun
{
    // The rocket motor's hiss when it lights. Uses a sound that is already in the game:
    // a clip named in the config, else a firework's launch sound, else the game's "ignite" sound.
    internal static class Sound
    {
        static AudioClip _clip;
        static bool _searched;
        static float _lastPlay;

        internal static void Reset() { _clip = null; _searched = false; }

        static AudioClip Clip()
        {
            if (Flight.Alive(_clip)) return _clip;
            if (_searched) return null;
            _searched = true;
            string wanted = (Settings.RocketSoundClip.Value ?? "").Trim();
            try
            {
                if (wanted.Length > 0)
                {
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>()))
                    {
                        var c = o?.TryCast<AudioClip>();
                        if (c != null && string.Equals(c.name, wanted, StringComparison.OrdinalIgnoreCase)) { _clip = c; break; }
                    }
                    if (_clip == null) PLog.Warn($"RocketSoundClip '{wanted}' isn't loaded; using the default sound.");
                }
                if (_clip == null)
                {
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<FireWorkParticle>()))
                    {
                        var f = o?.TryCast<FireWorkParticle>();
                        if (f == null) continue;
                        var c = f.fireAudioClip;
                        if (c != null) { _clip = c; break; }
                    }
                }
                if (_clip == null)
                {
                    var ac = AudioController.instance;
                    if (ac != null) _clip = ac.igniteClip;
                }
                if (Settings.VerboseLog.Value) ListCandidates();
            }
            catch (Exception e) { PLog.Debug("Finding the rocket sound failed: " + e.Message); }
            if (_clip != null) PLog.Info($"Rocket motor sound: {_clip.name}");
            else PLog.Warn("No rocket motor sound found; bolts ignite silently.");
            return _clip;
        }

        // VerboseLog: list loaded clips that sound like they might fit, so RocketSoundClip can be set by name.
        static void ListCandidates()
        {
            var keys = new[] { "rocket", "missile", "firework", "hiss", "launch", "whoosh", "ignite", "flare", "jet", "steam", "fuse" };
            var found = new System.Collections.Generic.List<string>();
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>()))
            {
                string n = o?.name;
                if (string.IsNullOrEmpty(n)) continue;
                string l = n.ToLowerInvariant();
                foreach (var k in keys) if (l.Contains(k)) { found.Add(n); break; }
            }
            PLog.Debug("Loaded clips that could be a rocket sound: " + (found.Count > 0 ? string.Join(", ", found) : "none"));
        }

        internal static void Ignition(Vector3 at)
        {
            float vol = Settings.RocketSoundVolume.Value;
            if (vol <= 0f) return;
            // Full auto lights a motor every 0.16 s; don't stack more than a few at once.
            if (Time.time - _lastPlay < 0.05f) return;
            var clip = Clip();
            if (clip == null) return;
            var ac = AudioController.instance;
            if (ac == null) return;
            _lastPlay = Time.time;
            ac.PlayAt(clip, at, vol);
        }
    }
}
