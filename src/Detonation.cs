using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boltgun
{
    // The mass-reactive charge: damage inside the struck body part after a short fuse, then a small blast.
    internal static class Detonation
    {
        sealed class Charge
        {
            internal float At;
            internal BasicCharacterController Target;   // null = surface hit, blast only
            internal CharacterBodyPart Part;
            internal Vector3 Point;
            internal Vector2 Direction;
            internal float Force;
            internal BasicCharacterController Shooter;
            internal bool PlayerSide, VisualOnly;
        }

        static readonly List<Charge> _pending = new List<Charge>();
        static GameObject _blast;
        static bool _blastMissing;

        internal static void Clear() => _pending.Clear();

        internal static void Queue(BasicBullet b, Vector2 point, BasicCharacterController target, CharacterBodyPart part, float delay)
        {
            var c = new Charge { At = Time.time + delay, Target = target, Part = part };
            try
            {
                var t = b.transform;
                c.Point = new Vector3(point.x, point.y, t.position.z);
                Vector3 r = t.right; c.Direction = new Vector2(r.x, r.y);
                c.Force = b.bulletForce;
                c.Shooter = b.fireCharacter;
                c.PlayerSide = b.playerSideDeviceSource;
                c.VisualOnly = b.netVisualOnly;   // someone else's bolt: they apply the damage
            }
            catch (Exception e) { PLog.Debug("Couldn't read the bolt: " + e.Message); return; }
            _pending.Add(c);
            PLog.Debug(target != null ? $"Lodged in {Name(target)}, charge in {delay:0.###}s" : "Surface hit, charge goes off on contact");
        }

        internal static void Tick()
        {
            if (_pending.Count == 0) return;
            float now = Time.time;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var c = _pending[i];
                if (now < c.At) continue;
                _pending.RemoveAt(i);
                try { Inside(c); } catch (Exception e) { PLog.Error("Internal detonation failed", e); }
                try { Shrapnel(c); } catch (Exception e) { PLog.Error("Shrapnel failed", e); }
                try { Blast(c); } catch (Exception e) { PLog.Error("Blast failed", e); }
            }
        }

        static void Inside(Charge c)
        {
            if (c.Target == null || c.VisualOnly || Settings.InternalDamage.Value <= 0f) return;
            if (!Flight.Alive(c.Target) || c.Target.deadFlag) return;
            bool shooterAlive = Flight.Alive(c.Shooter);
            if (shooterAlive && c.Shooter.Pointer != c.Target.Pointer && BasicCharacterController.IsSameSideForDamage(c.Shooter, c.Target))
            { PLog.Debug($"{Name(c.Target)} is on the shooter's side: no internal damage"); return; }

            float damage = Settings.InternalDamage.Value;
            if (shooterAlive) damage *= BasicCharacterController.GetEnemyDamageFloat(c.Shooter);   // same scaling as the round's own hit
            var part = Flight.Alive(c.Part) ? c.Part : null;
            var at = new Vector2(c.Point.x, c.Point.y);
            c.Target.OnDirectedHit(damage, at, c.Direction * (c.Force * Settings.InternalForce.Value), part,
                1f, true, true, true, true, Settings.ArmorPenetration.Value);
            PLog.Debug($"Charge: {damage:0} internal damage to {Name(c.Target)}");
            Gore(c, damage, at);
            Wound(c);
        }

        // Survivors of the charge are thrown off their feet.
        static void Wound(Charge c)
        {
            if (!Settings.Knockdown.Value || !Flight.Alive(c.Target) || c.Target.deadFlag) return;
            try
            {
                if (c.Target.CanEnterKnockdown() && c.Target.TryEnterKnockdown(BasicCharacterController.KnockdownCause.Impact, Settings.KnockdownTime.Value))
                    PLog.Debug($"{Name(c.Target)} survived and was knocked down");
            }
            catch (Exception e) { PLog.Debug("Knockdown failed: " + e.Message); }
        }

        static readonly HashSet<IntPtr> _hitOnce = new HashSet<IntPtr>();

        // Fragments of the bolt's casing fly out from the detonation and cut anyone close by.
        static void Shrapnel(Charge c)
        {
            if (Settings.ShrapnelSparks.Value > 0) Fx.Shrapnel(c.Point, Settings.ShrapnelSparks.Value);
            float dmg = Settings.ShrapnelDamage.Value, radius = Settings.ShrapnelRadius.Value;
            if (c.VisualOnly || dmg <= 0f) return;
            var at = new Vector2(c.Point.x, c.Point.y);
            var cols = Physics2D.OverlapCircleAll(at, radius);
            if (cols == null) return;
            bool shooterAlive = Flight.Alive(c.Shooter);
            _hitOnce.Clear();
            if (Flight.Alive(c.Target)) _hitOnce.Add(c.Target.Pointer);       // the charge already did its work there
            int n = 0;
            foreach (var col in cols)
            {
                if (col == null) continue;
                BasicCharacterController ch = null;
                try { ch = BasicCharacterController.GetCharacterFromCollider(col); } catch { }
                if (ch == null || !_hitOnce.Add(ch.Pointer) || ch.deadFlag) continue;
                if (shooterAlive && (ch.Pointer == c.Shooter.Pointer || BasicCharacterController.IsSameSideForDamage(c.Shooter, ch))) continue;
                Vector2 to = (Vector2)ch.transform.position - at;
                float d = to.magnitude;
                float fall = Mathf.Clamp01(1f - d / radius);
                if (fall <= 0f) continue;
                float hit = dmg * fall;
                if (shooterAlive) hit *= BasicCharacterController.GetEnemyDamageFloat(c.Shooter);
                var part = col.GetComponent<CharacterBodyPart>();
                Vector2 dir = d > 0.01f ? to / d : c.Direction;
                ch.OnDirectedHit(hit, (Vector2)col.bounds.ClosestPoint(c.Point), dir * (c.Force * 0.3f), part, 1f, true, true, true, true, 2);
                n++;
            }
            if (n > 0) PLog.Debug($"Shrapnel hit {n} more");
        }

        // Zombies have real dismemberment in the game: hand it the charge so limbs come off.
        static void Gore(Charge c, float damage, Vector2 at)
        {
            if (!Settings.Gore.Value || !Flight.Alive(c.Target)) return;
            ZombieController z = null;
            try { z = c.Target.TryCast<ZombieController>(); } catch { }
            if (z == null) return;
            try
            {
                z.OnExplosionDismember(damage, at, c.Direction * Settings.GoreForce.Value);
                PLog.Debug($"Gore: blew {Name(c.Target)} apart");
            }
            catch (Exception e) { PLog.Debug("Dismemberment failed: " + e.Message); }
        }

        static void Blast(Charge c)
        {
            if (Settings.BlastDamage.Value <= 0f) return;
            var prefab = BlastPrefab();
            if (prefab == null) return;
            var go = UnityEngine.Object.Instantiate(prefab, c.Point, Quaternion.identity);
            if (go == null) return;
            go.transform.localScale *= Settings.BlastVisualScale.Value;
            var ex = go.GetComponent<BasicExplosion>();
            if (ex == null) { PLog.Warn("Blast resource has no BasicExplosion; effect only."); return; }
            if (Flight.Alive(c.Shooter)) { ex.instigator = c.Shooter; ex.scoreAttacker = c.Shooter; }
            ex.playerSideDeviceSource = c.PlayerSide;
            ex.visualOnly = c.VisualOnly;
            ex.ConfigurePayload(Settings.BlastDamage.Value, Settings.BlastRadius.Value);
        }

        static GameObject BlastPrefab()
        {
            if (Flight.Alive(_blast)) return _blast;
            if (_blastMissing) return null;
            try { _blast = Resources.Load<GameObject>(Settings.BlastPrefab.Value); }
            catch (Exception e) { PLog.Error("Loading the blast resource failed", e); }
            if (_blast == null) { _blastMissing = true; PLog.Warn($"Blast resource '{Settings.BlastPrefab.Value}' not found: internal damage only."); }
            return _blast;
        }

        static string Name(UnityEngine.Object o)
        {
            try { return Flight.Alive(o) ? o.name : "?"; } catch { return "?"; }
        }
    }
}
