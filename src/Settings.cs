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
        internal static ConfigEntry<float> DudChance, GlanceAngle;
        internal static ConfigEntry<bool> Gore;
        internal static ConfigEntry<float> InternalForce, GoreForce;
        internal static ConfigEntry<bool> Trail;
        internal static ConfigEntry<float> TrailWidth, SmokeTime;
        internal static ConfigEntry<bool> BoltSprite, IgnitionFlash, Knockdown;
        internal static ConfigEntry<float> BoltLength, IgnitionFlashSize, ShrapnelDamage, ShrapnelRadius, KnockdownTime;
        internal static ConfigEntry<int> ShrapnelSparks;
        internal static ConfigEntry<float> RocketSoundVolume;
        internal static ConfigEntry<string> RocketSoundClip;
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

            DudChance = c.Bind("Detonation", "DudChance", 0.03f, new ConfigDescription("Chance (0-1) that a bolt's fuse fails and it only does the kinetic hit.", R(0, 1)));
            GlanceAngle = c.Bind("Detonation", "GlanceAngle", 75f, new ConfigDescription("Bolts that strike a wall or the ground at a shallower angle than this (degrees from straight on) glance off without detonating. 90 = never.", R(0, 90)));

            InternalForce = c.Bind("Gore", "InternalForce", 2.5f, new ConfigDescription("Push of the internal charge, as a multiple of the bolt's force. Big pushes make the game's own gore (blown-off limbs, split corpses) more likely.", R(0, 20)));
            Gore = c.Bind("Gore", "BlowApartZombies", true, "The internal charge tears limbs off zombies through the game's own dismemberment.");
            GoreForce = c.Bind("Gore", "GoreForce", 3000f, new ConfigDescription("Force handed to the zombie dismemberment when the charge goes off.", R(0, 50000)));

            Trail = c.Bind("Visuals", "RocketTrail", true, "Show the rocket motor's flame and smoke trail behind each bolt.");
            TrailWidth = c.Bind("Visuals", "TrailWidth", 0.06f, new ConfigDescription("Width (m) of the rocket trail.", R(0.005f, 1)));
            SmokeTime = c.Bind("Visuals", "SmokeTime", 0.35f, new ConfigDescription("Seconds the smoke trail lingers.", R(0.02f, 5)));

            BoltSprite = c.Bind("Visuals", "BoltSprite", true, "Draw our own bolt (casing, brass band, steel tip, glowing motor) on each round in flight.");
            BoltLength = c.Bind("Visuals", "BoltLength", 0.35f, new ConfigDescription("Length (m) the bolt sprite is drawn at.", R(0.05f, 2)));
            IgnitionFlash = c.Bind("Visuals", "IgnitionFlash", true, "A flash and a puff of smoke where the rocket motor lights, a few metres from the barrel.");
            IgnitionFlashSize = c.Bind("Visuals", "IgnitionFlashSize", 1f, new ConfigDescription("Size of the ignition flash and puff.", R(0.1f, 5)));

            ShrapnelDamage = c.Bind("Shrapnel", "ShrapnelDamage", 150f, new ConfigDescription("Damage the bolt's fragments do to others near the target (falls off with distance). 0 = off.", R(0, 5000)));
            ShrapnelRadius = c.Bind("Shrapnel", "ShrapnelRadius", 3f, new ConfigDescription("How far (m) fragments fly.", R(0.1f, 15)));
            ShrapnelSparks = c.Bind("Shrapnel", "ShrapnelSparks", 10, new ConfigDescription("Number of fragment sparks drawn.", new AcceptableValueRange<int>(0, 60)));

            Knockdown = c.Bind("Wounds", "Knockdown", true, "A target that survives the internal charge is knocked off its feet.");
            KnockdownTime = c.Bind("Wounds", "KnockdownTime", 1.5f, new ConfigDescription("Seconds the survivor stays down.", R(0.1f, 10)));

            RocketSoundVolume = c.Bind("Sound", "RocketSoundVolume", 0.6f, new ConfigDescription("Volume of the hiss when a bolt's motor lights. 0 = silent.", R(0, 2)));
            RocketSoundClip = c.Bind("Sound", "RocketSoundClip", "", "Name of a game sound to use for the motor. Empty = a firework's launch sound, else the game's ignite sound. With VerboseLog on, the log lists sounds that might fit.");

            VerboseLog = c.Bind("Debug", "VerboseLog", false, "Log every bolt fired, hit and detonated to BepInEx\\Boltgun.log.");
        }

        internal static string Describe() =>
            $"Config: Enabled={Enabled.Value} Flight {LaunchSpeed.Value}->{FinalSpeed.Value} m/s (ignite {IgnitionDelay.Value}s, burn {BurnTime.Value}s) " +
            $"Impact {KineticDamage.Value} AP{ArmorPenetration.Value} | Arm {ArmingDistance.Value}m Fuse {FuseDelay.Value}s Internal {InternalDamage.Value} " +
            $"Blast {BlastDamage.Value}/{BlastRadius.Value}m Dud {DudChance.Value} Glance {GlanceAngle.Value} Gore {Gore.Value} Trail {Trail.Value}";
    }
}
