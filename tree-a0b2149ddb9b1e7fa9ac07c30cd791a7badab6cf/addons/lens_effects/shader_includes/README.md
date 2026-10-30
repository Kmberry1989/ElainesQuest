`scene_data_inc.glsl` is the SceneData layout from Godot 4.7 stable:
https://github.com/godotengine/godot/blob/4.7-stable/servers/rendering/renderer_rd/shaders/scene_data_inc.glsl

It is provided under the accompanying `GODOT-LICENSE.txt`. Keeping the matching
header here lets clean, headless exports compile the lens shader without the
editor's built-in include resolver. Update it together with the pinned engine.
