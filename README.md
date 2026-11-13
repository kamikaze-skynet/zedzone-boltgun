# Mass-Reactive Rounds (ZED ZONE)

A BepInEx (IL2CPP) companion mod that changes how .75 bolt weapons behave. It doesn't add or change
any item, sprite or sound. It only changes what happens to the round after it is fired.

Out of the box it targets the two .75 bolt weapons from these Workshop packs (you need them
installed for the mod to do anything):

- rifle: Workshop item 3815200763 (modGuid `54db9210-9262-49f2-9034-b473642e9b4b`)
- pistol: Workshop item 3815190597 (modGuid `55904d9b-d13a-458b-a958-8ae50d57bed8`)

Any other weapon can be added by item id in the config.

## What a round does

1. **Launch.** A propellant charge pushes the round out of the barrel at `LaunchSpeed` (300 m/s).
2. **Rocket motor.** After `IgnitionDelay` (0.02 s) the motor lights and accelerates the round
   to `FinalSpeed` (975 m/s) over `BurnTime` (0.08 s). The round visibly speeds up after
   leaving the barrel.
3. **Armour-piercing impact.** The hardened tip hits for `KineticDamage` (2000, about a 20 mm
   autocannon hit) at `ArmorPenetration` 8, the top tier of the game's armour-piercing scale.
   The round stops inside the target and doesn't pass through.
4. **Mass-reactive detonation.** `FuseDelay` (0.04 s) later the payload goes off inside the body
   part it hit for `InternalDamage` (1500), and a small blast (`BlastDamage` 250, `BlastRadius` 1.2 m)
   goes off at that spot. Rounds that hit walls, vehicles or the ground go off on contact (blast only).

All numbers are in `BepInEx\config\girthy_mods.zedzone.massreactive.cfg`.

## Multiplayer

Every player should run the mod. The player who fires the round handles its damage, the same way
the game handles normal bullets. Other players see the blast as an effect only. This has not been
tested in multiplayer yet.

## Logs

`BepInEx\MassReactive.log`. Set `VerboseLog = true` to log every round fired (with the weapon's
item id), every impact and every detonation.

## Building

```
dotnet build src/MassReactive.csproj -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\ZED ZONE"
```

## Publishing to the Steam Workshop

```
.\publish-workshop.ps1 -SteamUser <login>
```

## How it works (for modders)

- `BasicBullet.Init` (virtual, called by the weapon after it sets the round's fields): tags rounds whose
  `fireWeapon.weaponData.itemId` is a configured weapon, sets launch speed, damage, and penetrate count 0.
- A hidden host object ramps `bulletSpeed` every physics step. The game moves rounds by
  `transform.right * bulletSpeed * fixedDeltaTime`, and the range check uses the distance from `startPos`.
- `BasicBullet.HitCharacter`: the game passes `penetrateTime` to `OnDirectedHit` as `armorPenetration`,
  so it is raised for the hit only. Afterwards the counter is filled so `BulletPenetrate` spawns no clone.
- Detonation: `OnDirectedHit` on the struck body part, plus an instance of the game's
  `FX/ExplosionEffect_40mmGrenade` with `BasicExplosion.ConfigurePayload(damage, radius)`.
