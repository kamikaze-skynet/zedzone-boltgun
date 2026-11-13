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
        internal GameObject Body;           // our own bolt sprite riding on the round

        // The bolt stopped (hit, out of range or gone): let the exhaust fade and remove the sprite.
        internal void Stop()
        {
            Exhaust?.Release();
            if (Exhaust == null) Exhaust = Exhaust.None;
            try { if (Flight.Alive(Body)) UnityEngine.Object.Destroy(Body); } catch { }
            Body = null;
        }
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
            foreach (var kv in _bolts) kv.Value.Stop();
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
            if (_bolts.TryGetValue(b.Pointer, out var old)) { old.Stop(); _bolts.Remove(b.Pointer); }
            if (!Settings.Enabled.Value) return;

            int id = Weapons.ItemIdOf(b);
            bool bolt = Weapons.IsBoltWeapon(id);
            if (Settings.VerboseLog.Value && id >= 0)
                PLog.Debug($"Fired: item {id}{(bolt ? " [bolt]" : "")} speed {b.bulletSpeed:0.#} dmg {b.bulletDamage:0.#} range {b.bulletRange:0.#}");
            if (!bolt) return;

            var state = new Bolt { Bullet = b, FiredAt = Time.time };
            try { state.Muzzle = b.startPos; if (state.Muzzle == Vector3.zero) state.Muzzle = b.transform.position; } catch { }
            if (speedBeforeInit > 0.01f) state.Jitter = Mathf.Clamp(b.bulletSpeed / speedBeforeInit, 0.5f, 1.5f);
            bool remote = false;
            try { remote = b.netVisualOnly; } catch { }
            // Duds are rolled by the shooter, who also does the damage; other players always see the blast.
            if (!remote && Settings.DudChance.Value > 0f && UnityEngine.Random.value < Settings.DudChance.Value) state.Dud = true;
            if (remote) state.Jitter = 1f;

            b.bulletSpeed = SpeedAt(0f) * state.Jitter;
            if (Settings.KineticDamage.Value > 0f) b.bulletDamage = Settings.KineticDamage.Value;
            if (Settings.MinRange.Value > 0f && b.bulletRange < Settings.MinRange.Value) b.bulletRange = Settings.MinRange.Value;
            b.penetrateTime = 0;        // bolts stop inside what they hit
            b.m_penetrateCounter = 0;
            try { state.Body = Fx.BoltBody(b); } catch (Exception e) { PLog.Debug("Bolt sprite: " + e.Message); }
            _bolts[b.Pointer] = state;
        }

        internal static bool Tracked(BasicBullet b) => b != null && _bolts.ContainsKey(b.Pointer);

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
                if (s.State != BoltState.Flying) { s.Stop(); continue; }
                try
                {
                    float t = now - s.FiredAt;
                    s.Bullet.bulletSpeed = SpeedAt(t) * s.Jitter;
                    if (s.Exhaust == null && t >= Settings.IgnitionDelay.Value)
                    {
                        s.Exhaust = Exhaust.Light(s.Bullet) ?? Exhaust.None;
                        try { Vector3 r = s.Bullet.transform.right; Fx.Ignition(s.Bullet.transform.position, new Vector2(r.x, r.y)); Sound.Ignition(s.Bullet.transform.position); }
                        catch (Exception e) { PLog.Debug("Ignition flash: " + e.Message); }
                    }
                }
                catch { _drop.Add(kv.Key); }
            }
            foreach (var k in _drop) { _bolts[k].Stop(); _bolts.Remove(k); }
        }

        // Every rendered frame: keep the exhaust on the bolt.
        internal static void FollowTrails()
        {
            if (_bolts.Count == 0) return;
            foreach (var kv in _bolts)
            {
                var s = kv.Value;
                if (s.State != BoltState.Flying || !Alive(s.Bullet)) continue;
                try
                {
                    var tr = s.Bullet.transform;
                    Vector3 p = tr.position;
                    s.Exhaust?.Follow(p);
                    if (Alive(s.Body)) { s.Body.transform.position = p; s.Body.transform.rotation = tr.rotation; }
                }
                catch { }
            }
        }
    }
}
