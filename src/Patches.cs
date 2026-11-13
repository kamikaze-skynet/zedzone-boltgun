using System;
using HarmonyLib;
using UnityEngine;

namespace Boltgun
{
    internal static class Patches
    {
        internal static void Apply(Harmony h)
        {
            Patch(h, nameof(BasicBullet.Init), nameof(Init_Pre), nameof(Init_Post));
            Patch(h, nameof(BasicBullet.HitCharacter), nameof(HitCharacter_Pre), nameof(HitCharacter_Post));
            Patch(h, nameof(BasicBullet.OnHit), null, nameof(OnHit_Post));

            try
            {
                var loadAll = AccessTools.Method(typeof(ModLoader), nameof(ModLoader.LoadAll));
                if (loadAll == null) PLog.Warn("ModLoader.LoadAll not found: bundled items can't load.");
                else h.Patch(loadAll, postfix: new HarmonyMethod(typeof(Patches), nameof(LoadAll_Post)));
            }
            catch (Exception e) { PLog.Error("Patching ModLoader.LoadAll failed", e); }

            try
            {
                var spawn = AccessTools.Method(typeof(ZedZone.Net.NetProjectileSync), nameof(ZedZone.Net.NetProjectileSync.SpawnVisualBullet));
                if (spawn == null) PLog.Warn("NetProjectileSync.SpawnVisualBullet not found: other players' bolts look like plain bullets.");
                else h.Patch(spawn, new HarmonyMethod(typeof(Patches), nameof(Spawn_Pre)), new HarmonyMethod(typeof(Patches), nameof(Spawn_Post)));
            }
            catch (Exception e) { PLog.Error("Patching NetProjectileSync.SpawnVisualBullet failed", e); }
        }

        static void LoadAll_Post()
        {
            try { Bundled.LoadMissing(); } catch (Exception e) { PLog.Error("Loading bundled items failed", e); }
        }

        static void Spawn_Pre(ZedZone.Net.MsgProjectileFire msg)
        {
            try { Remote.Before(msg); } catch (Exception e) { PLog.Error("Remote shot hook (pre) failed", e); }
        }

        static void Spawn_Post(Transform parent)
        {
            try { Remote.After(parent); } catch (Exception e) { PLog.Error("Remote shot hook failed", e); }
        }

        static void Patch(Harmony h, string method, string pre, string post)
        {
            try
            {
                var target = AccessTools.Method(typeof(BasicBullet), method);
                if (target == null) { PLog.Error($"BasicBullet.{method} not found; that part of the mod is off."); return; }
                h.Patch(target, pre == null ? null : new HarmonyMethod(typeof(Patches), pre),
                                post == null ? null : new HarmonyMethod(typeof(Patches), post));
            }
            catch (Exception e) { PLog.Error($"Patching BasicBullet.{method} failed", e); }
        }

        static void Init_Pre(BasicBullet __instance, out float __state)
        {
            __state = 0f;
            try { __state = __instance.bulletSpeed; } catch { }
        }

        static void Init_Post(BasicBullet __instance, float __state)
        {
            try { Flight.OnFired(__instance, __state); } catch (Exception e) { PLog.Error("Fire hook failed", e); }
        }

        static void HitCharacter_Pre(BasicBullet __instance)
        {
            try { Impact.BeforeCharacter(__instance); } catch (Exception e) { PLog.Error("Hit hook (pre) failed", e); }
        }

        static void HitCharacter_Post(BasicBullet __instance, RaycastHit2D hit2D, bool __result)
        {
            try { Impact.AfterCharacter(__instance, hit2D, __result); } catch (Exception e) { PLog.Error("Hit hook failed", e); }
        }

        static void OnHit_Post(BasicBullet __instance, RaycastHit2D hit, bool __result)
        {
            try { Impact.AfterAnyHit(__instance, hit, __result); } catch (Exception e) { PLog.Error("Impact hook failed", e); }
        }
    }
}
