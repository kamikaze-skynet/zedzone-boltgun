using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boltgun
{
    // One round in flight.
    internal sealed class Bolt
    {
        internal BasicBullet Bullet;
        internal float FiredAt;
        internal float SpreadFactor = 1f;   // the game's own random speed jitter, kept on top of our speeds
        internal bool HitCharacter;
        internal bool Detonated;
        internal int ItemId;
    }

    // A payload waiting for its fuse.
    internal sealed class PendingDetonation
    {
        internal float At;
        internal BasicCharacterController Target;
        internal CharacterBodyPart Part;
        internal Vector3 Point;
        internal Vector2 Direction;
        internal float Force;
        internal BasicCharacterController Shooter;
        internal bool PlayerSide;
        internal bool VisualOnly;
        internal bool Internal;
    }

    internal static class Bolts
    {
        internal static readonly Dictionary<IntPtr, Bolt> Live = new Dictionary<IntPtr, Bolt>();
        internal static readonly List<PendingDetonation> Pending = new List<PendingDetonation>();

        static HashSet<int> _weaponIds;
        static float _nextIdRefresh;
        static GameObject _blastPrefab;
        static bool _blastPrefabMissing;

        internal static bool Alive(UnityEngine.Object o)
        {
            try { return o != null && !o.WasCollected && o.Pointer != IntPtr.Zero && o; }
            catch { return false; }
        }

        // ---- which weapons fire mass-reactive rounds

        static HashSet<int> WeaponIds()
        {
            // The mod loader assigns ids at load time, so resolve them lazily and refresh
            // now and then until both default weapons are found.
            if (_weaponIds != null && Time.unscaledTime < _nextIdRefresh) return _weaponIds;
            var ids = new HashSet<int>();
            int expected = 0;
            try
            {
                var mods = ModLoader.loadedMods;
                foreach (var raw in Plugin.WeaponModGuids.Value.Split(','))
                {
                    var guid = raw.Trim();
                    if (guid.Length == 0) continue;
                    expected++;
                    if (mods != null && mods.ContainsKey(guid))
                    {
                        var info = mods[guid];
                        if (info != null && info.resolvedRuntimeId > 0) ids.Add(info.resolvedRuntimeId);
                    }
                }
            }
            catch (Exception e) { PLog.Debug("Couldn't read the mod loader's item ids: " + e.Message); }
            foreach (var raw in Plugin.ExtraWeaponIds.Value.Split(','))
                if (int.TryParse(raw.Trim(), out var id)) ids.Add(id);

            bool changed = _weaponIds == null || !_weaponIds.SetEquals(ids);
            _weaponIds = ids;
            // Keep looking every few seconds until every configured guid has resolved.
            _nextIdRefresh = Time.unscaledTime + (ids.Count >= expected ? 60f : 5f);
            if (changed) PLog.Info("Mass-reactive weapon ids: " + (ids.Count == 0 ? "(none yet)" : string.Join(", ", ids)));
            return ids;
        }

        static int WeaponItemId(BasicBullet b)
        {
            try
            {
                var w = b.fireWeapon;
                if (w == null) return -1;
                var data = w.weaponData;
                return data == null ? -1 : data.itemId;
            }
            catch { return -1; }
        }

        internal static Bolt Get(BasicBullet b)
        {
            if (b == null) return null;
            Live.TryGetValue(b.Pointer, out var bolt);
            return bolt;
        }

        // ---- firing

        internal static void OnInit(BasicBullet b, float speedBeforeInit)
        {
            if (!Plugin.Enabled.Value || b == null) return;
            int id = WeaponItemId(b);
            if (id < 0) return;
            bool ours = WeaponIds().Contains(id);
            if (Plugin.VerboseLog.Value) PLog.Debug($"Round fired by item {id}{(ours ? " (mass-reactive)" : "")}, speed {b.bulletSpeed:0.#}, damage {b.bulletDamage:0.#}, range {b.bulletRange:0.#}");
            if (!ours) return;

            var bolt = new Bolt { Bullet = b, FiredAt = Time.time, ItemId = id };
            if (speedBeforeInit > 0.01f) bolt.SpreadFactor = Mathf.Clamp(b.bulletSpeed / speedBeforeInit, 0.5f, 1.5f);

            b.bulletSpeed = Plugin.LaunchSpeed.Value * bolt.SpreadFactor;
            if (Plugin.KineticDamage.Value > 0f) b.bulletDamage = Plugin.KineticDamage.Value;
            if (Plugin.MinRange.Value > 0f && b.bulletRange < Plugin.MinRange.Value) b.bulletRange = Plugin.MinRange.Value;
            // The round stops inside whatever it hits: no over-penetration clones.
            b.penetrateTime = 0;
            b.m_penetrateCounter = 0;

            Live[b.Pointer] = bolt;
        }

        // ---- flight: propellant launch, then the rocket motor

        internal static float SpeedAt(float t)
        {
            float launch = Plugin.LaunchSpeed.Value, final = Plugin.FinalSpeed.Value;
            float ign = Plugin.IgnitionDelay.Value, burn = Mathf.Max(0.001f, Plugin.BurnTime.Value);
            if (t <= ign) return launch;
            if (t >= ign + burn) return final;
            return Mathf.Lerp(launch, final, (t - ign) / burn);   // constant thrust: speed rises linearly
        }

        static readonly List<IntPtr> _dead = new List<IntPtr>();

        internal static void TickFlight()
        {
            if (Live.Count == 0) return;
            float now = Time.time;
            _dead.Clear();
            foreach (var kv in Live)
            {
                var bolt = kv.Value;
                if (bolt.Detonated || !Alive(bolt.Bullet)) { _dead.Add(kv.Key); continue; }
                try { bolt.Bullet.bulletSpeed = SpeedAt(now - bolt.FiredAt) * bolt.SpreadFactor; }
                catch { _dead.Add(kv.Key); }
            }
            foreach (var k in _dead) Live.Remove(k);
        }

        // ---- impact

        internal static void BeforeHitCharacter(BasicBullet b)
        {
            var bolt = Get(b);
            if (bolt == null) return;
            // The game passes the round's penetrate count to the target as its armour-piercing level.
            b.penetrateTime = Plugin.ArmorPenetration.Value;
            b.m_penetrateCounter = 0;
        }

        internal static void AfterHitCharacter(BasicBullet b, RaycastHit2D hit, bool hitResult)
        {
            var bolt = Get(b);
            if (bolt == null) return;
            // Make sure the round doesn't carry on through the target.
            b.penetrateTime = 0;
            b.m_penetrateCounter = 1;
            if (!hitResult || bolt.HitCharacter) return;

            BasicCharacterController target = null;
            CharacterBodyPart part = null;
            try
            {
                var col = hit.collider;
                if (col != null)
                {
                    target = BasicCharacterController.GetCharacterFromCollider(col);
                    part = col.GetComponent<CharacterBodyPart>();
                    if (part == null) part = col.GetComponentInParent<CharacterBodyPart>();
                }
            }
            catch (Exception e) { PLog.Debug("Couldn't read the hit target: " + e.Message); }
            if (target == null) return;

            bolt.HitCharacter = true;
            Queue(b, bolt, hit, target, part, Plugin.FuseDelay.Value, true);
            PLog.Debug($"Impact on {SafeName(target)} ({(part != null ? part.name : "no part")}), payload in {Plugin.FuseDelay.Value:0.###}s");
        }

        internal static void AfterOnHit(BasicBullet b, RaycastHit2D hit, bool result)
        {
            var bolt = Get(b);
            if (bolt == null || bolt.Detonated) return;
            if (bolt.HitCharacter) { bolt.Detonated = true; return; }   // payload already queued
            if (!result || !Plugin.DetonateOnSurfaces.Value) return;
            Queue(b, bolt, hit, null, null, 0f, false);
            PLog.Debug("Surface impact, payload detonates on contact");
        }

        static void Queue(BasicBullet b, Bolt bolt, RaycastHit2D hit, BasicCharacterController target, CharacterBodyPart part, float delay, bool inside)
        {
            bolt.Detonated = inside ? bolt.Detonated : true;
            Vector3 point;
            Vector2 dir = Vector2.right;
            BasicCharacterController shooter = null;
            bool playerSide = false, visualOnly = false;
            float force = 0f;
            try
            {
                float z = b.transform.position.z;
                Vector2 p = hit.point;
                point = new Vector3(p.x, p.y, z);
                Vector3 r = b.transform.right;
                dir = new Vector2(r.x, r.y);
                shooter = b.fireCharacter;
                playerSide = b.playerSideDeviceSource;
                visualOnly = b.netVisualOnly;
                force = b.bulletForce;
            }
            catch (Exception e) { PLog.Debug("Couldn't read the round: " + e.Message); return; }

            Pending.Add(new PendingDetonation
            {
                At = Time.time + delay,
                Target = target,
                Part = part,
                Point = point,
                Direction = dir,
                Force = force,
                Shooter = shooter,
                PlayerSide = playerSide,
                VisualOnly = visualOnly,
                Internal = inside,
            });
        }

        // ---- detonation

        internal static void TickDetonations()
        {
            if (Pending.Count == 0) return;
            float now = Time.time;
            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                var d = Pending[i];
                if (now < d.At) continue;
                Pending.RemoveAt(i);
                try { Detonate(d); }
                catch (Exception e) { PLog.Error("Detonation failed", e); }
            }
        }

        static void Detonate(PendingDetonation d)
        {
            // 1. The payload goes off inside the body part the round is lodged in.
            if (d.Internal && !d.VisualOnly && Plugin.InternalDamage.Value > 0f && Alive(d.Target) && !d.Target.deadFlag)
            {
                bool shooterAlive = Alive(d.Shooter);
                if (shooterAlive && d.Shooter.Pointer != d.Target.Pointer && BasicCharacterController.IsSameSideForDamage(d.Shooter, d.Target))
                {
                    PLog.Debug($"Payload: {SafeName(d.Target)} is on the shooter's side, no internal damage");
                }
                else
                {
                    var part = Alive(d.Part) ? d.Part : null;
                    float damage = Plugin.InternalDamage.Value;
                    // Same scaling the game applies to the round's own hit (difficulty for enemy shooters).
                    if (shooterAlive) damage *= BasicCharacterController.GetEnemyDamageFloat(d.Shooter);
                    Vector2 force = d.Direction * (d.Force * 0.5f);
                    d.Target.OnDirectedHit(damage, new Vector2(d.Point.x, d.Point.y), force, part,
                        1f, true, true, true, true, Plugin.ArmorPenetration.Value);
                    PLog.Debug($"Payload: {damage:0} internal damage to {SafeName(d.Target)}");
                }
            }

            // 2. A small blast where it went off.
            if (Plugin.BlastDamage.Value > 0f) SpawnBlast(d);
        }

        static void SpawnBlast(PendingDetonation d)
        {
            var prefab = BlastPrefab();
            if (prefab == null) return;
            var go = UnityEngine.Object.Instantiate(prefab, d.Point, Quaternion.identity);
            if (go == null) return;
            go.transform.localScale = go.transform.localScale * Plugin.BlastVisualScale.Value;
            var ex = go.GetComponent<BasicExplosion>();
            if (ex == null)
            {
                PLog.Warn("The blast resource has no explosion component; showing the effect only.");
                return;
            }
            if (Alive(d.Shooter))
            {
                ex.instigator = d.Shooter;
                ex.scoreAttacker = d.Shooter;
            }
            ex.playerSideDeviceSource = d.PlayerSide;
            ex.visualOnly = d.VisualOnly;
            ex.ConfigurePayload(Plugin.BlastDamage.Value, Plugin.BlastRadius.Value);
            PLog.Debug($"Blast {Plugin.BlastDamage.Value:0}/{Plugin.BlastRadius.Value:0.##}m at {d.Point}{(d.VisualOnly ? " (visual only)" : "")}");
        }

        static GameObject BlastPrefab()
        {
            if (Alive(_blastPrefab)) return _blastPrefab;
            if (_blastPrefabMissing) return null;
            try { _blastPrefab = Resources.Load<GameObject>(Plugin.BlastPrefab.Value); }
            catch (Exception e) { PLog.Error("Loading the blast resource failed", e); }
            if (_blastPrefab == null)
            {
                _blastPrefabMissing = true;
                PLog.Warn($"Blast resource '{Plugin.BlastPrefab.Value}' not found; rounds will do internal damage only.");
            }
            return _blastPrefab;
        }

        static string SafeName(UnityEngine.Object o)
        {
            try { return Alive(o) ? o.name : "?"; } catch { return "?"; }
        }
    }

    // Hidden object that runs the rocket motors and the fuses.
    public class BoltHost : MonoBehaviour
    {
        public BoltHost(IntPtr ptr) : base(ptr) { }

        void FixedUpdate()
        {
            try { Bolts.TickFlight(); }
            catch (Exception e) { PLog.Error("Flight update failed", e); Bolts.Live.Clear(); }
        }

        void Update()
        {
            try { Bolts.TickDetonations(); }
            catch (Exception e) { PLog.Error("Fuse update failed", e); Bolts.Pending.Clear(); }
        }
    }
}
