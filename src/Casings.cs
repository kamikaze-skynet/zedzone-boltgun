using System;
using System.Collections.Generic;
using System.Text;

namespace Boltgun
{
    // Spent .75 casings are a real item: the guns eject one per shot (the game's own shell ejection,
    // weapon shellId), they can be picked up, and new bolts are crafted in them. Iron presses into
    // casings, 20 to one. After the mod loader runs this points the guns and recipes at the casing's
    // runtime id and finds the game's Iron Plate item by name for the casing recipe.
    internal static class Casings
    {
        const string CasingGuid = "8e0620cc-b474-4613-82ec-49622b502ecf";
        const int RequestedCasingId = 751080;
        const int IronPlate = 8;   // the game's Iron Plate, already in the casing recipe
        internal static int CasingId { get; private set; } = -1;

        internal static void Check()
        {
            var im = ItemManager.instance;
            if (im == null) return;
            int casing = Mags.IdOf(CasingGuid);
            CasingId = casing;
            if (casing <= 0) { PLog.Warn("Casings: the .75 Bolt Casing item isn't loaded."); return; }
            var sb = new StringBuilder($"Casings: casing {casing}");

            foreach (int gunId in Mags.GunIds())
            {
                var gun = im.GetItemAttrById(gunId)?.TryCast<ItemAttr_RangedWeapon>();
                if (gun == null) continue;
                if (gun.shellId != casing) { sb.Append($" | gun {gunId} shell {gun.shellId}->{casing}"); gun.shellId = casing; }
            }

            // The round's recipe needs casings; follow the casing if its id was remapped.
            var round = im.GetItemAttrById(Mags.AmmoId());
            if (round != null) Remap(round, RequestedCasingId, casing, sb, "round");

            // The casing's recipe: 5 Iron Plates -> 20 casings.
            int iron = Settings.IronItemId.Value > 0 ? Settings.IronItemId.Value
                      : im.GetItemAttrById(IronPlate) != null ? IronPlate : FindIron(im);
            var cAttr = im.GetItemAttrById(casing);
            if (iron > 0 && cAttr != null)
            {
                Remap(cAttr, IronPlate, iron, sb, "casing recipe iron");
                sb.Append($" | iron = {iron} ({Name(im.GetItemAttrById(iron))})");
            }
            else sb.Append(" | iron item not found: set General / IronItemId in the config (VerboseLog lists candidates)");
            PLog.Info(sb.ToString());
        }

        static void Remap(ItemAttr a, int from, int to, StringBuilder sb, string what)
        {
            var recipes = a.recipeDataAry;
            if (recipes == null) return;
            for (int r = 0; r < recipes.Length; r++)
            {
                var items = recipes[r]?.recipeItems;
                if (items == null) continue;
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    if (it == null || it.itemId != from || from == to) continue;
                    it.itemId = to;
                    sb.Append($" | {what} {from}->{to}");
                }
            }
        }

        static string Name(ItemAttr a)
        {
            if (a == null) return "";
            try { var n = a.itemName; if (!string.IsNullOrEmpty(n)) return n; } catch { }
            try { return a.ItemName ?? ""; } catch { return ""; }
        }

        // The game's iron: the item named "Iron Plate", else one named exactly "Iron", else the shortest
        // name that starts with "Iron", preferring crafting materials.
        static int FindIron(ItemManager im)
        {
            var list = im.itemList;
            if (list == null) return -1;
            var mats = new HashSet<int>();
            try { var m = im.materialList; if (m != null) for (int i = 0; i < m.Count; i++) if (m[i] != null) mats.Add(m[i].itemId); } catch { }
            int best = -1, bestScore = int.MaxValue;
            var seen = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a == null) continue;
                string n = Name(a).Trim();
                string l = n.ToLowerInvariant();
                if (!l.Contains("iron")) continue;
                if (Settings.VerboseLog.Value) seen.Add($"{a.itemId}={n}");
                int score;
                if (l == "iron plate") score = -1;
                else if (l == "iron") score = 0;
                else if (l.StartsWith("iron ") && (l.Contains("ingot") || l.Contains("bar") || l.Contains("plate"))) score = 10 + l.Length;
                else if (l.StartsWith("iron")) score = 100 + l.Length;
                else continue;
                if (!mats.Contains(a.itemId)) score += 50;
                if (score < bestScore) { bestScore = score; best = a.itemId; }
            }
            if (seen.Count > 0) PLog.Debug("Items with 'iron' in the name: " + string.Join(", ", seen));
            return best;
        }
    }
}
