extends SceneTree
## Preserve the supplied Mixamo swing as a lightweight animation-only runtime resource.
func _initialize() -> void:
	var source: Node = load("res://Assets/Animations/Mixamo/Heavy Weapon Swing.fbx").instantiate()
	var player: AnimationPlayer = source.find_child("AnimationPlayer", true, false)
	for clip_name in player.get_animation_list():
		if clip_name == "RESET":
			continue
		var clip: Animation = player.get_animation(clip_name).duplicate(true)
		clip.loop_mode = Animation.LOOP_NONE
		for track in range(clip.get_track_count() - 1, -1, -1):
			var path := str(clip.track_get_path(track))
			var colon := path.find(":")
			if colon < 0 or not "Skeleton3D" in path:
				clip.remove_track(track)
				continue
			clip.track_set_path(track, NodePath("RootNode/Skeleton3D" + path.substr(colon)))
		var result := ResourceSaver.save(clip, "res://Assets/Animations/MeleeStrike.res")
		source.free()
		print("MELEE_RESOURCE: ", result)
		quit(0 if result == OK else 1)
		return
	source.free()
	push_error("No supplied melee animation found")
	quit(1)
