extends SceneTree

const INPUT_DEADZONE := 0.5
const ANALOG_DEADZONE := 0.35

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
	set_action("move_left", [
		make_key_event(KEY_A),
		make_key_event(KEY_LEFT),
		make_joy_button_event(JOY_BUTTON_DPAD_LEFT),
		make_joy_axis_event(JOY_AXIS_LEFT_X, -1.0),
	], ANALOG_DEADZONE)
	set_action("move_right", [
		make_key_event(KEY_D),
		make_key_event(KEY_RIGHT),
		make_joy_button_event(JOY_BUTTON_DPAD_RIGHT),
		make_joy_axis_event(JOY_AXIS_LEFT_X, 1.0),
	], ANALOG_DEADZONE)
	set_action("move_forward", [
		make_key_event(KEY_W),
		make_key_event(KEY_UP),
		make_joy_button_event(JOY_BUTTON_DPAD_UP),
		make_joy_axis_event(JOY_AXIS_LEFT_Y, -1.0),
	], ANALOG_DEADZONE)
	set_action("move_backward", [
		make_key_event(KEY_S),
		make_key_event(KEY_DOWN),
		make_joy_button_event(JOY_BUTTON_DPAD_DOWN),
		make_joy_axis_event(JOY_AXIS_LEFT_Y, 1.0),
	], ANALOG_DEADZONE)
	set_action("jump", [make_key_event(KEY_SPACE), make_joy_button_event(JOY_BUTTON_A)])
	set_action("interact", [make_key_event(KEY_E), make_joy_button_event(JOY_BUTTON_X)])
	set_action("emote", [make_key_event(KEY_Q), make_joy_button_event(JOY_BUTTON_Y)])
	set_action("cast_magic", [make_key_event(KEY_F), make_joy_button_event(JOY_BUTTON_RIGHT_SHOULDER)])
	set_action("ui_cancel", [make_key_event(KEY_ESCAPE), make_joy_button_event(JOY_BUTTON_B), make_joy_button_event(JOY_BUTTON_START)])

func set_action(action_name: String, events: Array[InputEvent], deadzone: float = INPUT_DEADZONE) -> void:
	ProjectSettings.set_setting("input/%s" % action_name, {
		"deadzone": deadzone,
		"events": events,
	})

func make_key_event(keycode: Key) -> InputEventKey:
	var event := InputEventKey.new()
	event.physical_keycode = keycode
	return event

func make_joy_button_event(button_index: JoyButton) -> InputEventJoypadButton:
	var event := InputEventJoypadButton.new()
	event.button_index = button_index
	return event

func make_joy_axis_event(axis: JoyAxis, axis_value: float) -> InputEventJoypadMotion:
	var event := InputEventJoypadMotion.new()
	event.axis = axis
	event.axis_value = axis_value
	return event
