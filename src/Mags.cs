using System;
using System.Collections.Generic;
using System.Text;

namespace Boltgun
{
    // Magazine swaps (R) use the game's own code: BasicRangedWeapon.Reload asks
    // GetMagazineInInventory for magazines whose ammo id matches the gun's and that fit its slot
    // (ItemManager.magazineNormDic, filled when each magazine is registered).
    // This file checks that our items line up after the mod loader has given them their runtime ids,
    // repairs the links if they don't, and logs what R finds so a failed swap can be diagnosed.
    internal static class Mags
    {
        const string AmmoGuid = "cfe0eb6a-4ba6-46f6-b7fc-0476d0a6cd2e";
        static readonly string[] MagGuids = { "8c0b9189-f401-48b1-8273-18bf8ed4f4ad", "a9462c6d-1c5f-4cfe-b1d7-87bf808df939" };
        static readonly string[] GunGuids = { "f5fc9e37-40ad-46b3-ad71-6be84c155ae4", "bac356ff-83fd-47f1-a864-1a33e3c7ba5c" };

        internal static int IdOf(string guid)
        {
            try
            {
                var m = ModLoader.loadedMods;
                if (m != null && m.ContainsKey(guid)) { var i = m[guid]; if (i != null) return i.resolvedRuntimeId; }
            }
            catch { }
            return -1;
        }

        internal static int AmmoId() => IdOf(AmmoGuid);

        static int _pistol = -1;
        internal static int PistolId() { if (_pistol <= 0) _pistol = IdOf(GunGuids[1]); return _pistol; }

        internal static IEnumerable<int> GunIds()
        {
            foreach (var g in GunGuids) { int id = IdOf(g); if (id > 0) yield return id; }
        }

        // After ModLoader.LoadAll: make sure the guns, magazines and ammo point at each other.
        internal static void Check()
        {
            var im = ItemManager.instance;
            if (im == null) return;
            int ammo = IdOf(AmmoGuid);
            if (ammo <= 0) { PLog.Warn("Magazine check: .75 Bolt isn't loaded."); return; }
            var sb = new StringBuilder($"Magazine check: ammo {ammo}");
            int fixedLinks = 0;

            foreach (var g in GunGuids)
            {
                int id = IdOf(g);
                var gun = id > 0 ? im.GetItemAttrById(id)?.TryCast<ItemAttr_RangedWeapon>() : null;
                if (gun == null) continue;
                sb.Append($" | gun {id} ammo {gun.ammoId} slot {(int)gun.weaponSlotSize} feed {(int)gun.feedingType} defaultMag {gun.defaultMagazineId}");
                if (gun.ammoId != ammo) { gun.ammoId = ammo; fixedLinks++; }
            }

            var dic = im.magazineNormDic;
            foreach (var g in MagGuids)
            {
                int id = IdOf(g);
                var mag = id > 0 ? im.GetItemAttrById(id)?.TryCast<ItemAttr_Magazine>() : null;
                if (mag == null) continue;
                sb.Append($" | mag {id} ammo {mag.ammoId} fit {(int)mag.magazineFit} size {mag.magazineSize}");
                if (mag.ammoId != ammo) { mag.ammoId = ammo; fixedLinks++; }
                if (dic != null)
                {
                    if (!dic.ContainsKey(ammo)) dic[ammo] = new Il2CppSystem.Collections.Generic.List<ItemAttr_Magazine>();
                    var list = dic[ammo];
                    bool there = false;
                    for (int i = 0; i < list.Count; i++) if (list[i] != null && list[i].Pointer == mag.Pointer) { there = true; break; }
                    if (!there) { list.Add(mag); fixedLinks++; sb.Append(" (indexed)"); }
                }
            }
            PLog.Info(sb.ToString() + (fixedLinks > 0 ? $" | repaired {fixedLinks} link(s)" : " | all linked"));
        }

        // R pressed on a bolt weapon: log what the game's magazine search finds.
        internal static void OnReload(BasicRangedWeapon w)
        {
            if (w == null) return;
            var data = w.weaponData;
            if (data == null || !Weapons.IsBoltWeapon(data.itemId)) return;
            var sb = new StringBuilder($"Reload: gun {data.itemId}, loaded {BasicRangedWeapon.GetLoadedAmmo(data)}");
            try
            {
                var mags = BasicRangedWeapon.GetMagazineInInventory(w.character, data);
                if (mags == null || mags.Count == 0) sb.Append(", no fitting magazine in inventory");
                else
                {
                    sb.Append($", {mags.Count} magazine(s):");
                    for (int i = 0; i < mags.Count; i++) { var m = mags[i]; if (m != null) sb.Append($" {m.itemId}={ItemData.GetMagazineAmmoNumber(m)}"); }
                }
                sb.Append($", loose bolts {BasicRangedWeapon.GetInventoryAmmoTotal(w.character, data)}");
            }
            catch (Exception e) { sb.Append(" (lookup failed: " + e.Message + ")"); }
            PLog.Info(sb.ToString());
        }
    }
}
