#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Generate 5 color-tinted variants of the SunnyLand player sprites.

For each color the script:
  1. Duplicates every player PNG, multiplying RGB by the color (alpha kept).
  2. Writes a matching .meta with a fresh Unity GUID and the original importer
     settings (filter mode, compression, sprite mode ...).
  3. Rebuilds each .anim clip so its sprite-curve references the new tinted
     textures (by GUID), and renames the clip with a _<color> suffix.
  4. Rebuilds each .controller so its motion references the new clip, and
     renames the controller / its state with a _<color> suffix.

Variants are written to sibling folders  player_<color>/  next to the original
player/ folder, mirroring the climb / crouch / hurt / idle / jump / run
sub-folders.

Usage:
    py Tools/GeneratePlayerColorVariants.py
"""

import os
import re
import shutil
import uuid

from PIL import Image

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.dirname(SCRIPT_DIR)
SPRITES_ROOT = os.path.join(REPO_ROOT, "Assets", "SunnyLand Artwork", "Sprites")
PLAYER_DIR = os.path.join(SPRITES_ROOT, "player")

# Five deeper / more saturated multiply colors (RGB 0-255).
# result = base * color / 255  ->  white pixels take the color, dark outlines
# stay dark, so the character reads as a tinted version of the original.
COLORS = {
    "crimson":     (190, 35, 50),
    "royalblue":   (45, 70, 195),
    "forestgreen": (30, 105, 50),
    "amber":       (200, 140, 30),
    "violet":      (120, 50, 170),
}

SUBFOLDERS = ["climb", "crouch", "hurt", "idle", "jump", "run"]

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def new_guid():
    """A Unity-style 32-char lowercase hex GUID (no dashes)."""
    return uuid.uuid4().hex


def read_meta_guid(meta_path):
    """Pull the `guid:` value out of a Unity .meta file."""
    with open(meta_path, "r", encoding="utf-8") as f:
        for line in f:
            m = re.match(r"guid: ([0-9a-f]{32})\s*$", line)
            if m:
                return m.group(1)
    return None


def write_text(path, text):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def make_folder_meta(meta_path):
    write_text(meta_path, (
        "fileFormatVersion: 2\n"
        f"guid: {new_guid()}\n"
        "folderAsset: yes\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    ))


def make_native_meta(meta_path, main_object_file_id):
    """Write a minimal NativeFormatImporter .meta and return its new GUID."""
    guid = new_guid()
    write_text(meta_path, (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "NativeFormatImporter:\n"
        "  externalObjects: {}\n"
        f"  mainObjectFileID: {main_object_file_id}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    ))
    return guid


def tint_texture(src_png, dst_png, color):
    """Multiply the RGB channels by `color` (0-255), keep alpha untouched."""
    img = Image.open(src_png).convert("RGBA")
    cr, cg, cb = color
    r, g, b, a = img.split()
    r = r.point(lambda v: min(255, int(v * cr / 255 + 0.5)))
    g = g.point(lambda v: min(255, int(v * cg / 255 + 0.5)))
    b = b.point(lambda v: min(255, int(v * cb / 255 + 0.5)))
    Image.merge("RGBA", (r, g, b, a)).save(dst_png)


def make_texture_meta(src_meta, dst_meta):
    """Copy a texture .meta verbatim but with a fresh GUID. Returns the GUID."""
    with open(src_meta, "r", encoding="utf-8") as f:
        text = f.read()
    guid = new_guid()
    text = re.sub(r"(?m)^guid: [0-9a-f]{32}", f"guid: {guid}", text, count=1)
    write_text(dst_meta, text)
    return guid


def append_color_to_names(text, color):
    """Append _<color> to every `  m_Name: <x>` line whose value isn't Base Layer."""

    def repl(m):
        value = m.group(1).strip()
        if value == "Base Layer":
            return m.group(0)
        return f"  m_Name: {value}_{color}"

    return re.sub(r"(?m)^  m_Name: (.+)$", repl, text)


def find_files(root, ext):
    result = []
    for dirpath, _dirs, files in os.walk(root):
        for fn in files:
            # ".png" must not match "foo.png.meta" -> exact endswith check is fine
            # because "foo.png.meta".endswith(".png") is False.
            if fn.endswith(ext):
                result.append(os.path.join(dirpath, fn))
    return result


# ---------------------------------------------------------------------------
# Core
# ---------------------------------------------------------------------------


def process_color(color_name, color_rgb):
    out_root = os.path.join(SPRITES_ROOT, f"player_{color_name}")

    # Start clean so re-runs don't leave stale GUIDs around.
    if os.path.isdir(out_root):
        shutil.rmtree(out_root)
    if os.path.exists(out_root + ".meta"):
        os.remove(out_root + ".meta")

    os.makedirs(out_root, exist_ok=True)
    make_folder_meta(out_root + ".meta")
    for sub in SUBFOLDERS:
        sub_path = os.path.join(out_root, sub)
        os.makedirs(sub_path, exist_ok=True)
        make_folder_meta(sub_path + ".meta")

    tex_guid_map = {}    # original texture guid -> new texture guid (this color)
    anim_guid_map = {}   # original anim guid -> new anim guid (this color)

    # 1. Textures ----------------------------------------------------------
    tex_count = 0
    for png in find_files(PLAYER_DIR, ".png"):
        rel = os.path.relpath(png, PLAYER_DIR).replace(os.sep, "/")
        stem = os.path.splitext(os.path.basename(png))[0]
        dst_png = os.path.join(out_root, os.path.dirname(rel).replace("/", os.sep),
                               f"{stem}_{color_name}.png")
        os.makedirs(os.path.dirname(dst_png), exist_ok=True)

        src_meta = png + ".meta"
        orig_guid = read_meta_guid(src_meta)
        tint_texture(png, dst_png, color_rgb)
        tex_guid_map[orig_guid] = make_texture_meta(src_meta, dst_png + ".meta")
        tex_count += 1

    # 2. Animations --------------------------------------------------------
    anim_count = 0
    for anim in find_files(PLAYER_DIR, ".anim"):
        rel = os.path.relpath(anim, PLAYER_DIR).replace(os.sep, "/")
        stem = os.path.splitext(os.path.basename(anim))[0]
        dst_anim = os.path.join(out_root, os.path.dirname(rel).replace("/", os.sep),
                                f"{stem}_{color_name}.anim")
        os.makedirs(os.path.dirname(dst_anim), exist_ok=True)

        with open(anim, "r", encoding="utf-8") as f:
            text = f.read()

        # Re-point every sprite reference at the tinted texture for this color.
        for old, new in tex_guid_map.items():
            text = text.replace(old, new)
        text = append_color_to_names(text, color_name)

        write_text(dst_anim, text)
        orig_anim_guid = read_meta_guid(anim + ".meta")
        new_anim_guid = make_native_meta(dst_anim + ".meta", 7400000)
        anim_guid_map[orig_anim_guid] = new_anim_guid
        anim_count += 1

    # 3. Controllers -------------------------------------------------------
    ctrl_count = 0
    for ctrl in find_files(PLAYER_DIR, ".controller"):
        rel = os.path.relpath(ctrl, PLAYER_DIR).replace(os.sep, "/")
        stem = os.path.splitext(os.path.basename(ctrl))[0]
        dst_ctrl = os.path.join(out_root, os.path.dirname(rel).replace("/", os.sep),
                                f"{stem}_{color_name}.controller")
        os.makedirs(os.path.dirname(dst_ctrl), exist_ok=True)

        with open(ctrl, "r", encoding="utf-8") as f:
            text = f.read()

        # Re-point the motion reference at the new clip.
        for old, new in anim_guid_map.items():
            text = text.replace(old, new)
        text = append_color_to_names(text, color_name)

        write_text(dst_ctrl, text)
        make_native_meta(dst_ctrl + ".meta", 9100000)
        ctrl_count += 1

    print(f"[{color_name:12s}] textures={tex_count:2d}  anims={anim_count}  "
          f"controllers={ctrl_count}  -> {out_root}")


def main():
    if not os.path.isdir(PLAYER_DIR):
        raise SystemExit(f"Player sprite folder not found: {PLAYER_DIR}")
    print(f"Source: {PLAYER_DIR}")
    for color_name, color_rgb in COLORS.items():
        process_color(color_name, color_rgb)
    print("Done.")


if __name__ == "__main__":
    main()
