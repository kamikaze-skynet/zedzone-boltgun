using System;
using UnityEngine;

namespace Boltgun
{
    // What happens when a bolt hits something. BasicBullet.OnHit calls HitCharacter for creatures;
    // when that returns true the round is finished (BulletPenetrate + DestroyBullet).
    internal static class Impact
    {
        // The game hands BasicBullet.penetrateTime to OnDirectedHit as the armour-piercing level,
        // so raise it just for the hit.
        internal static void BeforeCharacter(BasicBullet b)
        {
            if (Flight.Get(b) == null) return;
            b.penetrateTime = Settings.ArmorPenetration.Value;
            b.m_penetrateCounter = 0;
        }

        internal static void AfterCharacter(BasicBullet b, RaycastHit2D hit, bool hitIt)
        {
            var bolt = Flight.Get(b);
            if (bolt == null) return;
            // Counter >= limit: BulletPenetrate won't spawn a through-and-through copy.
            b.penetrateTime = 0;
            b.m_penetrateCounter = 1;
            if (!hitIt || bolt.State != BoltState.Flying) return;

            BasicCharacterController target = null;
            CharacterBodyPart part = null;
            try
            {
                var col = hit.collider;
                if (col != null)
                {
                    target = BasicCharacterController.GetCharacterFromCollider(col);
                    part = col.GetComponent<CharacterBodyPart>() ?? col.GetComponentInParent<CharacterBodyPart>();
                }
            }
            catch (Exception e) { PLog.Debug("Couldn't read the hit target: " + e.Message); }
            if (target == null) return;

            bolt.State = BoltState.Lodged;
            if (!bolt.Armed(hit.point)) { PLog.Debug("Hit before the fuse armed: kinetic only"); return; }
            Detonation.Queue(b, hit.point, target, part, Settings.FuseDelay.Value);
        }

        internal static void AfterAnyHit(BasicBullet b, RaycastHit2D hit, bool consumed)
        {
            var bolt = Flight.Get(b);
            if (bolt == null) return;
            if (bolt.State == BoltState.Lodged) { bolt.State = BoltState.Spent; return; }
            if (!consumed || bolt.State != BoltState.Flying) return;
            bolt.State = BoltState.Spent;
            if (!Settings.DetonateOnSurfaces.Value || !bolt.Armed(hit.point)) return;
            Detonation.Queue(b, hit.point, null, null, 0f);
        }
    }
}
