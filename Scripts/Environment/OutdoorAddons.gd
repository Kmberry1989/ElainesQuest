@tool
extends Node3D

@export var sky_time: float = 9.5
@export var day_length_minutes: float = 24.0
@export var terrain_radius: int = 128
@export var terrain_base_y: float = -4.0
@export var terrain_height: float = 5.5
@export var grass_center: Vector3 = Vector3.ZERO
@export var grass_extent: Vector2i = Vector2i(18, 18)
@export var grass_spacing: int = 2
@export var grass_height: float = 0.05

var _rng := RandomNumberGenerator.new()


func _ready() -> void:
	if Engine.is_editor_hint():
		return

	_rng.seed = 1337
	_ensure_sky()
	await _ensure_terrain()
	_ensure_grass()


func _ensure_sky() -> void:
	if has_node("Sky3D"):
		return

	var sky: Node = load("res://addons/sky_3d/src/Sky3D.gd").new()
	sky.name = "Sky3D"
	sky.set("current_time", sky_time)
	sky.set("minutes_per_day", day_length_minutes)
	sky.set("editor_time_enabled", false)
	sky.set("game_time_enabled", true)
	add_child(sky, true)


func _ensure_terrain() -> void:
	if has_node("Terrain3D"):
		return

	var green_gradient := Gradient.new()
	green_gradient.set_color(0, Color(0.19, 0.31, 0.18))
	green_gradient.set_color(1, Color(0.34, 0.47, 0.28))
	var green_texture: Terrain3DTextureAsset = await _create_texture_asset("QuestGrass", green_gradient, 512, 0.006)
	green_texture.uv_scale = 0.09

	var dirt_gradient := Gradient.new()
	dirt_gradient.set_color(0, Color(0.34, 0.27, 0.18))
	dirt_gradient.set_color(1, Color(0.45, 0.36, 0.24))
	var dirt_texture: Terrain3DTextureAsset = await _create_texture_asset("QuestDirt", dirt_gradient, 512, 0.01)
	dirt_texture.uv_scale = 0.04

	var grass_mesh_asset := Terrain3DMeshAsset.new()
	grass_mesh_asset.name = "QuestTerrainGrass"
	grass_mesh_asset.generated_type = Terrain3DMeshAsset.TYPE_TEXTURE_CARD
	grass_mesh_asset.material_override.albedo_color = Color(0.46, 0.62, 0.38)

	var terrain := Terrain3D.new()
	terrain.name = "Terrain3D"
	add_child(terrain, true)

	terrain.material.world_background = Terrain3DMaterial.NONE
	terrain.material.auto_shader = true
	terrain.material.set_shader_param("auto_slope", 9)
	terrain.material.set_shader_param("blend_sharpness", 0.93)
	terrain.assets = Terrain3DAssets.new()
	terrain.assets.set_texture(0, green_texture)
	terrain.assets.set_texture(1, dirt_texture)
	terrain.assets.set_mesh_asset(0, grass_mesh_asset)

	var noise := FastNoiseLite.new()
	noise.frequency = 0.02

	var diameter := terrain_radius * 2
	var img := Image.create_empty(diameter, diameter, false, Image.FORMAT_RF)
	for x in img.get_width():
		for y in img.get_height():
			img.set_pixel(x, y, Color(noise.get_noise_2d(float(x), float(y)), 0.0, 0.0, 1.0))

	terrain.region_size = terrain_radius
	terrain.data.import_images([img, null, null], Vector3(-terrain_radius, terrain_base_y, -terrain_radius), 0.0, terrain_height)

	var transforms: Array[Transform3D] = []
	var half_radius := int(terrain_radius / 2)
	for x in range(-half_radius, half_radius, 6):
		for z in range(-half_radius, half_radius, 6):
			var pos := Vector3(float(x), 0.0, float(z))
			pos.y = terrain.data.get_height(pos)
			transforms.push_back(Transform3D(Basis(), pos))
	terrain.instancer.add_transforms(0, transforms)


func _ensure_grass() -> void:
	if has_node("ForegroundGrass"):
		return

	var grass: Node = load("res://addons/simplegrasstextured/grass.gd").new()
	grass.name = "ForegroundGrass"
	grass.set("albedo", Color(0.54, 0.71, 0.40))
	grass.set("interactive", false)
	grass.set("scale_h", 0.75)
	grass.set("scale_w", 0.65)
	grass.set("scale_var", -0.12)
	grass.set("grass_strength", 0.35)
	grass.set("optimization_by_distance", true)
	grass.set("optimization_dist_min", 6.0)
	grass.set("optimization_dist_max", 32.0)
	add_child(grass, true)

	var transforms: Array[Transform3D] = []
	var min_x := int(round(grass_center.x)) - grass_extent.x
	var max_x := int(round(grass_center.x)) + grass_extent.x
	var min_z := int(round(grass_center.z)) - grass_extent.y
	var max_z := int(round(grass_center.z)) + grass_extent.y
	for x in range(min_x, max_x + 1, grass_spacing):
		for z in range(min_z, max_z + 1, grass_spacing):
			if _rng.randf() < 0.28:
				continue
			var rotation := _rng.randf_range(0.0, TAU)
			var scale_value := _rng.randf_range(0.85, 1.2)
			var basis := Basis().rotated(Vector3.UP, rotation).scaled(Vector3(scale_value, scale_value, scale_value))
			var origin := Vector3(float(x), grass_height, float(z))
			transforms.append(Transform3D(basis, origin))

	grass.call("add_grass_batch", transforms)
	grass.call("_update_multimesh")


func _create_texture_asset(asset_name: String, gradient: Gradient, texture_size: int, noise_frequency: float) -> Terrain3DTextureAsset:
	var fnl := FastNoiseLite.new()
	fnl.frequency = noise_frequency

	var albedo_noise := NoiseTexture2D.new()
	albedo_noise.width = texture_size
	albedo_noise.height = texture_size
	albedo_noise.seamless = true
	albedo_noise.noise = fnl
	albedo_noise.color_ramp = gradient
	await albedo_noise.changed
	var albedo_image := albedo_noise.get_image()
	for x in albedo_image.get_width():
		for y in albedo_image.get_height():
			var clr := albedo_image.get_pixel(x, y)
			clr.a = clr.v
			albedo_image.set_pixel(x, y, clr)
	albedo_image.generate_mipmaps()

	var normal_noise := NoiseTexture2D.new()
	normal_noise.width = texture_size
	normal_noise.height = texture_size
	normal_noise.as_normal_map = true
	normal_noise.seamless = true
	normal_noise.noise = fnl
	await normal_noise.changed
	var normal_image := normal_noise.get_image()
	for x in normal_image.get_width():
		for y in normal_image.get_height():
			var clr := normal_image.get_pixel(x, y)
			clr.a = 0.82
			normal_image.set_pixel(x, y, clr)
	normal_image.generate_mipmaps()

	var asset := Terrain3DTextureAsset.new()
	asset.name = asset_name
	asset.albedo_texture = ImageTexture.create_from_image(albedo_image)
	asset.normal_texture = ImageTexture.create_from_image(normal_image)
	return asset
