extends SceneTree

func _initialize() -> void:
	var scene := load("res://Scenes/Environment/OutdoorAddons.tscn") as PackedScene
	if scene == null:
		push_error("Failed to load OutdoorAddons.tscn")
		quit(1)
		return

	var root := Node.new()
	root.name = "SmokeRoot"
	get_root().add_child(root)
	root.add_child(scene.instantiate())
	await process_frame
	quit()
