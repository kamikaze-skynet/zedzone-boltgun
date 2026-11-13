using BepInEx.Configuration;

namespace Boltgun
{
    // Everything tunable, in BepInEx\config\girthy_mods.zedzone.boltgun.cfg
    internal static class Settings
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> WeaponModGuids;
        internal static ConfigEntry<string> ExtraWeaponIds;

        internal static ConfigEntry<float> LaunchSpeed, IgnitionDelay, BurnTime, FinalSpeed, MinRange;
        internal static ConfigEntry<float> KineticDamage;
        internal static ConfigEntry<int> ArmorPenetration;
        internal static ConfigEntry<float> ArmingDistance, FuseDelay, InternalDamage, BlastDamage, BlastRadius, BlastVisualScale;
        internal static ConfigEntry<bool> DetonateOnSurfaces;
        internal static ConfigEntry<string> BlastPrefab;
        internal static ConfigEntry<bool> VerboseLog;

        static AcceptableValueRange<float> R(float a, float b) => new AcceptableValueRange<float>(a, b);

        internal static void Bind(ConfigFile c)
        {
            Enabled = c.Bind("General", "Enabled", true, "Turn the rocket-bolt behaviour on or off (the items stay).");
            WeaponModGuids = c.Bind("General", "WeaponModGuids",
                "f5fc9e37-40ad-46b3-ad71-6be84c155ae4,bac356ff-83fd-47f1-a864-1a33e3c7ba5c",
                "Comma-separated modGuids of weapons that fire rocket bolts. Defaults: this mod's Boltgun and Bolt Pistol. " +
                "Add another mod weapon's modGuid to give it the same rounds.");
            ExtraWeaponIds = c.Bind("General", "ExtraWeaponIds", "",
                "Extra item ids (comma-separated) that also fire rocket bolts, e.g. a vanilla gun. VerboseLog shows each weapon's id when it fires.");

            LaunchSpeed = c.Bind("Flight", "LaunchSpeed", 300f, new ConfigDescription("Speed (m/s) the powder charge gives the bolt leaving the barrel.", R(20, 3000)));
            IgnitionDelay = c.Bind("Flight", "IgnitionDelay", 0.02f, new ConfigDescription("Seconds of coasting before the rocket motor lights.", R(0, 1)));
            BurnTime = c.Bind("Flight", "BurnTime", 0.08f, new ConfigDescription("Seconds the motor burns while accelerating the bolt to FinalSpeed.", R(0.01f, 2)));
            FinalSpeed = c.Bind("Flight", "FinalSpeed", 975f, new ConfigDescription("Speed (m/s) at motor burn-out.", R(20, 3000)));
            MinRange = c.Bind("Flight", "MinRange", 0f, new ConfigDescription("If above 0, bolts fly at least this far (m). 0 keeps the weapon's range.", R(0, 500)));

            KineticDamage = c.Bind("Impact", "KineticDamage", 2000f, new ConfigDescription("Damage of the impact itself (about a 20 mm autocannon hit). 0 keeps the weapon's damage.", R(0, 20000)));
            ArmorPenetration = c.Bind("Impact", "ArmorPenetration", 8, new ConfigDescription("Armour-piercing level of the hardened tip (game scale, 8 = top tier).", new AcceptableValueRange<int>(0, 8)));

            ArmingDistance = c.Bind("Detonation", "ArmingDistance", 2f, new ConfigDescription("The fuse arms after the bolt has flown this far (m). Closer hits are kinetic only, so you can't blow yourself up point-blank.", R(0, 50)));
            FuseDelay = c.Bind("Detonation", "FuseDelay", 0.04f, new ConfigDescription("Seconds between impact and the charge going off inside the target.", R(0, 1)));
            InternalDamage = c.Bind("Detonation", "InternalDamage", 1500f, new ConfigDescription("Damage the charge does inside the body part it is lodged in.", R(0, 20000)));
            BlastDamage = c.Bind("Detonation", "BlastDamage", 250f, new ConfigDescription("Damage of the small blast at the detonation point. 0 = no blast.", R(0, 5000)));
            BlastRadius = c.Bind("Detonation", "BlastRadius", 1.2f, new ConfigDescription("Radius (m) of the small blast.", R(0.1f, 10)));
            BlastVisualScale = c.Bind("Detonation", "BlastVisualScale", 0.45f, new ConfigDescription("Explosion effect size compared with a 40 mm grenade.", R(0.05f, 3)));
            DetonateOnSurfaces = c.Bind("Detonation", "DetonateOnSurfaces", true, "Bolts that hit walls, vehicles or the ground also go off (blast only).");
            BlastPrefab = c.Bind("Detonation", "BlastPrefab", "FX/ExplosionEffect_40mmGrenade", "Game resource used for the blast.");

            VerboseLog = c.Bind("Debug", "VerboseLog", false, "Log every bolt fired, hit and detonated to BepInEx\\Boltgun.log.");
        }

        internal static string Describe() =>
            $"Config: Enabled={Enabled.Value} Flight {LaunchSpeed.Value}->{FinalSpeed.Value} m/s (ignite {IgnitionDelay.Value}s, burn {BurnTime.Value}s) " +
            $"Impact {KineticDamage.Value} AP{ArmorPenetration.Value} | Arm {ArmingDistance.Value}m Fuse {FuseDelay.Value}s Internal {InternalDamage.Value} " +
            $"Blast {BlastDamage.Value}/{BlastRadius.Value}m";
    }
}
