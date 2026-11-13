# Boltgun (ZED ZONE)

A standalone mod that adds a .75 **Boltgun** rifle, a **Bolt Pistol**, **.75 Bolt** rounds and their
magazines, plus a BepInEx (IL2CPP) plugin that makes every bolt fly and hit like a rocket-propelled
explosive round. The item art is a placeholder until we have our own (see `ART.md`).

| Item | Notes |
|---|---|
| Boltgun | full auto, 30-round magazine, 80 m range |
| Bolt Pistol | semi auto, one-handed, 10-round magazine |
| .75 Bolt | ammo, stacks to 40, craftable |
| Boltgun Magazine (30), Bolt Pistol Magazine (10) | craftable |

The guns spawn rarely in rooms and can be crafted.

## What a bolt does

1. **Launch.** A propellant charge pushes the bolt out of the barrel at `LaunchSpeed` (300 m/s).
2. **Rocket motor.** After `IgnitionDelay` (0.02 s) the motor lights and accelerates the bolt to
   `FinalSpeed` (975 m/s) over `BurnTime` (0.08 s). You can see the bolt speed up after it leaves the barrel.
3. **Armour-piercing impact.** The hardened tip hits for `KineticDamage` (2000, about a 20 mm
   autocannon hit) at `ArmorPenetration` 8, the top tier of the game's armour-piercing scale.
   The bolt stops inside the target and doesn't pass through.
4. **Mass-reactive detonation.** The fuse arms after `ArmingDistance` (2 m) of flight, so point-blank hits are
   kinetic only. `FuseDelay` (0.04 s) later the payload goes off inside the body
   part it hit for `InternalDamage` (1500), and a small blast (`BlastDamage` 250, `BlastRadius` 1.2 m)
   goes off at that spot. Bolts that hit walls, vehicles or the ground go off on contact (blast only).
5. **Lore touches.** The motor leaves a flame and smoke trail (`[Visuals]`). The internal charge hands
   zombies to the game's own dismemberment so limbs come off (`[Gore]`). `DudChance` (3%) of fuses fail,
   and bolts that strike a surface shallower than `GlanceAngle` (75 degrees from straight on) skip off
   without detonating. Recoil is heavier than a normal rifle, more so on the pistol.
6. **More lore (1.2.0).** Each round is drawn as our own bolt with a glowing motor (`BoltSprite`), the motor
   lights with a flash and smoke puff (`IgnitionFlash`), the guns throw out spent casings, the charge throws
   shrapnel at anyone within `ShrapnelRadius` (3 m), and a target that survives it is knocked down (`[Wounds]`).
7. **Rocket sound (1.3.0).** A hiss when the motor lights (`[Sound]`), using a sound already in the game.

Every number is in `BepInEx\config\girthy_mods.zedzone.boltgun.cfg`. `WeaponModGuids` lists the
weapons that fire rocket bolts. Add another weapon's modGuid there to give it the same rounds.

## Multiplayer

Every player needs the mod (the items are part of it anyway). The player who fires a bolt handles
its damage, duds, gore, shrapnel and knockdowns, the same way the game handles normal bullets.
Other players see the bolt fly and explode: the game rebuilds their shots as visual-only rounds in
`NetProjectileSync.SpawnVisualBullet`, which the plugin hooks to read the weapon id from the shot message.
Everyone needs the same Boltgun version so the item ids match. Not tested in multiplayer yet.

## Logs

`BepInEx\Boltgun.log`. Set `VerboseLog = true` to log every round fired (with the weapon's item id),
every impact and every detonation.

## Repo layout

- `src/` the plugin (rocket flight, impact, detonation)
- `items/<Type>/<item>/` sprites and previews (placeholder art)
- `tools/build_items.py` writes the item data and packs `workshop/content/<Type>/<guid>.zedmod` + `modpack.json`.
  Names and stats live in that script. Never change an item's guid after release.
- `workshop/` Steam Workshop content, preview and description; `publish-workshop.ps1` uploads it

## Building and publishing

```
python tools/build_items.py      # only after changing art, names or stats
.\publish-workshop.ps1 -SteamUser <login>                     # builds the DLL and uploads (Private on first upload)
```

## How the plugin works (for modders)

Code: `Settings` (config), `Weapons` (which item ids fire bolts), `Flight` (launch + rocket ramp),
`Impact` (hit hooks, duds, glancing hits), `Detonation` (fuse, internal damage, gore, blast), `Trail` (rocket exhaust), `Fx` (bolt sprite, ignition flash, sparks; sprites drawn in code because the game ships without Unity's image loader),
`Patches` (Harmony wiring).


- `BasicBullet.Init` (virtual, called by the weapon after it sets the round's fields): tags rounds whose
  `fireWeapon.weaponData.itemId` is one of the configured weapons, then sets launch speed, damage, and penetrate count 0.
  Item ids come from `ModLoader.loadedMods[guid].resolvedRuntimeId`.
- A hidden host object ramps `bulletSpeed` every physics step. The game moves rounds by
  `transform.right * bulletSpeed * fixedDeltaTime`, and the range check uses the distance from `startPos`.
- `BasicBullet.HitCharacter`: the game passes `penetrateTime` to `OnDirectedHit` as `armorPenetration`,
  so it is raised for the hit only. Afterwards the counter is filled so `BulletPenetrate` spawns no clone.
- Detonation: `OnDirectedHit` on the struck body part, plus an instance of the game's
  `FX/ExplosionEffect_40mmGrenade` with `BasicExplosion.ConfigurePayload(damage, radius)`.
- Gore: `ZombieController.OnExplosionDismember(damage, point, force)` after the internal hit.
- Trail: two free-standing `TrailRenderer` objects (Sprites/Default) that follow the bolt in `LateUpdate`
  and fade out after it stops, so nothing is left attached to a pooled or destroyed bullet.
