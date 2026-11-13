# Item art

The sprites in `items/` are **placeholders**, taken from the 爆弹枪 / 爆弹手枪 Workshop packs
(3815200763 / 3815190597) so the guns look right in game until we have our own art.

To replace a sprite, keep the same canvas size, or update that item's `ppu`, `spriteRegions` and
hand/muzzle offsets in `tools/build_items.py`. Then run `python tools/build_items.py`.
`tools/legacy/make_art.py` has the first original (rougher) set if you want a starting point.
