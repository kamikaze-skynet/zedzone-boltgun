using System;
using UnityEngine;

namespace Boltgun
{
    // The rocket motor's exhaust: a short hot flame plus a thin smoke trail that lingers.
    // The trail objects are not children of the bullet, so they don't vanish when the round is destroyed
    // and can't leak onto a pooled bullet. They follow the bolt until it stops, then fade out on their own.
    internal sealed class Exhaust
    {
        GameObject _flame, _smoke;
        TrailRenderer _flameTr, _smokeTr;
        bool _released;

        internal static readonly Exhaust None = new Exhaust { _released = true };   // trail off or unavailable

        static Material _mat;
        static bool _matMissing;

        internal static Exhaust Light(BasicBullet b)
        {
            if (!Settings.Trail.Value) return null;
            var mat = MaterialFor(b);
            if (mat == null) return null;
            var e = new Exhaust();
            Vector3 p = b.transform.position;
            int layer = 0, order = 0;
            try
            {
                var r = b.GetComponentInChildren<Renderer>();
                if (r != null) { layer = r.sortingLayerID; order = r.sortingOrder - 1; }
            }
            catch { }

            float w = Settings.TrailWidth.Value;
            e._flame = Make("Boltgun.Flame", p, mat, layer, order + 1, 0.05f, w,
                new Color(1f, 0.85f, 0.45f, 1f), new Color(1f, 0.35f, 0.05f, 0f), out e._flameTr);
            e._smoke = Make("Boltgun.Smoke", p, mat, layer, order, Settings.SmokeTime.Value, w * 0.8f,
                new Color(0.75f, 0.72f, 0.68f, 0.45f), new Color(0.45f, 0.45f, 0.45f, 0f), out e._smokeTr);
            return e;
        }

        static GameObject Make(string name, Vector3 p, Material mat, int layer, int order, float time, float width,
                               Color head, Color tail, out TrailRenderer tr)
        {
            var go = new GameObject(name);
            go.transform.position = p;
            tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = mat;
            tr.time = time;
            tr.widthMultiplier = width;
            tr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
            tr.startColor = head;
            tr.endColor = tail;
            tr.minVertexDistance = 0.1f;
            tr.numCapVertices = 2;
            tr.sortingLayerID = layer;
            tr.sortingOrder = order;
            tr.emitting = true;
            return go;
        }

        static Material MaterialFor(BasicBullet b)
        {
            if (Flight.Alive(_mat)) return _mat;
            if (_matMissing) return null;
            try
            {
                var sh = Shader.Find("Sprites/Default");
                if (sh != null) _mat = new Material(sh);
            }
            catch (Exception e) { PLog.Debug("Sprites/Default shader: " + e.Message); }
            if (_mat == null)
            {
                // Fall back to the material the game's own bullet trail uses.
                try { var t = b.trailRenderer; if (t != null) _mat = t.sharedMaterial; } catch { }
            }
            if (_mat == null) { _matMissing = true; PLog.Warn("No material for the rocket trail; trails are off."); return null; }
            UnityEngine.Object.DontDestroyOnLoad(_mat);
            return _mat;
        }

        // Called every frame while the bolt flies.
        internal void Follow(Vector3 p)
        {
            if (_released) return;
            try
            {
                if (Flight.Alive(_flame)) _flame.transform.position = p;
                if (Flight.Alive(_smoke)) _smoke.transform.position = p;
            }
            catch { }
        }

        // The bolt has stopped: let the exhaust fade out, then clean up.
        internal void Release()
        {
            if (_released) return;
            _released = true;
            try
            {
                if (Flight.Alive(_flameTr)) _flameTr.emitting = false;
                if (Flight.Alive(_smokeTr)) _smokeTr.emitting = false;
                if (Flight.Alive(_flame)) UnityEngine.Object.Destroy(_flame, 0.2f);
                if (Flight.Alive(_smoke)) UnityEngine.Object.Destroy(_smoke, Settings.SmokeTime.Value + 0.2f);
            }
            catch { }
        }
    }
}
