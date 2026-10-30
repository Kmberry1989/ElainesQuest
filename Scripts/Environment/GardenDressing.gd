extends Node3D
## Ground the supplied artwork by its measured bounds instead of assuming a GLB pivot.
## Decorative instances share imported resources; the gameplay scene owns all triggers.

const CLIFF := preload("res://Assets/Models/Environment/grassy_rock.glb")
const ART := "res://Assets/Models/Environment/"
var _landscape: Node3D
var _rng := RandomNumberGenerator.new()

func build(world: Node3D, landscape: Node3D, forest: bool) -> void:
	_landscape = landscape
	_rng.seed = 28028
	if forest:
		_dress_forest(world)
	else:
		_dress_hub(world)


func _bounds(node: Node3D) -> AABB:
	var meshes := node.find_children("*", "MeshInstance3D", true, false)
	if node is MeshInstance3D:
		meshes.append(node)
	var result := AABB()
	var first := true
	for mesh: MeshInstance3D in meshes:
		if mesh.mesh == null:
			continue
		var box: AABB = node.global_transform.affine_inverse() * mesh.global_transform * mesh.get_aabb()
		result = box if first else result.merge(box)
		first = false
	return result


func _place(node: Node3D, foot: Vector3, height: float, yaw: float = 0.0) -> void:
	node.scale = Vector3.ONE
	node.rotation = Vector3(0, yaw, 0)
	var box := _bounds(node)
	if box.size.y < 0.001:
		return
	node.scale = Vector3.ONE * height / box.size.y
	var anchor := Vector3(box.get_center().x, box.position.y, box.get_center().z)
	node.position = foot - node.basis * anchor


func _prop(filename: String, foot: Vector3, height: float, yaw: float = 0.0) -> Node3D:
	var prop: Node3D = load(ART + filename + ".glb").instantiate()
	add_child(prop)
	_place(prop, foot, height, yaw)
	return prop


func _dress_hub(world: Node3D) -> void:
	var placements := {
		"Ground/plant_bed": [Vector3(-8, 0, -7), 0.7],
		"Ground/plant_bed2": [Vector3(8, 0, -7), 0.7],
		"Ground/gnome1": [Vector3(-11, 0, -5), 1.2],
		"Ground/gnome2": [Vector3(11, 0, -5), 1.0],
		"plant_bed": [Vector3(-9, 0, -10), 0.7],
		"plant_bed2": [Vector3(9, 0, -10), 0.7],
		"well": [Vector3(-9, 0, 2), 1.5],
		"empty_plant_bed": [Vector3(-12, 0, -8), 0.4],
		"empty_plant_bed2": [Vector3(12, 0, -8), 0.4],
		"bed": [Vector3(8, 0, 3), 1.0],
		"Campfire": [Vector3(10, 0, 0), 0.65],
		"LampNearPortal": [Vector3(3.8, 0, -13), 3.4],
		"FlowerPatch": [Vector3(6, 0, -2), 0.35],
		"FenceNearPortal": [Vector3(-5, 0, -17), 1.15],
		"FenceNearPortal2": [Vector3(5, 0, -17), 1.15],
		"SignpostNearCamp": [Vector3(-3.4, 0, -9), 1.8]
	}
	for path: String in placements:
		var item: Node3D = world.get_node(path)
		var value: Array = placements[path]
		var foot: Vector3 = value[0]
		_place(item, item.get_parent().to_local(foot), value[1])
	for stage in range(5):
		var stage_node := world.get_node("FamilyTree/Stage%d" % stage)
		var visual: Node3D = stage_node.get_child(0)
		_place(visual, Vector3.ZERO, [1.2, 3.0, 4.0, 4.6, 5.0][stage])
	for portal_name in ["PortalToWorld1", "TownTrailPortal", "CaveTrailPortal", "StarfallSanctumPortal"]:
		var portal: Node3D = world.get_node(portal_name)
		var foot := portal.global_position
		foot.y = 0.0
		_place(portal.get_node("PortalStone"), portal.to_local(foot), 3.6)
		if portal_name != "PortalToWorld1":
			_portal_label(portal, str(portal.get("PortalName")))
	# A layered tree line frames the clearing without putting foliage over the camera.
	for i in range(42):
		var angle := TAU * i / 42.0
		var radius := _rng.randf_range(27.0, 37.0)
		var point := Vector3(cos(angle) * radius, 0, sin(angle) * radius)
		point.y = _landscape.ground_height(point.x, point.z)
		_prop("pointed_tree" if i % 3 == 0 else "rounded_tree", point, _rng.randf_range(6.0, 9.0), angle)
	for i in range(32):
		var angle := TAU * i / 32.0
		var point := Vector3(cos(angle) * _rng.randf_range(20, 24), 0, sin(angle) * _rng.randf_range(20, 24))
		if absf(point.x) < 3.5 or absf(point.z - absf(point.x) * 0.47) < 3.0:
			continue
		_prop("bush", point, _rng.randf_range(0.55, 1.1), angle)
	for point in [Vector3(-4.5, 0, -12), Vector3(4.5, 0, -12), Vector3(-5.5, 0, 3.5), Vector3(5.5, 0, 7)]:
		_prop("circle_flowers", point, 0.3)
	var controls := Label3D.new()
	controls.text = "FOREST TRAIL\nTalk to Sasha, then follow the path"
	controls.font_size = 38
	controls.pixel_size = 0.008
	controls.modulate = Color(1, 0.95, 0.78)
	controls.outline_modulate = Color(0.12, 0.2, 0.16)
	controls.position = Vector3(0, 4.4, -17)
	add_child(controls)


func _portal_label(portal: Node3D, title: String) -> void:
	var label := Label3D.new()
	label.text = title
	label.font_size = 32
	label.pixel_size = 0.007
	label.position = Vector3(0, 2.7, 0)
	label.modulate = Color(1, 0.95, 0.78)
	label.outline_modulate = Color(0.12, 0.2, 0.16)
	label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	portal.add_child(label)


func _dress_forest(world: Node3D) -> void:
	world.get_node("StartMeadow/StartBridge").queue_free()
	world.get_node("MatthewRescue/CollisionShape3D").position.y = 0.9
	for name in ["StartMeadow", "StepPlatformA", "StepPlatformB", "LowerArena", "RidgeBridge", "CliffStep", "UpperArena", "FinalSanctuary"]:
		var platform: Node3D = world.get_node(name)
		var shape: CollisionShape3D = platform.get_node("CollisionShape3D")
		var size: Vector3 = shape.shape.size
		var top: float = shape.position.y + size.y * 0.5
		# Fit the existing plank and bridge art to the actual walkable footprint.
		for child in platform.get_children():
			if child is Node3D and ("Visual" in child.name or "Platform" in child.name):
				child.transform = Transform3D.IDENTITY
				var box := _bounds(child)
				child.scale = Vector3(size.x / box.size.x, minf(0.5, size.y) / box.size.y, size.z / box.size.z)
				var anchor := Vector3(box.get_center().x, box.end.y, box.get_center().z)
				child.position = Vector3(0, top, 0) - child.basis * anchor
			elif child is Node3D and child is not CollisionShape3D:
				var foot: Vector3 = child.position
				foot.y = top
				_place(child, foot, 4.5 if "Tree" in child.name else 0.55)
		if name != "RidgeBridge":
			_cliff_foundation(platform, size, top)
	# Scenery remains outside the jumps, enemies, gates, and rescue triggers.
	for i in range(42):
		var x := _rng.randf_range(-33, 85)
		var z := _rng.randf_range(-27, -17) if i % 2 == 0 else _rng.randf_range(18, 28)
		var point := Vector3(x, _landscape.ground_height(x, z), z)
		_prop("pointed_tree" if i % 3 == 0 else "rounded_tree", point, _rng.randf_range(9, 15), _rng.randf() * TAU)
	for path in ["StartTreeA", "StartTreeB", "StartFlowers", "TrailSignpost", "BridgeLamp", "SanctuaryLampA", "SanctuaryLampB", "SanctuaryFlowers"]:
		var prop: Node3D = world.get_node(path)
		var foot := prop.position
		if path.begins_with("Start") or path == "TrailSignpost": foot.y = 0.0
		var height := 4.8 if "Tree" in path else (3.0 if "Lamp" in path else (1.8 if "Signpost" in path else 0.3))
		_place(prop, foot, height)
	var portal: Node3D = world.get_node("ReturnPortal")
	_place(portal.get_node("PortalStone"), portal.to_local(Vector3(portal.position.x, 9.4, portal.position.z)), 3.0 / portal.scale.y)


func _cliff_foundation(platform: Node3D, size: Vector3, top: float) -> void:
	var bottom := -8.0 - platform.position.y
	var foundation: Node3D = CLIFF.instantiate()
	foundation.name = "GroundedCliff"
	platform.add_child(foundation)
	var box := _bounds(foundation)
	foundation.scale = Vector3(size.x * 1.1 / box.size.x, (top - 0.3 - bottom) / box.size.y, size.z * 1.1 / box.size.z)
	var anchor := Vector3(box.get_center().x, box.end.y, box.get_center().z)
	foundation.position = Vector3(0, top - 0.3, 0) - foundation.basis * anchor
