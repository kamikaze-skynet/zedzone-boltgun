using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boltgun
{
    internal enum BoltState { Flying, Lodged, Spent }

    // One bolt in flight.
    internal sealed class Bolt
    {
        internal BasicBullet Bullet;
        internal float FiredAt;
        internal Vector3 Muzzle;
        internal float Jitter = 1f;        // the game's own per-shot speed jitter, kept on top of ours
        internal BoltState State = BoltState.Flying;
        internal Exhaust Exhaust;           // rocket trail, lit when the motor ignites
        internal bool Dud;                  // the fuse failed: kinetic hit only
        internal bool Armed(Vector2 at) => ((Vector2)Muzzle - at).sqrMagnitude >= Settings.ArmingDistance.Value * Settings.ArmingDistance.Value;
    }

    // Two-stage flight: the powder charge launches the bolt, then a rocket motor speeds it up.
    // The game moves a round by transform.right * bulletSpeed * fixedDeltaTime each physics step,
    // so driving bulletSpeed is all it takes.
    internal static class Flight
    {
        static readonly Dictionary<IntPtr, Bolt> _bolts = new Dictionary<IntPtr, Bolt>();
        static readonly List<IntPtr> _drop = new List<IntPtr>();

        internal static int Count => _bolts.Count;
        internal static void Clear()
        {
            foreach (var kv in _bolts) kv.Value.Exhaust?.Release();
            _bolts.Clear();
        }

        internal static Bolt Get(BasicBullet b)
        {
            if (b == null) return null;
            _bolts.TryGetValue(b.Pointer, out var bolt);
            return bolt;
        }

        internal static bool Alive(UnityEngine.Object o)
        {
            try { return o != null && !o.WasCollected && o.Pointer != IntPtr.Zero && o; }
            catch { return false; }
        }

        // BasicBullet.Init runs once per shot, after the weapon has filled in the round.
        internal static void OnFired(BasicBullet b, float speedBeforeInit)
        {
            if (b == null) return;
            // A new round can reuse a destroyed one's address: always start clean.
            if (_bolts.TryGetValue(b.Pointer, out var old)) { old.Exhaust?.Release(); _bolts.Remove(b.Pointer); }
            if (!Settings.Enabled.Value) return;

            int id = Weapons.ItemIdOf(b);
            bool bolt = Weapons.IsBoltWeapon(id);
            if (Settings.VerboseLog.Value && id >= 0)
                PLog.Debug($"Fired: item {id}{(bolt ? " [bolt]" : "")} speed {b.bulletSpeed:0.#} dmg {b.bulletDamage:0.#} range {b.bulletRange:0.#}");
            if (!bolt) return;

            var state = new Bolt { Bullet = b, FiredAt = Time.time };
            try { state.Muzzle = b.startPos; if (state.Muzzle == Vector3.zero) state.Muzzle = b.transform.position; } catch { }
            if (speedBeforeInit > 0.01f) state.Jitter = Mathf.Clamp(b.bulletSpeed / speedBeforeInit, 0.5f, 1.5f);
            if (Settings.DudChance.Value > 0f && UnityEngine.Random.value < Settings.DudChance.Value) state.Dud = true;

            b.bulletSpeed = SpeedAt(0f) * state.Jitter;
            if (Settings.KineticDamage.Value > 0f) b.bulletDamage = Settings.KineticDamage.Value;
            if (Settings.MinRange.Value > 0f && b.bulletRange < Settings.MinRange.Value) b.bulletRange = Settings.MinRange.Value;
            b.penetrateTime = 0;        // bolts stop inside what they hit
            b.m_penetrateCounter = 0;
            _bolts[b.Pointer] = state;
        }

        internal static float SpeedAt(float t)
        {
            float launch = Settings.LaunchSpeed.Value, final = Settings.FinalSpeed.Value;
            float ign = Settings.IgnitionDelay.Value, burn = Mathf.Max(0.001f, Settings.BurnTime.Value);
            if (t <= ign) return launch;
            if (t >= ign + burn) return final;
            return Mathf.Lerp(launch, final, (t - ign) / burn);   // constant thrust: speed climbs linearly
        }

        internal static void Tick()
        {
            if (_bolts.Count == 0) return;
            float now = Time.time;
            _drop.Clear();
            foreach (var kv in _bolts)
            {
                var s = kv.Value;
                if (s.State == BoltState.Spent || !Alive(s.Bullet)) { _drop.Add(kv.Key); continue; }
                if (s.State != BoltState.Flying) { s.Exhaust?.Release(); continue; }
                try
                {
                    float t = now - s.FiredAt;
                    s.Bullet.bulletSpeed = SpeedAt(t) * s.Jitter;
                    if (s.Exhaust == null && t >= Settings.IgnitionDelay.Value) s.Exhaust = Exhaust.Light(s.Bullet) ?? Exhaust.None;
                }
                catch { _drop.Add(kv.Key); }
            }
            foreach (var k in _drop) { _bolts[k].Exhaust?.Release(); _bolts.Remove(k); }
        }

        // Every rendered frame: keep the exhaust on the bolt.
        internal static void FollowTrails()
        {
            if (_bolts.Count == 0) return;
            foreach (var kv in _bolts)
            {
                var s = kv.Value;
                if (s.Exhaust == null || s.State != BoltState.Flying) continue;
                try { if (Alive(s.Bullet)) s.Exhaust.Follow(s.Bullet.transform.position); } catch { }
            }
        }
    }
}
