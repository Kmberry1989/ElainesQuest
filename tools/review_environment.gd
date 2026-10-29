extends SceneTree
## Native graphics review, separate from the headless export contract.
## godot --path . --script tools/review_environment.gd -- --review-forest --review-output=/absolute/path.png

func _initialize() -> void:
	call_deferred("_review")
	call_deferred("_watchdog")

func _review() -> void:
	var args := OS.get_cmdline_user_args()
	var scene_path := "res://Scenes/Levels/World_1_Forest.tscn" if "--review-forest" in args else "res://Scenes/Hub/HubWorld.tscn"
	var world: Node = load(scene_path).instantiate()
	root.add_child(world)
	current_scene = world
	await create_timer(10.0 if "--review-profile" in args else 3.0).timeout
	var player := get_first_node_in_group("player") as Node3D
	var camera := root.get_camera_3d()
	if "--review-overview" in args:
		camera.set_process(false)
		var center := Vector3(20, 2, 0) if "--review-forest" in args else Vector3(0, 0, -2)
		camera.global_position = center + (Vector3(12, 50, 65) if "--review-forest" in args else Vector3(8, 25, 30))
		camera.look_at(center)
	for node in world.find_children("*", "MeshInstance3D", true, false):
		if "__RuntimeModel" in str(node.get_path()):
			var mesh: Mesh = node.bake_mesh_from_current_skeleton_pose() if node.skin != null else node.mesh
			print("REVIEW_MODEL ", node.get_path(), " ", node.global_transform * mesh.get_aabb())
	print("REVIEW_PLAYER ", player.global_position, " CAMERA ", camera.global_position, " FPS ", Engine.get_frames_per_second())
	if "--review-dialogue" in args:
		world.find_child("DialogueUI", true, false).call("ShowDialogue", "Sasha", "Matthew was last seen beyond the stone path. Toby knows the route.")
	await process_frame
	await RenderingServer.frame_post_draw
	for arg in args:
		if arg.begins_with("--review-output="):
			var output := arg.trim_prefix("--review-output=")
			root.get_texture().get_image().save_png(output)
			print("VISUAL_REVIEW: saved ", output)
	if "--review-stay" not in args:
		quit()

func _watchdog() -> void:
	await create_timer(40).timeout
	push_error("VISUAL_REVIEW: timed out")
	quit(1)
