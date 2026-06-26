extends SceneTree

const INPUT_DEADZONE := 0.5

func _initialize() -> void:
	configure_application()
	configure_autoloads()
	configure_input_map()
	ProjectSettings.save()
	quit()

func configure_application() -> void:
	ProjectSettings.set_setting("application/config/name", "Elaine's Quest")
	ProjectSettings.set_setting("application/run/main_scene", "res://Scenes/System/Bootstrap.tscn")
	ProjectSettings.set_setting("dotnet/project/assembly_name", "ElainesQuest")
	ProjectSettings.set_setting("rendering/renderer/rendering_method", "forward_plus")
	ProjectSettings.set_setting("rendering/renderer/rendering_method.mobile", "mobile")

func configure_autoloads() -> void:
	ProjectSettings.set_setting("autoload/GameManager", "*res://Scripts/Core/GameManager.cs")

func configure_input_map() -> void:
	set_action("move_left", [make_key_event(KEY_A), make_key_event(KEY_LEFT)])
	set_action("move_right", [make_key_event(KEY_D), make_key_event(KEY_RIGHT)])
	set_action("move_forward", [make_key_event(KEY_W), make_key_event(KEY_UP)])
	set_action("move_backward", [make_key_event(KEY_S), make_key_event(KEY_DOWN)])
	set_action("jump", [make_key_event(KEY_SPACE)])
	set_action("interact", [make_key_event(KEY_E)])

func set_action(action_name: String, events: Array[InputEvent]) -> void:
	ProjectSettings.set_setting("input/%s" % action_name, {
		"deadzone": INPUT_DEADZONE,
		"events": events,
	})

func make_key_event(keycode: Key) -> InputEventKey:
	var event := InputEventKey.new()
	event.physical_keycode = keycode
	return event
