using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Boltgun
{
    // Small home-made effects: the bolt itself in flight, the motor's ignition flash and puff,
    // and shrapnel sparks. Sprites are drawn in code at start-up (the game ships without Unity's
    // image loader), so the mod needs no extra files.
    internal static class Fx
    {
        sealed class Particle
        {
            internal GameObject Go;
            internal SpriteRenderer Sr;
            internal Vector3 Vel;
            internal float Born, Life, Gravity, Drag;
            internal Vector3 Scale0, Scale1;
            internal Color Col0, Col1;
        }

        static readonly List<Particle> _live = new List<Particle>();
        static Sprite _bolt, _disc, _puff, _spark;
        static bool _failed;

        // Sorting taken from the last bullet we saw, so our effects draw on the same layer as rounds.
        internal static int SortLayer, SortOrder = 50;

        internal static void TakeSorting(BasicBullet b)
        {
            try
            {
                var r = b.GetComponentInChildren<Renderer>();
                if (r != null) { SortLayer = r.sortingLayerID; SortOrder = r.sortingOrder; }
            }
            catch { }
        }

        static bool Ready()
        {
            if (_failed) return false;
            if (Flight.Alive(_bolt) && Flight.Alive(_disc) && Flight.Alive(_puff) && Flight.Alive(_spark)) return true;
            try
            {
                _bolt = Make(BoltPixels(out int bw, out int bh), bw, bh, new Vector2(0.75f, 0.5f), bw / Settings.BoltLength.Value);
                _disc = Make(Disc(32, 1.6f), 32, 32, new Vector2(0.5f, 0.5f), 32f);
                _puff = Make(Disc(32, 0.8f), 32, 32, new Vector2(0.5f, 0.5f), 32f);
                _spark = Make(SparkPixels(), 8, 2, new Vector2(0.5f, 0.5f), 32f);
                return true;
            }
            catch (Exception e) { _failed = true; PLog.Error("Couldn't make the effect sprites; bolt sprite, flash and sparks are off", e); return false; }
        }

        static Sprite Make(Color32[] px, int w, int h, Vector2 pivot, float ppu)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(new Il2CppStructArray<Color32>(px));
            tex.Apply(false, false);
            UnityEngine.Object.DontDestroyOnLoad(tex);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect, Vector4.zero);
            UnityEngine.Object.DontDestroyOnLoad(s);
            return s;
        }

        // A 24x7 side view of a bolt, pointing right: dark casing, brass band, steel tip, hot motor at the back.
        static Color32[] BoltPixels(out int w, out int h)
        {
            w = 24; h = 7;
            var px = new Color32[w * h];
            var casing = new Color32(58, 52, 46, 255);
            var casingHi = new Color32(96, 88, 76, 255);
            var brass = new Color32(196, 150, 60, 255);
            var tip = new Color32(170, 178, 186, 255);
            var tipHi = new Color32(226, 232, 238, 255);
            var flame = new Color32(255, 196, 90, 255);
            var flameCore = new Color32(255, 246, 210, 255);
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                int dy = Math.Abs(y - 3);
                Color32 c = default;
                if (x <= 2) { if (dy <= 2 - x / 2) c = x == 0 ? flame : flameCore; }                  // motor glow
                else if (x <= 3) { if (dy <= 2) c = casing; }                                         // nozzle
                else if (x <= 14) { if (dy <= 3) c = (y == 5) ? casingHi : casing; }                  // body
                else if (x <= 16) { if (dy <= 3) c = brass; }                                         // driving band
                else { int r = 3 - (x - 17) / 2; if (dy <= r) c = (y >= 4) ? tipHi : tip; }           // ogive tip
                px[y * w + x] = c;
            }
            return px;
        }

        // Soft round blob; power > 1 makes the edge harder.
        static Color32[] Disc(int n, float power)
        {
            var px = new Color32[n * n];
            float c = (n - 1) / 2f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - Mathf.Pow(d, power));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            return px;
        }

        static Color32[] SparkPixels()
        {
            var px = new Color32[16];
            for (int x = 0; x < 8; x++)
            {
                byte a = (byte)(255 * (x + 1) / 8);
                px[x] = new Color32(255, 220, 140, a);
                px[8 + x] = new Color32(255, 240, 200, a);
            }
            return px;
        }

        static SpriteRenderer NewRenderer(string name, Sprite s, Vector3 p, int orderOffset)
        {
            var go = new GameObject(name);
            go.transform.position = p;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingLayerID = SortLayer;
            sr.sortingOrder = SortOrder + orderOffset;
            return sr;
        }

        // ---- the bolt in flight ----

        internal static GameObject BoltBody(BasicBullet b)
        {
            if (!Settings.BoltSprite.Value || !Ready()) return null;
            TakeSorting(b);
            var sr = NewRenderer("Boltgun.Bolt", _bolt, b.transform.position, 2);
            sr.transform.rotation = b.transform.rotation;
            return sr.gameObject;
        }

        // ---- one-off effects ----

        static void Spawn(Sprite s, Vector3 p, float rotZ, Vector3 vel, float life, float s0, float s1, Color c0, Color c1,
                          float gravity = 0f, float drag = 0f, int orderOffset = 1)
        {
            var sr = NewRenderer("Boltgun.Fx", s, p, orderOffset);
            sr.transform.rotation = Quaternion.Euler(0, 0, rotZ);
            sr.transform.localScale = Vector3.one * s0;
            sr.color = c0;
            _live.Add(new Particle
            {
                Go = sr.gameObject, Sr = sr, Vel = vel, Born = Time.time, Life = life, Gravity = gravity, Drag = drag,
                Scale0 = Vector3.one * s0, Scale1 = Vector3.one * s1, Col0 = c0, Col1 = c1,
            });
        }

        // The rocket motor lighting: a hot flash and a puff of smoke that drifts and spreads.
        internal static void Ignition(Vector3 p, Vector2 dir)
        {
            if (!Settings.IgnitionFlash.Value || !Ready()) return;
            float k = Settings.IgnitionFlashSize.Value;
            Spawn(_disc, p, 0, Vector3.zero, 0.07f, 0.25f * k, 0.6f * k, new Color(1f, 0.92f, 0.65f, 1f), new Color(1f, 0.5f, 0.1f, 0f), orderOffset: 3);
            Vector3 back = new Vector3(-dir.x, -dir.y, 0f);
            for (int i = 0; i < 3; i++)
            {
                var v = back * UnityEngine.Random.Range(0.5f, 1.5f) + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.4f) + Vector3.up * 0.3f;
                Spawn(_puff, p + back * (0.1f * i), UnityEngine.Random.Range(0f, 360f), v, UnityEngine.Random.Range(0.5f, 0.8f),
                      0.2f * k, 0.9f * k, new Color(0.8f, 0.78f, 0.74f, 0.55f), new Color(0.6f, 0.6f, 0.6f, 0f), drag: 2f);
            }
        }

        // Fragments thrown out by the internal charge.
        internal static void Shrapnel(Vector3 p, int count)
        {
            if (!Ready()) return;
            for (int i = 0; i < count; i++)
            {
                float a = UnityEngine.Random.Range(0f, 360f);
                float sp = UnityEngine.Random.Range(6f, 16f);
                var v = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0f) * sp;
                Spawn(_spark, p, a, v, UnityEngine.Random.Range(0.12f, 0.3f), 1f, 0.6f,
                      new Color(1f, 0.85f, 0.5f, 1f), new Color(1f, 0.4f, 0.1f, 0f), gravity: 9f, drag: 1.5f, orderOffset: 4);
            }
        }

        internal static void Tick()
        {
            if (_live.Count == 0) return;
            float now = Time.time, dt = Time.deltaTime;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var q = _live[i];
                float t = (now - q.Born) / q.Life;
                if (t >= 1f || !Flight.Alive(q.Go))
                {
                    try { if (Flight.Alive(q.Go)) UnityEngine.Object.Destroy(q.Go); } catch { }
                    _live.RemoveAt(i);
                    continue;
                }
                try
                {
                    q.Vel += Vector3.down * (q.Gravity * dt);
                    q.Vel *= Mathf.Max(0f, 1f - q.Drag * dt);
                    var tr = q.Go.transform;
                    tr.position += q.Vel * dt;
                    tr.localScale = Vector3.Lerp(q.Scale0, q.Scale1, t);
                    q.Sr.color = Color.Lerp(q.Col0, q.Col1, t);
                }
                catch { _live.RemoveAt(i); }
            }
        }

        internal static void Clear()
        {
            foreach (var q in _live) { try { if (Flight.Alive(q.Go)) UnityEngine.Object.Destroy(q.Go); } catch { } }
            _live.Clear();
        }
    }
}
