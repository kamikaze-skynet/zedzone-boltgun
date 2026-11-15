using System;
using UnityEngine;

namespace Boltgun
{
    // Laser sights on the bolt guns. The game builds the beam (a LaserPointerEffect) when a laser part
    // is fitted, then draws it every LateUpdate. On the bolt guns it attaches but the beam stays dark,
    // so after the game's own LateUpdate this logs what the beam is doing and, if it is still hidden,
    // casts the beam itself and shows it.
    internal static class Laser
    {
        static float _nextLog;
        static int _logs;

        internal static void After(LaserPointerEffect fx)
        {
            if (fx == null) return;
            var w = fx.rangedWeapon;
            if (w == null) return;
            var data = w.weaponData;
            if (data == null || !Weapons.IsBoltWeapon(data.itemId)) return;

            var beam = fx.laserSpriteRenderer;
            var dot = fx.startPointSpriteRenderer;
            var end = fx.endPointSpriteRenderer;
            bool hidden = beam == null || !beam.enabled || !beam.gameObject.activeInHierarchy || !(beam.color.a > 0.02f);

            if (Time.unscaledTime >= _nextLog && _logs < 12)
            {
                _nextLog = Time.unscaledTime + 2f;
                _logs++;
                PLog.Info($"Laser: gun {data.itemId} hidden={hidden} beam={(beam == null ? "none" : $"on={beam.enabled} act={beam.gameObject.activeInHierarchy} a={beam.color.a:0.00} size={beam.transform.lossyScale}")} " +
                          $"start={(dot == null ? "none" : dot.enabled.ToString())} end={(end == null ? "none" : end.gameObject.activeInHierarchy.ToString())} " +
                          $"range={fx.laserRange:0.0} intensity={fx.laserIntensity:0.00} pos={fx.transform.position} char={(w.character != null)}");
            }

            if (!hidden || !Settings.LaserFix.Value) return;
            if (w.character == null || !w.gameObject.activeInHierarchy || beam == null) return;

            Vector3 start = fx.transform.position;
            Vector2 dir = fx.transform.right * (fx.transform.lossyScale.x < 0f ? -1f : 1f);
            float range = fx.laserRange > 0.5f ? fx.laserRange : 30f;
            Vector3 stop = start + (Vector3)(dir.normalized * range);
            try
            {
                var shooter = w.character.transform.root;
                var hits = Physics2D.RaycastAll(start, dir, range);
                float best = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    var c = hits[i].collider;
                    if (c == null || c.isTrigger) continue;
                    if (c.transform.root == shooter) continue;
                    if (hits[i].distance < best) { best = hits[i].distance; stop = hits[i].point; }
                }
            }
            catch { }
            stop.z = start.z;

            beam.gameObject.SetActive(true);
            beam.enabled = true;
            if (dot != null) dot.enabled = true;
            if (end != null) end.gameObject.SetActive(true);
            fx.UpdateLaserPoint(start, stop);
            float a = Mathf.Clamp01(fx.laserIntensity > 0.05f ? fx.laserIntensity : 1f);
            Visible(beam, a); Visible(dot, a); Visible(end, a);
        }

        static void Visible(SpriteRenderer r, float a)
        {
            if (r == null) return;
            var c = r.color;
            if (!(c.a > 0.02f)) { c.a = a; r.color = c; }
        }
    }
}
