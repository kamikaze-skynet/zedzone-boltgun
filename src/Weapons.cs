using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boltgun
{
    // Which item ids fire rocket bolts. Mod items get their ids from the game's mod loader at
    // start-up, so they are resolved lazily and re-checked until every configured guid is found.
    internal static class Weapons
    {
        static readonly HashSet<int> _ids = new HashSet<int>();
        static int _expected = -1;
        static float _nextCheck;

        internal static bool IsBoltWeapon(int itemId)
        {
            if (itemId < 0) return false;
            if (_expected < 0 || (_ids.Count < _expected && Time.unscaledTime >= _nextCheck)) Resolve();
            return _ids.Contains(itemId);
        }

        static void Resolve()
        {
            _nextCheck = Time.unscaledTime + 5f;
            var found = new HashSet<int>();
            int expected = 0;
            try
            {
                var mods = ModLoader.loadedMods;
                foreach (var raw in Settings.WeaponModGuids.Value.Split(','))
                {
                    var guid = raw.Trim();
                    if (guid.Length == 0) continue;
                    expected++;
                    if (mods == null || !mods.ContainsKey(guid)) continue;
                    var info = mods[guid];
                    if (info != null && info.resolvedRuntimeId > 0) found.Add(info.resolvedRuntimeId);
                }
            }
            catch (Exception e) { PLog.Debug("Mod loader not ready: " + e.Message); }

            foreach (var raw in Settings.ExtraWeaponIds.Value.Split(','))
                if (int.TryParse(raw.Trim(), out var id)) { found.Add(id); expected++; }

            if (!found.SetEquals(_ids))
            {
                _ids.Clear();
                _ids.UnionWith(found);
                PLog.Info($"Bolt weapons: {(_ids.Count == 0 ? "none found yet" : string.Join(", ", _ids))} ({_ids.Count}/{expected})");
            }
            _expected = expected;
        }

        internal static int ItemIdOf(BasicBullet b)
        {
            try
            {
                var data = b.fireWeapon?.weaponData;
                if (data != null) return data.itemId;
            }
            catch { }
            // Another player's shot: the game rebuilds it from a network message that names the weapon.
            try { if (Remote.WeaponId >= 0 && b.netVisualOnly) return Remote.WeaponId; } catch { }
            return -1;
        }
    }
}
