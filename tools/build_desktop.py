"""Install matching Godot tools if needed, export, verify, and package a desktop build."""
import argparse
import os
import pathlib
import plistlib
import shutil
import subprocess
import sys
import urllib.request
import zipfile

ROOT = pathlib.Path(__file__).resolve().parent.parent
VERSION = '4.7-stable'
TEMPLATE_VERSION = '4.7.stable.mono'
parser = argparse.ArgumentParser()
parser.add_argument('platform', choices=['macos', 'windows'])
parser.add_argument('--godot', help='Path to an already installed Godot 4.7 .NET editor')
args = parser.parse_args()
tools = ROOT / '.desktop-tools'
build = ROOT / 'build'
tools.mkdir(exist_ok=True)
build.mkdir(exist_ok=True)
log_file = build / f'build-{args.platform}.log'

def run(command, timeout=1200):
    print('Running:', subprocess.list2cmdline([str(part) for part in command]), flush=True)
    with log_file.open('a') as log:
        process = subprocess.run([str(part) for part in command], cwd=ROOT, stdout=log,
                                 stderr=subprocess.STDOUT, timeout=timeout)
    if process.returncode:
        text = log_file.read_text(errors='replace')
        errors = [line for line in text.splitlines() if 'ERROR:' in line or 'error ' in line.lower()]
        print('\n'.join(errors[-15:]))
        print(text[-3000:])
        raise RuntimeError(f'Command failed with exit {process.returncode}; see {log_file}')

def download(name):
    target = tools / name
    if not target.exists():
        url = f'https://github.com/godotengine/godot-builds/releases/download/{VERSION}/{name}'
        print('Downloading', name, flush=True)
        with urllib.request.urlopen(url) as response, target.open('wb') as output:
            shutil.copyfileobj(response, output)
    return target

if args.godot:
    godot = pathlib.Path(args.godot).resolve()
else:
    suffix = 'macos.universal' if args.platform == 'macos' else 'win64'
    editor = download(f'Godot_v{VERSION}_mono_{suffix}.zip')
    if args.platform == 'macos':
        run(['ditto', '-x', '-k', editor, tools])
        godot = tools / 'Godot_mono.app/Contents/MacOS/Godot'
    else:
        with zipfile.ZipFile(editor) as archive:
            archive.extractall(tools)
        godot = next(tools.rglob('Godot*_console.exe'))
    editor.unlink()

if sys.platform == 'darwin':
    template_base = pathlib.Path.home() / 'Library/Application Support/Godot/export_templates'
else:
    template_base = pathlib.Path(os.environ['APPDATA']) / 'Godot/export_templates'
templates = template_base / TEMPLATE_VERSION
required = templates / ('macos.zip' if args.platform == 'macos' else 'windows_release_x86_64.exe')
if not required.exists():
    archive_path = download(f'Godot_v{VERSION}_mono_export_templates.tpz')
    templates.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(archive_path) as archive:
        for info in archive.infolist():
            relative = pathlib.PurePosixPath(info.filename)
            if relative.parts[0] != 'templates' or info.is_dir():
                continue
            target = templates.joinpath(*relative.parts[1:])
            target.parent.mkdir(parents=True, exist_ok=True)
            with archive.open(info) as source, target.open('wb') as output:
                shutil.copyfileobj(source, output)
    archive_path.unlink()

version = subprocess.check_output([str(godot), '--version'], text=True).strip()
if not version.startswith('4.7.stable.mono.'):
    raise RuntimeError(f'Expected Godot 4.7 stable .NET, got {version}')

run([sys.executable, 'tools/prepare_desktop_exports.py'])
run(['dotnet', 'build', 'ElainesQuest.csproj', '-v:minimal'])
run([godot, '--headless', '--editor', '--path', ROOT, '--import'])
# On a fresh checkout, native extension/global script registration happens during
# the first scan. Reopen once with that registration available to scene parsers.
run([godot, '--headless', '--editor', '--path', ROOT, '--import'])
run([godot, '--headless', '--path', ROOT, '--script', 'tools/verify_export_shaders.gd'])
folder = build / args.platform
folder.mkdir(exist_ok=True)
executable = folder / ('ElainesQuest.app' if args.platform == 'macos' else 'ElainesQuest.exe')
if executable.is_dir():
    shutil.rmtree(executable)
elif executable.exists():
    executable.unlink()
run([godot, '--headless', '--path', ROOT, '--export-release',
     'macOS' if args.platform == 'macos' else 'Windows', executable])

if args.platform == 'macos':
    with (executable / 'Contents/Info.plist').open('rb') as source:
        bundle = plistlib.load(source)
    game = executable / 'Contents/MacOS' / bundle['CFBundleExecutable']
    run(['codesign', '--verify', '--deep', '--strict', executable])
else:
    game = executable
verification = build / f'validation-{args.platform}.log'
verification.unlink(missing_ok=True)
run([game, '--headless', '--log-file', verification.resolve(), '--', '--verify-export'], timeout=180)
text = verification.read_text(errors='replace')
if 'EXPORT_VERIFY: PASS' not in text or 'EXPORT_VERIFY: FAIL' in text:
    raise RuntimeError('The exported executable did not pass its integration check: ' + text[-12000:])
if any(marker in text for marker in ['SCRIPT ERROR:', 'SHADER ERROR:', 'Failed loading resource:']):
    raise RuntimeError('The exported executable reported a script, shader, or resource error: ' + text[-12000:])

shutil.copyfile(ROOT / 'PLAY.md', folder / 'READ-ME.md')
shutil.copyfile(ROOT / 'LICENSE.md', folder / 'LICENSE.md')
shutil.copyfile(ROOT / 'addons/lens_effects/shader_includes/GODOT-LICENSE.txt', folder / 'GODOT-LICENSE.txt')
shutil.copyfile(verification, folder / 'BUILD-VALIDATION.log')
output = build / f'ElainesQuest-{"macOS" if args.platform == "macos" else "Windows"}.zip'
if args.platform == 'macos':
    run(['ditto', '-c', '-k', '--sequesterRsrc', folder, output])
else:
    with zipfile.ZipFile(output, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path in sorted(folder.rglob('*')):
            if path.is_file():
                archive.write(path, path.relative_to(folder))
print(f'PLAYABLE_BUILD: {output} ({output.stat().st_size} bytes)', flush=True)
