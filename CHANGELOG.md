# Changelog

## 1.3.0
- Multiplayer: other players' bolts now look and fly like yours (rocket boost, trail, bolt sprite, ignition flash and sound,
  the blast where they hit). The game rebuilds their shots from a network message without a weapon attached, so the
  plugin hooks NetProjectileSync.SpawnVisualBullet and reads the weapon id from that message. Damage, duds, gore,
  shrapnel damage and knockdowns stay with the shooter, as before.
- Rocket sound: a hiss when the motor lights, borrowed from the game (a firework's launch sound, else the ignite sound).
  `[Sound] RocketSoundVolume` and `RocketSoundClip` pick the volume and the sound.

## 1.2.0
- Our own bolt in flight: a code-drawn bolt (dark casing, brass band, steel tip, glowing motor) rides on every round.
- Ignition flash: a flash and smoke puff where the rocket motor lights, a few metres from the barrel.
- Spent casings: both guns throw out a casing on each shot (the game's rifle casing, item 1005).
- Shrapnel: the internal charge throws fragments that hurt others within 3 m (150 damage, falling off with distance), with sparks.
- Wounds: a target that survives the charge is knocked down for 1.5 s.

## 1.1.0
- Rocket trail: a flame and lingering smoke trail from the moment the motor lights.
- Gore: the internal charge tears limbs off zombies (the game's own dismemberment) and pushes harder.
- Duds: 3% of fuses fail (kinetic hit only). Glancing hits on surfaces (shallower than 75 degrees) don't detonate.
- Heavier recoil (Boltgun 0.95, Bolt Pistol 1.7).
- Longer lore descriptions for every item.

## 1.0.0
- Standalone mod "Boltgun": Boltgun rifle, Bolt Pistol, .75 Bolt ammo and two magazines, with placeholder art.
- Rewritten plugin (Settings / Weapons / Flight / Impact / Detonation), arming distance so point-blank hits don't blow up the shooter.
- Rocket bolts: 300 m/s launch, rocket boost to 975 m/s, 20 mm-class armour-piercing impact, delayed
  detonation inside the target plus a small blast, contact detonation on surfaces. All configurable.

## 0.1.0 (unreleased)
- Companion-only version that changed another pack's bolt weapons.
