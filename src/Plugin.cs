using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace Boltgun
{
    [BepInPlugin(Guid, "Boltgun", Version)]
    public class Plugin : BasePlugin
    {
        public const string Guid = "girthy_mods.zedzone.boltgun";
        public const string Version = "1.1.0";

        public override void Load()
        {
            Settings.Bind(Config);
            PLog.Verbose = Settings.VerboseLog.Value;
            PLog.Open(Log, Version);
            PLog.Info(Settings.Describe());

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<BoltHost>();
                var go = new GameObject("Boltgun.Host");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                go.AddComponent<BoltHost>();
            }
            catch (Exception e) { PLog.Error("Could not create the host object; the mod is off", e); return; }

            Patches.Apply(new Harmony(Guid));
            PLog.Info($"Boltgun {Version} loaded.");
        }
    }

    // Hidden object that drives the rocket motors and the fuses.
    public class BoltHost : MonoBehaviour
    {
        public BoltHost(IntPtr ptr) : base(ptr) { }

        void FixedUpdate()
        {
            try { Flight.Tick(); }
            catch (Exception e) { PLog.Error("Flight update failed", e); Flight.Clear(); }
        }

        void Update()
        {
            try { Detonation.Tick(); }
            catch (Exception e) { PLog.Error("Fuse update failed", e); Detonation.Clear(); }
        }

        void LateUpdate()
        {
            try { Flight.FollowTrails(); }
            catch (Exception e) { PLog.Error("Trail update failed", e); }
        }
    }
}
