using System;
using HarmonyLib;
using UnityEngine;

namespace MassReactive
{
    internal static class Patches
    {
        internal static void Apply(Harmony h)
        {
            Patch(h, typeof(BasicBullet), nameof(BasicBullet.Init), nameof(Init_Prefix), nameof(Init_Postfix));
            Patch(h, typeof(BasicBullet), nameof(BasicBullet.HitCharacter), nameof(HitCharacter_Prefix), nameof(HitCharacter_Postfix));
            Patch(h, typeof(BasicBullet), nameof(BasicBullet.OnHit), null, nameof(OnHit_Postfix));
        }

        static void Patch(Harmony h, Type t, string method, string prefix, string postfix)
        {
            try
            {
                var target = AccessTools.Method(t, method);
                if (target == null) { PLog.Error($"{t.Name}.{method} not found; that part of the mod is off."); return; }
                h.Patch(target,
                    prefix == null ? null : new HarmonyMethod(typeof(Patches), prefix),
                    postfix == null ? null : new HarmonyMethod(typeof(Patches), postfix));
                PLog.Debug($"Patched {t.Name}.{method}");
            }
            catch (Exception e) { PLog.Error($"Patching {t.Name}.{method} failed", e); }
        }

        static void Init_Prefix(BasicBullet __instance, out float __state)
        {
            __state = 0f;
            try { __state = __instance.bulletSpeed; } catch { }
        }

        static void Init_Postfix(BasicBullet __instance, float __state)
        {
            try { Bolts.OnInit(__instance, __state); }
            catch (Exception e) { PLog.Error("Init hook failed", e); }
        }

        static void HitCharacter_Prefix(BasicBullet __instance)
        {
            try { Bolts.BeforeHitCharacter(__instance); }
            catch (Exception e) { PLog.Error("HitCharacter prefix failed", e); }
        }

        static void HitCharacter_Postfix(BasicBullet __instance, RaycastHit2D hit2D, bool __result)
        {
            try { Bolts.AfterHitCharacter(__instance, hit2D, __result); }
            catch (Exception e) { PLog.Error("HitCharacter postfix failed", e); }
        }

        static void OnHit_Postfix(BasicBullet __instance, RaycastHit2D hit, bool __result)
        {
            try { Bolts.AfterOnHit(__instance, hit, __result); }
            catch (Exception e) { PLog.Error("OnHit postfix failed", e); }
        }
    }
}
