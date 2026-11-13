using System;
using UnityEngine;
using ZedZone.Net;

namespace Boltgun
{
    // Other players' shots. The shooter's game sends each shot as a message; ours rebuilds it as a
    // visual-only round (NetProjectileSync.SpawnVisualBullet) that does no damage. Those rounds have no
    // weapon attached, so the message's weapon id is kept here while the round is spawned, and the
    // round is then tracked like our own: rocket boost, trail, bolt sprite, ignition and the blast you see.
    internal static class Remote
    {
        internal static int WeaponId = -1;

        internal static void Before(MsgProjectileFire msg)
        {
            WeaponId = -1;
            try { if (msg != null) WeaponId = msg.weaponItemId; } catch { }
        }

        internal static void After(Transform parent)
        {
            int id = WeaponId;
            WeaponId = -1;
            if (id < 0 || !Settings.Enabled.Value || !Weapons.IsBoltWeapon(id)) return;
            if (parent == null) { PLog.Debug("Remote bolt: no bullet parent"); return; }
            // The round was just instantiated under the bullet parent, so it is the last child.
            BasicBullet b = null;
            try
            {
                int n = parent.childCount;
                for (int i = n - 1; i >= 0 && i >= n - 4; i--)
                {
                    var cand = parent.GetChild(i).GetComponent<BasicBullet>();
                    if (cand != null && cand.netVisualOnly) { b = cand; break; }
                }
            }
            catch (Exception e) { PLog.Debug("Remote bolt lookup failed: " + e.Message); }
            if (b == null || Flight.Tracked(b)) return;     // already picked up by the Init hook
            WeaponId = id;
            try { Flight.OnFired(b, 0f); }
            finally { WeaponId = -1; }
            if (Settings.VerboseLog.Value) PLog.Debug($"Remote bolt from weapon {id}");
        }
    }
}
