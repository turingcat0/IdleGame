import re
import shutil
from pathlib import Path

SCENE_PATH = Path(r'd:\Workspace\xluaDemo\Assets\Scene\main.unity')
BACKUP_PATH = SCENE_PATH.with_suffix('.unity.before-slot-clone')

# Slot name -> GameObject fileID
SLOTS = {
    'Slot1': 1027335126,
    'Slot2': 1779874143,
    'Slot3': 294862561,
    'Slot4': 1239780436,
    'Slot5': 1745164905,
    'Slot6': 457366807,
    'Slot7': 43961237,
    'Slot8': 1583381717,
}

# Slot1 child fileIDs (in RectTransform child order)
SLOT1_CHILDREN = [2124822734, 748622252, 146485240]
SLOT1_BACKGROUND_ICON = 1366203348
SLOT1_DUMMY_IMAGE = 371032579

# Sprite used by Slot1
SLOT1_SPRITE = 'dcdc43f7a0ad67b4ab3e3db38233dfae'
SLOT1_DUMMY_CHILD_SPRITE = 'f2c6f216748c23e48844cac363144790'


def get_object_block(content: str, fileid: int) -> str:
    """Extract a Unity YAML object block by its &fileID."""
    pattern = rf'--- !u!\d+ &{fileid}\b.*?\n(?=--- !u|\Z)'
    match = re.search(pattern, content, re.DOTALL)
    if not match:
        raise ValueError(f'Could not find object with fileID {fileid}')
    return match.group(0)


def replace_object_block(content: str, fileid: int, new_block: str) -> str:
    """Replace a Unity YAML object block by its &fileID."""
    pattern = rf'--- !u!\d+ &{fileid}\b.*?\n(?=--- !u|\Z)'
    def repl(m):
        return new_block
    return re.sub(pattern, repl, content, count=1, flags=re.DOTALL)


def get_component_fileids_of_go(content: str, go_fileid: int) -> list[int]:
    """Get component fileIDs from a GameObject block."""
    block = get_object_block(content, go_fileid)
    ids = [int(m.group(1)) for m in re.finditer(r'- component: \{fileID: (\d+)\}', block)]
    return ids


def get_rect_transform(content: str, go_fileid: int) -> int:
    """Find the RectTransform component fileID for a GameObject."""
    for cid in get_component_fileids_of_go(content, go_fileid):
        block = get_object_block(content, cid)
        if block.startswith('--- !u!224'):
            return cid
    raise ValueError(f'No RectTransform found for GO {go_fileid}')


def get_go_from_component(content: str, comp_fileid: int) -> int:
    """Get the GameObject fileID referenced by a component block."""
    block = get_object_block(content, comp_fileid)
    match = re.search(r'm_GameObject: \{fileID: (\d+)\}', block)
    if not match:
        raise ValueError(f'No GameObject reference found for component {comp_fileid}')
    return int(match.group(1))


def get_image_component(content: str, go_fileid: int) -> int:
    """Find the Image (u!114 with Image script) component fileID for a GameObject."""
    for cid in get_component_fileids_of_go(content, go_fileid):
        block = get_object_block(content, cid)
        if 'm_Sprite:' in block and 'm_Type:' in block and f'm_GameObject: {{fileID: {go_fileid}}}' in block:
            # Distinguish Image from other MonoBehaviour by the Image script GUID
            if 'guid: fe87c0e1cc204ed48ad3b37840f39efc' in block:
                return cid
    raise ValueError(f'No Image found for GO {go_fileid}')


def get_button_component(content: str, go_fileid: int) -> int:
    for cid in get_component_fileids_of_go(content, go_fileid):
        block = get_object_block(content, cid)
        if 'guid: 4e29b1a8efbd4b44bb3f3716e73f07ff' in block:
            return cid
    raise ValueError(f'No Button found for GO {go_fileid}')


def get_children_from_rt(content: str, rt_fileid: int) -> list[int]:
    block = get_object_block(content, rt_fileid)
    children_match = re.search(r'm_Children:\n((?:  - \{fileID: \d+\}\n)*)', block)
    if not children_match or not children_match.group(1).strip():
        return []
    return [int(m.group(1)) for m in re.finditer(r'fileID: (\d+)', children_match.group(1))]


def get_size_of_slot(content: str, rt_fileid: int) -> tuple[float, float]:
    block = get_object_block(content, rt_fileid)
    sx = float(re.search(r'm_SizeDelta: \{x: ([\d.-]+),', block).group(1))
    sy = float(re.search(r'm_SizeDelta: \{x: [\d.-]+, y: ([\d.-]+)\}', block).group(1))
    return sx, sy


def set_field(block: str, field: str, value: str) -> str:
    """Replace a top-level field value in a YAML block."""
    pattern = rf'(^{field}: ).*$'
    def repl(m):
        return m.group(1) + value
    return re.sub(pattern, repl, block, flags=re.MULTILINE)


def set_yaml_field(block: str, field: str, value: str) -> str:
    """Set a YAML field, supporting multi-line values that start on next line."""
    # For simple single-line fields
    return set_field(block, field, value)


def make_gameobject_yaml(fileid: int, name: str, layer: int, components: list[int]) -> str:
    comp_lines = '\n'.join(f'  - component: {{fileID: {c}}}' for c in components)
    return f'''--- !u!1 &{fileid}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comp_lines}
  m_Layer: {layer}
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
'''


def make_rect_transform_yaml(fileid: int, go_fileid: int, father_fileid: int,
                             root_order: int, children: list[int],
                             anchor_min: tuple, anchor_max: tuple,
                             anchored_pos: tuple, size_delta: tuple,
                             pivot: tuple) -> str:
    child_lines = '\n'.join(f'  - {{fileID: {c}}}' for c in children) if children else '  []'
    return f'''--- !u!224 &{fileid}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fileid}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{child_lines}
  m_Father: {{fileID: {father_fileid}}}
  m_RootOrder: {root_order}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {anchor_min[0]}, y: {anchor_min[1]}}}
  m_AnchorMax: {{x: {anchor_max[0]}, y: {anchor_max[1]}}}
  m_AnchoredPosition: {{x: {anchored_pos[0]}, y: {anchored_pos[1]}}}
  m_SizeDelta: {{x: {size_delta[0]}, y: {size_delta[1]}}}
  m_Pivot: {{x: {pivot[0]}, y: {pivot[1]}}}
'''


def make_image_yaml(fileid: int, go_fileid: int, color: tuple, sprite: str,
                    img_type: int = 0, raycast: int = 1) -> str:
    if sprite == '0':
        sprite_line = '  m_Sprite: {fileID: 0}'
    else:
        sprite_line = f'  m_Sprite: {{fileID: 21300000, guid: {sprite}, type: 3}}'
    return f'''--- !u!114 &{fileid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fileid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {{r: {color[0]}, g: {color[1]}, b: {color[2]}, a: {color[3]}}}
  m_RaycastTarget: {raycast}
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
{sprite_line}
  m_Type: {img_type}
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
'''


def make_canvas_renderer_yaml(fileid: int, go_fileid: int) -> str:
    return f'''--- !u!222 &{fileid}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fileid}}}
  m_CullTransparentMesh: 1
'''


def make_mask_yaml(fileid: int, go_fileid: int) -> str:
    return f'''--- !u!114 &{fileid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fileid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 31a19414c41e5ae4aae2af33fee712f6, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_ShowMaskGraphic: 1
'''


def main():
    if not SCENE_PATH.exists():
        raise FileNotFoundError(SCENE_PATH)

    shutil.copy(SCENE_PATH, BACKUP_PATH)
    print(f'Backup saved to {BACKUP_PATH}')

    content = SCENE_PATH.read_text(encoding='utf-8')

    used_ids = {int(m.group(1)) for m in re.finditer(r'&(\d+)', content)}
    next_id = max(used_ids) + 1

    slot1_go = SLOTS['Slot1']
    slot1_components = get_component_fileids_of_go(content, slot1_go)
    slot1_mask_cid = max(slot1_components)  # Mask is the last component
    slot1_rt = get_rect_transform(content, slot1_go)
    slot1_img = get_image_component(content, slot1_go)

    # Extract Slot1's background/Icon and dummy/Image as templates
    bg_go = SLOT1_CHILDREN[0]
    bg_rt = get_rect_transform(content, bg_go)
    bg_components = get_component_fileids_of_go(content, bg_go)
    bg_img = get_image_component(content, bg_go)

    icon_go = SLOT1_BACKGROUND_ICON
    icon_rt = get_rect_transform(content, icon_go)
    icon_components = get_component_fileids_of_go(content, icon_go)
    icon_img = get_image_component(content, icon_go)

    level_go = SLOT1_CHILDREN[1]
    level_rt = get_rect_transform(content, level_go)
    level_components = get_component_fileids_of_go(content, level_go)

    dummy_go = SLOT1_CHILDREN[2]
    dummy_rt = get_rect_transform(content, dummy_go)
    dummy_components = get_component_fileids_of_go(content, dummy_go)
    dummy_img = get_image_component(content, dummy_go)

    dummy_child_go = SLOT1_DUMMY_IMAGE
    dummy_child_rt = get_rect_transform(content, dummy_child_go)
    dummy_child_components = get_component_fileids_of_go(content, dummy_child_go)
    dummy_child_img = get_image_component(content, dummy_child_go)

    new_objects = []

    for slot_name in ['Slot2', 'Slot3', 'Slot4', 'Slot5', 'Slot6', 'Slot7', 'Slot8']:
        print(f'Processing {slot_name}...')
        go_fid = SLOTS[slot_name]

        # 1. Add Mask component to slot GameObject
        go_block = get_object_block(content, go_fid)
        comp_ids = get_component_fileids_of_go(content, go_fid)
        if slot1_mask_cid not in comp_ids:
            # Add Mask if not present
            new_mask_id = next_id
            next_id += 1
            comp_match = re.search(r'(  m_Component:\n(?:  - component: \{fileID: \d+\}\n)+)', go_block)
            if comp_match:
                old_comp_section = comp_match.group(1)
                new_comp_section = old_comp_section.rstrip('\n') + f'\n  - component: {{fileID: {new_mask_id}}}' + '\n'
                go_block = go_block.replace(old_comp_section, new_comp_section)
                content = replace_object_block(content, go_fid, go_block)
                new_objects.append(make_mask_yaml(new_mask_id, go_fid))
            else:
                raise ValueError(f'Could not parse components for {slot_name}')

        # 2. Update slot Image sprite/type
        img_cid = get_image_component(content, go_fid)
        img_block = get_object_block(content, img_cid)
        img_block = set_field(img_block, '  m_Sprite', f'{{fileID: 21300000, guid: {SLOT1_SPRITE}, type: 3}}')
        img_block = set_field(img_block, '  m_Type', '0')
        content = replace_object_block(content, img_cid, img_block)

        # 3. Update children
        rt_fid = get_rect_transform(content, go_fid)
        children = get_children_from_rt(content, rt_fid)
        sx, sy = get_size_of_slot(content, rt_fid)

        # First child -> background (inactive, add Icon child)
        if len(children) >= 1:
            first_child = children[0]
            first_child_go = get_go_from_component(content, first_child)
            first_block = get_object_block(content, first_child_go)
            first_block = set_field(first_block, '  m_Name', 'background')
            first_block = set_field(first_block, '  m_IsActive', '0')
            content = replace_object_block(content, first_child_go, first_block)

            # Add Icon child to background
            new_icon_go = next_id; next_id += 1
            new_icon_rt = next_id; next_id += 1
            new_icon_img = next_id; next_id += 1
            new_icon_cr = next_id; next_id += 1

            new_objects.append(make_gameobject_yaml(new_icon_go, 'Icon', 5,
                                                    [new_icon_rt, new_icon_cr, new_icon_img]))
            new_objects.append(make_rect_transform_yaml(
                new_icon_rt, new_icon_go, first_child, 0, [],
                (0.5, 0.5), (0.5, 0.5), (0, 0), (sx, sy), (0.5, 0.5)))
            new_objects.append(make_image_yaml(
                new_icon_img, new_icon_go, (1, 1, 1, 1), '0', 0, 1))
            new_objects.append(make_canvas_renderer_yaml(new_icon_cr, new_icon_go))

            # Update background's RectTransform children list
            first_rt_block = get_object_block(content, first_child)
            if 'm_Children: []' in first_rt_block:
                first_rt_block = first_rt_block.replace(
                    'm_Children: []',
                    f'm_Children:\n  - {{fileID: {new_icon_rt}}}')
            else:
                first_rt_block = re.sub(
                    r'm_Children:\n(?:  - \{fileID: \d+\}\n)*',
                    f'm_Children:\n  - {{fileID: {new_icon_rt}}}\n',
                    first_rt_block)
            content = replace_object_block(content, first_child, first_rt_block)

        # Second child -> Level (inactive)
        if len(children) >= 2:
            second_child = children[1]
            second_child_go = get_go_from_component(content, second_child)
            second_block = get_object_block(content, second_child_go)
            second_block = set_field(second_block, '  m_Name', 'Level')
            second_block = set_field(second_block, '  m_IsActive', '0')
            content = replace_object_block(content, second_child_go, second_block)
            # Set text to "1级" on the TextMeshPro component
            second_child_go = get_go_from_component(content, second_child)
            for cid in get_component_fileids_of_go(content, second_child_go):
                comp_block = get_object_block(content, cid)
                if 'm_text:' in comp_block:
                    comp_block = set_field(comp_block, '  m_text', r'"1\u7EA7"')
                    content = replace_object_block(content, cid, comp_block)

        # 4. Add dummy child (active) with Image child
        new_dummy_go = next_id; next_id += 1
        new_dummy_rt = next_id; next_id += 1
        new_dummy_img = next_id; next_id += 1
        new_dummy_cr = next_id; next_id += 1
        new_dummy_child_go = next_id; next_id += 1
        new_dummy_child_rt = next_id; next_id += 1
        new_dummy_child_img = next_id; next_id += 1
        new_dummy_child_cr = next_id; next_id += 1

        new_objects.append(make_gameobject_yaml(new_dummy_go, 'dummy', 5,
                                                [new_dummy_rt, new_dummy_cr, new_dummy_img]))
        new_objects.append(make_rect_transform_yaml(
            new_dummy_rt, new_dummy_go, rt_fid, 2, [new_dummy_child_rt],
            (0, 0), (1, 1), (0, 0), (-10, -10.000013), (0.5, 0.5)))
        new_objects.append(make_image_yaml(
            new_dummy_img, new_dummy_go, (0.5294118, 0.43529415, 0.31764707, 1), SLOT1_SPRITE, 0, 1))

        # Update first child's Image to match Slot1 background
        if len(children) >= 1:
            first_child_go = get_go_from_component(content, children[0])
            first_img = get_image_component(content, first_child_go)
            first_img_block = get_object_block(content, first_img)
            first_img_block = set_field(first_img_block, '  m_Color', '{r: 0.7960785, g: 0.65882355, b: 0.5058824, a: 1}')
            first_img_block = set_field(first_img_block, '  m_Sprite', f'{{fileID: 21300000, guid: {SLOT1_SPRITE}, type: 3}}')
            first_img_block = set_field(first_img_block, '  m_Type', '0')
            content = replace_object_block(content, first_img, first_img_block)
        new_objects.append(make_canvas_renderer_yaml(new_dummy_cr, new_dummy_go))

        new_objects.append(make_gameobject_yaml(new_dummy_child_go, 'Image', 5,
                                                [new_dummy_child_rt, new_dummy_child_cr, new_dummy_child_img]))
        new_objects.append(make_rect_transform_yaml(
            new_dummy_child_rt, new_dummy_child_go, new_dummy_rt, 0, [],
            (0.5, 0.5), (0.5, 0.5), (0, 0), (100, 100), (0.5, 0.5)))
        new_objects.append(make_image_yaml(
            new_dummy_child_img, new_dummy_child_go, (0.8000001, 0.6627451, 0.50980395, 1),
            SLOT1_DUMMY_CHILD_SPRITE, 0, 1))
        new_objects.append(make_canvas_renderer_yaml(new_dummy_child_cr, new_dummy_child_go))

        # Update slot RectTransform children list
        rt_block = get_object_block(content, rt_fid)
        # Replace children list with background, Level, dummy
        children_yaml = f'''m_Children:
  - {{fileID: {children[0]}}}
  - {{fileID: {children[1]}}}
  - {{fileID: {new_dummy_rt}}}
'''
        rt_block = re.sub(r'm_Children:\n(?:  - \{fileID: \d+\}\n)*', children_yaml, rt_block)
        content = replace_object_block(content, rt_fid, rt_block)

    # Append new objects at end of file
    if new_objects:
        # Ensure file ends with newline
        if not content.endswith('\n'):
            content += '\n'
        content += ''.join(new_objects)

    SCENE_PATH.write_text(content, encoding='utf-8')
    print(f'Done. Added {len(new_objects)} new objects.')


if __name__ == '__main__':
    main()
