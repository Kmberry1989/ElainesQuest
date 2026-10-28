"""Generate desktop presets, including dynamically referenced models and scenes."""
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parent.parent
scenes = sorted((ROOT / 'Scenes').rglob('*.tscn'))
resources = {'res://' + path.relative_to(ROOT).as_posix() for path in scenes}
for scene in scenes:
    resources.update(re.findall(r'ModelPath = "(res://[^"]+)"', scene.read_text()))
    resources.update(re.findall(r'path="(res://[^"]+)"', scene.read_text()))
# A fresh import can encounter typed GDExtension scripts before registration.
# Select game scripts explicitly rather than relying on that first dependency scan.
for path in (ROOT / 'Scripts').rglob('*'):
    if path.suffix in {'.gd', '.cs'}:
        resources.add('res://' + path.relative_to(ROOT).as_posix())
resources.update({
    'res://addons/terrain_3d/terrain.gdextension',
    'res://addons/sky_3d/src/Sky3D.gd',
    'res://addons/lens_effects/world_environment.gd',
    'res://addons/lens_effects/lens_flare_compositor_effect.gd',
})
# These runtime add-ons also use class_name, dynamic loads, shader includes, and
# native libraries that are not all discoverable from a PackedScene dependency.
for addon in ['terrain_3d', 'lovely_event', 'simplegrasstextured', 'sky_3d',
              'lens_effects', 'advanced_radial_menu', 'extendable-scene-transitions']:
    for path in (ROOT / 'addons' / addon).rglob('*'):
        if path.is_file() and 'bin' not in path.relative_to(ROOT / 'addons' / addon).parts:
            if path.suffix in {'.gd', '.tscn', '.tres', '.res', '.gdshader', '.gdshaderinc',
                               '.glsl', '.png', '.jpg', '.jpeg', '.webp', '.svg', '.gdextension'}:
                resources.add('res://' + path.relative_to(ROOT).as_posix())
files = ', '.join('"' + path + '"' for path in sorted(resources))
sections = []
for index, (name, platform, output, options) in enumerate([
    ('macOS', 'macOS', 'build/macos/ElainesQuest.app', '''binary_format/architecture="universal"
application/bundle_identifier="com.kylematthewberry.elainesquest"
application/app_category="Adventure-games"
application/min_macos_version_x86_64="11.0"
application/min_macos_version_arm64="11.0"
codesign/codesign=3
codesign/identity="-"
codesign/entitlements/disable_library_validation=true
notarization/notarization=0'''),
    ('Windows', 'Windows Desktop', 'build/windows/ElainesQuest.exe', '''binary_format/architecture="x86_64"
binary_format/embed_pck=false
application/product_name="Elaine's Quest"
application/file_description="Elaine's Quest adventure game"
codesign/enable=false'''),
]):
    sections.append(f'''[preset.{index}]
name="{name}"
platform="{platform}"
runnable=true
advanced_options=false
dedicated_server=false
custom_features=""
export_filter="resources"
export_files=PackedStringArray({files})
include_filter="sounds/*,Assets/generated/*,addons/lens_effects/*.glsl"
exclude_filter=".git/*,.venv/*,node_modules/*,pet-runs/*,demo/*,docs/*,build/*,*.csproj.old"
export_path="{output}"
encryption_include_filters=""
encryption_exclude_filters=""
encrypt_pck=false
encrypt_directory=false
script_export_mode=2

[preset.{index}.options]
{options}
dotnet/include_scripts_content=false
dotnet/include_debug_symbols=false
dotnet/embed_build_outputs=false
''')
(ROOT / 'export_presets.cfg').write_text('\n'.join(sections))
print(f'Prepared desktop exports: {len(scenes)} scenes, {len(resources)} explicit resources.')
