"""Writes the game's item mods (.zedmod = zip of mod.json + sprite.png + preview.jpg + locale)
into workshop/content, plus modpack.json. Names come from NAMES below, so a rename is one edit.
Run:  python tools/build_items.py"""
import json, os, zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.join(HERE, "..")
ITEMS = os.path.join(REPO, "items")
CONTENT = os.path.join(REPO, "workshop", "content")

PACK = "Boltgun"
NAMES = {
    "rifle":      ("Boltgun", "A .75 calibre war-rifle, hand-forged and rite-blessed in an age that worshipped war, and far too heavy for an ordinary soldier. "
                   "It does not fire bullets. Each bolt is a small rocket: a powder charge throws it from the barrel, a solid-fuel motor lights a heartbeat later "
                   "and drives it to 975 m/s, a hardened tip punches through plate, and a mass-reactive fuse waits until the bolt is buried in its target before the charge inside goes off. "
                   "The thunder of its firing is said to be a prayer, and every kill an answered one."),
    "pistol":     ("Bolt Pistol", "A brutal .75 sidearm built for hands larger than yours, carried by officers and zealots as both weapon and badge of office. "
                   "It fires the same rocket-propelled, armour-piercing explosive bolts as the Boltgun, in a frame barely able to contain them. "
                   "The recoil is a sermon of its own."),
    "shell75":    (".75 Bolt", "Self-propelled .75 explosive round. Propellant base, solid-fuel rocket motor, hardened armour-piercing tip and a mass-reactive "
                   "fuse that only arms after a short flight and detonates the charge once the bolt has buried itself in something dense. "
                   "Not every bolt is perfect: a few fuses fail, and a bolt that strikes at too shallow an angle skips off without going off."),
    "rifle_mag":  ("Boltgun Magazine (30)", "Curved box magazine holding 30 .75 bolts. Its spring is stiff enough to take a finger off, and the rounds inside are prayed over before loading."),
    "pistol_mag": ("Bolt Pistol Magazine (10)", "Box magazine holding 10 .75 bolts. Heavy for its size: each round is a rocket in its own right."),
}

# Fixed ids. Never change a guid after release: saves and other mods refer to it.
GUID = {
    "rifle":      "f5fc9e37-40ad-46b3-ad71-6be84c155ae4",
    "pistol":     "bac356ff-83fd-47f1-a864-1a33e3c7ba5c",
    "shell75":    "cfe0eb6a-4ba6-46f6-b7fc-0476d0a6cd2e",
    "rifle_mag":  "8c0b9189-f401-48b1-8273-18bf8ed4f4ad",
    "pistol_mag": "a9462c6d-1c5f-4cfe-b1d7-87bf808df939",
}
# Requested runtime ids; the game's mod loader remaps them if they clash.
RID = {"shell75": 751075, "rifle_mag": 751076, "pistol_mag": 751077, "rifle": 751078, "pistol": 751079}

def base(key, mod_type, ppu, regions, size, rarity, weight, price, stack, craft, platform, out_n, repair, spawn=True, hidden=False):
    name, desc = NAMES[key]
    return {
        "modType": mod_type, "modGuid": GUID[key], "runtimeId": RID[key], "publishMode": 0, "targetNativeItemId": 0,
        "primaryLangCode": "en_US", "spritePath": "sprite.png", "ppu": ppu,
        "spriteRegions": [{"slotName": s, "region": {"x": r[0], "y": r[1], "w": r[2], "h": r[3]}} for s, r in regions],
        "itemName": name, "itemName_EN": name, "itemDescription": desc, "itemDescription_EN": desc,
        "itemRarity": rarity, "hiddenItem": hidden, "itemInventorySizeX": size[0], "itemInventorySizeY": size[1],
        "generateItemInRoom": spawn, "generateItemInField": False, "generateItemInCoastFloat": 0.0,
        "weight": weight, "itemPrice": price, "stackNumber": stack,
        "craftRecipeItems": [{"itemId": i, "itemNumber": float(n)} for i, n in craft],
        "craftPlatform": platform, "craftAudioType": 0, "craftOutputNumber": out_n,
        "repairRecipeItems": [{"itemId": i, "itemNumber": float(n)} for i, n in repair],
    }

# Spent casing thrown out on each shot: the game's own rifle casing item (the same id other weapon mods use).
SHELL_CASING = 1005

def weapon_common():
    return {
        "feedingType": 0, "exinjectShellMode": 0, "isMechWeapon": False, "isFlareGun": False,
        "hp": 1500.0, "smokeParticleNumber": 30, "temperaturePerShot": 2.0, "ammoPlusOne": False,
        "ammoId": RID["shell75"], "shellId": SHELL_CASING, "shellExinjectionForceFloat": 1.6,
        "liquidAmmoItemID": -1, "extraLiquidAmmoItemIDAry": [], "liquidCapacity": 0.0, "batteryEnergyPerSec": 0.0,
        "isAirPowerd": False, "airPressure_redZoneSize": 0.0, "airPressure_yellowZoneSize": 0.0,
        "airPressure_greenZoneSize": 0.0, "airPressure_chargeZoneSize": 0.0, "movingPartRange": 0.0,
        "hasMainGripBaseline": True, "mainGripBaselineY": 0.09031009674072266, "handPoseVersion": 1,
        "partModifiable_Barrel": False, "partModifiable_Aimer": False, "partModifiable_Gunstock": False,
        "bulletPrefabName": "50BMGBullet", "fireClip": "50BMG_Fire_0", "fillingClip": "AK_Reload_2",
        "reloadClip": "Uzi_Reload", "reloadFinishedClip": "Shotgun_Reload", "boltActionClip": "",
        "fireClipVolume": 1.0, "fireClipPitch": 0.53, "fillingClipVolume": 1.0, "fillingClipPitch": 1.0,
        "reloadClipVolume": 1.0, "reloadClipPitch": 1.0, "reloadFinishedClipVolume": 1.0, "reloadFinishedClipPitch": 1.0,
        "boltActionClipVolume": 1.0, "boltActionClipPitch": 1.0,
    }

# Placeholder sprites; regions and hand/muzzle offsets match them. See ART.md before swapping art.
RIFLE_REGIONS = [("Main", (3, 15, 253, 118)), ("MainBody", (21, 21, 229, 110)), ("MovingPart", (99, 31, 45, 17)),
                 ("Barrel", (221, 42, 29, 25)), ("Aimer", (73, 17, 128, 11)), ("Gunstock", (21, 21, 32, 63))]
PISTOL_REGIONS = [("Main", (2, 0, 254, 195)), ("MainBody", (15, 0, 241, 191)), ("MovingPart", (70, 54, 38, 26)),
                  ("Barrel", (207, 41, 43, 39)), ("Aimer", (83, 0, 128, 22)), ("Gunstock", (0, 21, 16, 63))]

def rifle():
    d = base("rifle", "RangedWeapon", 230, RIFLE_REGIONS, (4, 4), 0.93, 6.5, 2500.0, 0,
             [(8, 12), (29, 6), (13, 4)], 2, 1, [(8, 1)])
    d.update(weapon_common())
    d.update({
        "weaponType": 2, "weaponSlotSize": 1, "gunActionType": 0, "singleHand": False,
        "moveSpeedFloat": 0.85, "damage": 2000.0, "fireRange": 80.0, "noiseRange": 70.0,
        "bulletForce": 1400.0, "bulletSpeed": 300.0, "fireAngle": 1.2, "bulletNumber": 1,
        "fireInterval": 0.1, "recoilIntensity": 0.95, "reloadTime": 3.2,
        "defaultMagazineId": RID["rifle_mag"], "magazineSize": 30,
        "weaponHandHeightOffset": -0.2847541570663452, "weaponHandOffsetRange": 0.3921833336353302,
        "leftHandOffsetRange": 0.4398137927055359, "leftHandHeightOffset": -0.0033399462699890138,
        "spriteOffset_x": 0.293729305267334, "spriteOffset_y": 0.2013387680053711,
        "gunPointOffset_x": 0.35866662859916687, "gunPointOffset_y": 0.22095239162445068,
        "magazineOffset_x": 0.22326290607452393, "magazineOffset_y": 0.2423933744430542,
        "barrelOffset_x": 0.26514285802841187, "barrelOffset_y": 0.11695237457752228, "aimerOffset_x": 0.045904770493507385, "aimerOffset_y": 0.12323809415102005,
        "gunstockOffset_x": -0.08761905878782272, "gunstockOffset_y": 0.024761900305747986,
    })
    return d

def pistol():
    d = base("pistol", "RangedWeapon", 350, PISTOL_REGIONS, (4, 4), 0.9, 3.0, 1500.0, 0,
             [(8, 8), (29, 4), (13, 2)], 2, 1, [(8, 1)])
    d.update(weapon_common())
    d.update({
        "weaponType": 1, "weaponSlotSize": 0, "gunActionType": 1, "singleHand": True,
        "moveSpeedFloat": 1.0, "damage": 2000.0, "fireRange": 45.0, "noiseRange": 60.0,
        "bulletForce": 1400.0, "bulletSpeed": 300.0, "fireAngle": 2.5, "bulletNumber": 1,
        "fireInterval": 0.2, "recoilIntensity": 1.7, "reloadTime": 2.6,
        "defaultMagazineId": RID["pistol_mag"], "magazineSize": 10,
        "weaponHandHeightOffset": 0.1802505999803543, "weaponHandOffsetRange": 0.7120091915130615,
        "leftHandOffsetRange": 0.20000000298023225, "leftHandHeightOffset": 0.029999999329447748,
        "spriteOffset_x": 0.19030076265335084, "spriteOffset_y": 0.1703100949525833,
        "gunPointOffset_x": 0.5072380304336548, "gunPointOffset_y": 0.31066668033599854,
        "magazineOffset_x": 0.26438093185424805, "magazineOffset_y": 0.145523801445961,
        "barrelOffset_x": 0.46399998664855957, "barrelOffset_y": 0.24666666984558105, "aimerOffset_x": 0.10304762423038483, "aimerOffset_y": 0.2746666669845581,
        "gunstockOffset_x": -0.17733334004878998, "gunstockOffset_y": 0.19333332777023315,
    })
    return d

def shell():
    d = base("shell75", "Ammo", 2500, [("Main", (12, 7, 223, 91))], (2, 1), 0.75, 0.1, 60.0, 40,
             [(7, 10), (9, 5), (75, 5)], 3, 20, [(56, 1)])
    d.update({"ammoNumber": 5, "ammoBaseDamage": 2000.0})
    return d

def mag(key, ppu, region, size, capacity, size_float, fit, off):
    d = base(key, "Magazine", ppu, [("Main", region)], size, 0.25, 0.3 + capacity * 0.03, 150.0, 0,
             [(34, 2 + capacity // 10), (35, 1 + capacity // 20)], 2, 1, [(35, 1)])
    d.update({"ammoId": RID["shell75"], "magazineSize": capacity, "durabilityCostPerShot": 0.01,
              "sizeFloat": size_float, "magazineFit": fit, "magazineOffset_x": off[0], "magazineOffset_y": off[1]})
    return d

BUILD = {
    ("RangedWeapons", "rifle"): rifle,
    ("RangedWeapons", "pistol"): pistol,
    ("Ammo", "shell75"): shell,
    ("Magazine", "rifle_mag"): lambda: mag("rifle_mag", 400, (96, 7, 67, 132), (2, 4), 30, 1.2000000476837158, 1, (0.012888908386230469, -0.12591969966888428)),
    ("Magazine", "pistol_mag"): lambda: mag("pistol_mag", 1000, (23, 14, 211, 231), (2, 3), 10, 1.0, 0, (0.005671977996826172, -0.09917294979095459)),
}

def main():
    os.makedirs(CONTENT, exist_ok=True)
    pack = {"packName": PACK, "packDescription": NAMES["rifle"][1], "author": "girthy_mods",
            "createTime": "2026-11-13 00:00:00", "mods": []}
    for (kind, key), fn in BUILD.items():
        src = os.path.join(ITEMS, kind, key)
        d = fn()
        name, desc = NAMES[key]
        locale = {"langCode": "en_US", "modGuid": d["modGuid"], "translatorNote": "", "itemName": name,
                  "itemDescription": desc, "extraFields": []}
        out_dir = os.path.join(CONTENT, kind); os.makedirs(out_dir, exist_ok=True)
        rel = f"{kind}/{d['modGuid']}.zedmod"
        with zipfile.ZipFile(os.path.join(CONTENT, rel), "w", zipfile.ZIP_DEFLATED) as z:
            z.writestr("mod.json", json.dumps(d, indent=4, ensure_ascii=False))
            z.write(os.path.join(src, "sprite.png"), "sprite.png")
            z.write(os.path.join(src, "preview.jpg"), "preview.jpg")
            z.writestr("locale/en_US.json", json.dumps(locale, indent=4, ensure_ascii=False))
        pack["mods"].append({"modGuid": d["modGuid"], "modType": d["modType"], "itemName": name, "relativePath": rel})
        print("built", rel, name)
    with open(os.path.join(CONTENT, "modpack.json"), "w", encoding="utf-8") as f:
        json.dump(pack, f, indent=4, ensure_ascii=False)

if __name__ == "__main__":
    main()
