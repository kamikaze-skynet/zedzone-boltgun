using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace MassReactive
{
    [BepInPlugin(Guid, "Mass-Reactive Rounds", Version)]
    public class Plugin : BasePlugin
    {
        public const string Guid = "girthy_mods.zedzone.massreactive";
        public const string Version = "0.1.0";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> WeaponModGuids;
        internal static ConfigEntry<string> ExtraWeaponIds;

        // Two-stage flight
        internal static ConfigEntry<float> LaunchSpeed;
        internal static ConfigEntry<float> IgnitionDelay;
        internal static ConfigEntry<float> BurnTime;
        internal static ConfigEntry<float> FinalSpeed;
        internal static ConfigEntry<float> MinRange;

        // Impact
        internal static ConfigEntry<float> KineticDamage;
        internal static ConfigEntry<int> ArmorPenetration;

        // Mass-reactive detonation
        internal static ConfigEntry<float> FuseDelay;
        internal static ConfigEntry<float> InternalDamage;
        internal static ConfigEntry<float> BlastDamage;
        internal static ConfigEntry<float> BlastRadius;
        internal static ConfigEntry<float> BlastVisualScale;
        internal static ConfigEntry<bool> DetonateOnSurfaces;
        internal static ConfigEntry<string> BlastPrefab;

        internal static ConfigEntry<bool> VerboseLog;

        public override void Load()
        {
            Enabled = Config.Bind("General", "Enabled", true, "Turn the mass-reactive behaviour on or off.");
            WeaponModGuids = Config.Bind("General", "WeaponModGuids",
                "54db9210-9262-49f2-9034-b473642e9b4b,55904d9b-d13a-458b-a958-8ae50d57bed8",
                "Comma-separated modGuids of the weapon items that fire mass-reactive rounds. The game's mod loader resolves each one to its item id. Defaults: the two .75 bolt weapons from the Workshop packs 3815200763 (rifle) and 3815190597 (pistol).");
            ExtraWeaponIds = Config.Bind("General", "ExtraWeaponIds", "",
                "Extra item ids (comma-separated) that also fire mass-reactive rounds. Turn VerboseLog on and fire a weapon to see its id in BepInEx\\MassReactive.log.");

            LaunchSpeed = Config.Bind("Flight", "LaunchSpeed", 300f,
                new ConfigDescription("Speed (m/s) the propellant charge gives the round as it leaves the barrel.", new AcceptableValueRange<float>(20f, 3000f)));
            IgnitionDelay = Config.Bind("Flight", "IgnitionDelay", 0.02f,
                new ConfigDescription("Seconds after leaving the barrel before the rocket motor lights.", new AcceptableValueRange<float>(0f, 1f)));
            BurnTime = Config.Bind("Flight", "BurnTime", 0.08f,
                new ConfigDescription("Seconds the rocket motor burns while it accelerates the round to FinalSpeed.", new AcceptableValueRange<float>(0.01f, 2f)));
            FinalSpeed = Config.Bind("Flight", "FinalSpeed", 975f,
                new ConfigDescription("Speed (m/s) at motor burn-out.", new AcceptableValueRange<float>(20f, 3000f)));
            MinRange = Config.Bind("Flight", "MinRange", 0f,
                new ConfigDescription("If above 0, rounds fly at least this far (m) before the game removes them. 0 keeps the weapon's own range.", new AcceptableValueRange<float>(0f, 500f)));

            KineticDamage = Config.Bind("Impact", "KineticDamage", 2000f,
                new ConfigDescription("Damage of the impact itself, before the payload goes off (about a 20 mm autocannon hit). 0 keeps the weapon's own damage.", new AcceptableValueRange<float>(0f, 20000f)));
            ArmorPenetration = Config.Bind("Impact", "ArmorPenetration", 8,
                new ConfigDescription("Armour-piercing level of the hard tip, for the impact and the detonation (the game's scale: 8 is the top tier).", new AcceptableValueRange<int>(0, 8)));

            FuseDelay = Config.Bind("Detonation", "FuseDelay", 0.04f,
                new ConfigDescription("Seconds between impact and the payload going off inside the target.", new AcceptableValueRange<float>(0f, 1f)));
            InternalDamage = Config.Bind("Detonation", "InternalDamage", 1500f,
                new ConfigDescription("Damage the payload does inside the body part it is lodged in.", new AcceptableValueRange<float>(0f, 20000f)));
            BlastDamage = Config.Bind("Detonation", "BlastDamage", 250f,
                new ConfigDescription("Damage of the small blast around the detonation point (hits everything nearby, including the shooter if too close). 0 turns the blast off.", new AcceptableValueRange<float>(0f, 5000f)));
            BlastRadius = Config.Bind("Detonation", "BlastRadius", 1.2f,
                new ConfigDescription("Radius (m) of the small blast.", new AcceptableValueRange<float>(0.1f, 10f)));
            BlastVisualScale = Config.Bind("Detonation", "BlastVisualScale", 0.45f,
                new ConfigDescription("Size of the explosion effect compared with a 40 mm grenade.", new AcceptableValueRange<float>(0.05f, 3f)));
            DetonateOnSurfaces = Config.Bind("Detonation", "DetonateOnSurfaces", true,
                "Rounds that hit a wall, vehicle or the ground also go off (blast only, no internal damage).");
            BlastPrefab = Config.Bind("Detonation", "BlastPrefab", "FX/ExplosionEffect_40mmGrenade",
                "Game resource used for the blast. Leave as is unless the game renames it.");

            VerboseLog = Config.Bind("Debug", "VerboseLog", false, "Log every round fired, hit and detonation to BepInEx\\MassReactive.log.");

            PLog.Verbose = VerboseLog.Value;
            PLog.Open(Log, Version);
            PLog.Info($"Config: Enabled={Enabled.Value} Launch={LaunchSpeed.Value} Final={FinalSpeed.Value} Ignition={IgnitionDelay.Value}s Burn={BurnTime.Value}s " +
                      $"Kinetic={KineticDamage.Value} AP={ArmorPenetration.Value} Fuse={FuseDelay.Value}s Internal={InternalDamage.Value} Blast={BlastDamage.Value}/{BlastRadius.Value}m");

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<BoltHost>();
                var go = new GameObject("MassReactive.Host");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                go.AddComponent<BoltHost>();
            }
            catch (Exception e) { PLog.Error("Could not create the host object", e); return; }

            var harmony = new Harmony(Guid);
            Patches.Apply(harmony);
            PLog.Info($"Mass-Reactive Rounds {Version} loaded.");
        }
    }
}
