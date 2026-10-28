# Godot runtime models

These are derived copies of the original project GLBs. Godot 4.7 cannot import
the originals that require `KHR_mesh_quantization`; the runtime copies use
floating-point mesh attributes. Textures, geometry, and scene structure are
retained. The originals remain at the paths recorded in `manifest.json`.

The manifest records each original file's SHA-256 and its authored Godot import
scale. That scale is baked into the derived scene so the existing wrappers keep
their intended size without depending on a local `.glb.import` file.

Regenerate with `npm ci` and `node tools/prepare_runtime_models.cjs`. The scene
references and desktop export presets use these derived resources.
