using System;
using System.IO;
using UnityEngine;

namespace Boltgun
{
    // Installs from Steam Workshop put the item packs where the game's own mod loader finds them.
    // Installs from Girthy Mods (Hefty) put everything in BepInEx\plugins\<mod>\, which the game doesn't scan,
    // so those builds carry the item packs in an "items" folder next to Boltgun.dll and load them here,
    // right after the game has loaded its own mods. Packs the game already loaded are skipped.
    internal static class Bundled
    {
        internal static void LoadMissing()
        {
            string dir;
            try { dir = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? "", "items"); }
            catch { return; }
            if (!Directory.Exists(dir)) return;

            var loaded = ModLoader.loadedMods;
            var handlers = ModLoader.handlers;
            if (handlers == null) { PLog.Warn("The game's mod handlers aren't ready; Boltgun items not loaded."); return; }

            int ok = 0, skipped = 0, failed = 0;
            foreach (var sub in Directory.GetDirectories(dir))
            {
                string folder = Path.GetFileName(sub);
                IModHandler handler = null;
                foreach (var kv in handlers)
                {
                    var h = kv.Value;
                    if (h == null) continue;
                    string hs = null, ht = null;
                    try { hs = h.SubFolder; ht = h.ModType; } catch { }
                    if (string.Equals(hs, folder, StringComparison.OrdinalIgnoreCase) || string.Equals(ht, folder, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(kv.Key, folder, StringComparison.OrdinalIgnoreCase)) { handler = h; break; }
                }
                if (handler == null) { PLog.Warn($"No game mod handler for '{folder}'"); continue; }

                foreach (var file in Directory.GetFiles(sub, "*.zedmod"))
                {
                    string guid = Path.GetFileNameWithoutExtension(file);
                    if (loaded != null && loaded.ContainsKey(guid)) { skipped++; continue; }
                    try
                    {
                        if (ModLoader.TryLoadZedmod(file, handler)) ok++;
                        else { failed++; PLog.Warn("The game refused " + Path.GetFileName(file)); }
                    }
                    catch (Exception e) { failed++; PLog.Error("Loading " + Path.GetFileName(file) + " failed", e); }
                }
            }
            PLog.Info($"Bundled items: {ok} loaded, {skipped} already loaded by the game, {failed} failed.");
            if (ok > 0)
            {
                try { ModLocaleManager.ReapplyAllLocales(); } catch (Exception e) { PLog.Debug("Locale refresh: " + e.Message); }
            }
        }
    }
}
