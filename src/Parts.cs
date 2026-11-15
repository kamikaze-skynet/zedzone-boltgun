using System;
using System.Collections.Generic;
using System.Text;

namespace Boltgun
{
    // Attachments. The game lets a gun part go on a gun only if the part's ammoIds list holds the gun's
    // ammo id ("wrong caliber" otherwise), and only if the gun's slot for that part type is open.
    // No vanilla part knows the .75 Bolt, so after the mod loader runs we add it to every sight
    // (the Aimer slot: scopes, red dots, lasers) and to every laser part, and open those slots on our guns.
    internal static class Parts
    {
        internal static void Check()
        {
            if (!Settings.Attachments.Value) return;
            var im = ItemManager.instance;
            if (im == null) return;
            int ammo = Mags.AmmoId();
            if (ammo <= 0) return;
            var list = im.gunPartList;
            if (list == null) { PLog.Warn("Attachments: the game's gun part list isn't ready."); return; }

            var slots = new HashSet<int> { (int)GunPartType.Aimer };
            var names = new List<string>();
            int added = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p == null) continue;
                bool laser = false;
                try { laser = p.laserEffect; } catch { }
                if (p.gunPartType != GunPartType.Aimer && !laser) continue;
                slots.Add((int)p.gunPartType);
                var ids = p.ammoIds;
                int n = ids == null ? 0 : ids.Length;
                // An empty list fits nothing (unless the part is for air guns), so leave those alone.
                if (n == 0) continue;
                bool has = false;
                for (int k = 0; k < n; k++) if (ids[k] == ammo) { has = true; break; }
                if (has) continue;
                var grown = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<int>(n + 1);
                for (int k = 0; k < n; k++) grown[k] = ids[k];
                grown[n] = ammo;
                p.ammoIds = grown;
                added++;
                if (Settings.VerboseLog.Value) names.Add($"{p.name}{(laser ? " (laser)" : "")}");
            }

            int opened = 0;
            foreach (int gunId in Mags.GunIds())
            {
                var gun = im.GetItemAttrById(gunId)?.TryCast<ItemAttr_RangedWeapon>();
                var pm = gun?.partModifiable;
                if (pm == null) continue;
                foreach (int s in slots)
                    if (s >= 0 && s < pm.Length && !pm[s]) { pm[s] = true; opened++; }
            }
            var sb = new StringBuilder($"Attachments: .75 Bolt added to {added} sight/laser part(s); opened {opened} slot(s) on the bolt guns.");
            if (names.Count > 0) sb.Append(" Parts: " + string.Join(", ", names));
            PLog.Info(sb.ToString());
        }
    }
}
