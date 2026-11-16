# Changelog

## 1.4.4
- Bigger casing: it fills its inventory slot and picture, and is about two thirds larger on the ground.

## 1.4.3
- The casing is now the whole round with the nose cone cut off (2x1 in the inventory), drawn at the same scale as the round.

## 1.4.2
- The casing recipe uses the game's Iron Plate directly (item 8) instead of looking it up by name.

## 1.4.1
- The casing recipe uses Iron Plate: 1 Iron Plate makes 20 .75 Bolt Casings.

## 1.4.0
- New item: .75 Bolt Casing (art cut from the new round). The Boltgun and Bolt Pistol eject one per shot like the game's
  own guns; it lands on the ground and can be picked up.
- Crafting: 1 iron presses 20 casings, and the .75 Bolt recipe now also needs 20 casings (for 20 bolts).
  The plugin finds the game's iron item by name when the game loads; if it picks the wrong one, set
  General / IronItemId in the config. The Boltgun.log "Casings:" line says which item it used.
- The plugin's drawn casings now only show for other players' shots.

## 1.3.9
- New .75 Bolt round art (inventory sprite and preview) and a new Workshop preview image.

## 1.3.8
- Works with Mod Menu (F11): change this mod's settings in game. No gameplay change.
- Same version number as on Girthy Mods.

## 1.3.7
- Laser sights: a fitted laser attached to the bolt guns but its beam stayed dark. After the game draws the laser,
  the plugin now checks it and, if the beam is still hidden on a bolt gun, casts it itself (stops at walls and
  targets) and shows the beam and dot. Config: General / LaserFix (on by default). The first few checks are written
  to BepInEx\Boltgun.log as "Laser:" lines.

## 1.3.6
- Sights and laser sights now fit the Boltgun and Bolt Pistol. The sight slot is open on both guns, and after the game
  loads mods the plugin adds the .75 Bolt to the calibre list of every sight-slot part and every laser part (this fixed
  "wrong caliber"). Config: General / Attachments (on by default).

## 1.3.5
- Magazine swaps (R): after the game loads mods, the plugin checks that the guns, magazines and .75 ammo point at each other
  (ammo id, magazine index) and repairs the links if the mod loader changed an id. Each R press on a bolt weapon logs
  which magazines the game found and how full they are, to track down failed swaps.

## 1.3.4
- Boltgun spread (fireAngle) 1.4. Recoil 1.0 on both guns (Bolt Pistol was 1.7).

## 1.3.3
- Boltgun: spread (fireAngle) 1.0 (was 1.2), recoil 1.0 (was 0.95).

## 1.3.2
- Spent casings are now our own fat brass .75 casings drawn by the plugin (the game's casing item was a .300 Magnum shell).
  They arc out of the gun, bounce, lie for 4 s and fade. Nothing to pick up. `[Visuals] SpentCasings` etc.
- Shorter range: Boltgun 40 m (was 80), Bolt Pistol 30 m (was 45), about one screen.

## 1.3.1
- Faster fire rate: Boltgun 600 rounds a minute (was 375), Bolt Pistol 0.2 s between shots (was 0.35).

## 1.3.0
- Multiplayer: other players' bolts now look and fly like yours (rocket boost, trail, bolt sprite, ignition flash and sound,
  the blast where they hit). The game rebuilds their shots from a network message without a weapon attached, so the
  plugin hooks NetProjectileSync.SpawnVisualBullet and reads the weapon id from that message. Damage, duds, gore,
  shrapnel damage and knockdowns stay with the shooter, as before.
- Rocket sound: a hiss when the motor lights, borrowed from the game (a firework's launch sound, else the ignite sound).
  `[Sound] RocketSoundVolume` and `RocketSoundClip` pick the volume and the sound.
- Painted the emblems off the rifle and pistol sprites (and their previews), ready for a public release.
- Girthy Mods / Hefty installs: the item packs ship in `items\` next to Boltgun.dll and the plugin loads any the game
  didn't (ModLoader.TryLoadZedmod after ModLoader.LoadAll). Workshop installs are unchanged.

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
