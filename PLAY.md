# Play Elaine's Quest

Download the archive for your computer from the repository's Releases page.
The builds include the game engine and .NET runtime; Godot and a developer setup are not required.

- **Windows (64-bit):** extract the entire ZIP, then open `ElainesQuest.exe`. Keep its data files and folders beside it.
- **Mac (Apple Silicon or Intel):** extract the ZIP, then open `ElainesQuest.app`. This preview uses an ad-hoc signature and is not Apple-notarized. If macOS blocks the trusted download, use **System Settings → Privacy & Security → Open Anyway** after attempting to open it.

Move with **WASD / arrow keys**, jump with **Space**, interact or advance dialogue with **E**, cast with **F**, open the spell wheel with **Tab**, and wave with **Q**. **Escape** closes dialogue. A gamepad can also move Elaine, jump, interact, and cast.

Begin in the Hub, talk to Sasha, and enter the Forest portal. Find Toby, clear the encounters, rescue Matthew, and return to the Hub. Follow the objective text for later areas.

This is an early playable build. Progress currently lasts for the running session; there is no disk save system. Close the game window to quit.

## Rebuild

Use Godot **4.7 stable .NET**, its matching .NET export templates, and .NET 8.

```sh
python3 tools/prepare_desktop_exports.py
godot --headless --editor --path . --import
godot --headless --path . --export-release macOS build/macos/ElainesQuest.app
# On Windows, export the Windows preset instead.
godot --headless --path . --export-release Windows build/windows/ElainesQuest.exe
```

Run the exported executable with `--headless -- --verify-export` to check packaged models, movement, the active camera, Forest travel, and return to the Hub. This is an integration check, not a visual playthrough or a full quest-completion test.
