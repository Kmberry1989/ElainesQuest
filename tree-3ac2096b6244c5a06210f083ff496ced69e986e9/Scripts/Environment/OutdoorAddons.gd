@tool
extends Node3D

@export var sky_time: float = 9.5
@export var day_length_minutes: float = 24.0
@export var terrain_radius: int = 128
@export var terrain_base_y: float = -4.0
@export var terrain_height: float = 5.5
var _rng := RandomNumberGenerator.new()


func _ready() -> void:
	if Engine.is_editor_hint():
		return

	_rng.seed = 1337
	_ensure_sky()
	await _ensure_terrain()


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

	var noise := FastNoiseLite.new()
	noise.frequency = 0.02

	var diameter := terrain_radius * 2
	var img := Image.create_empty(diameter, diameter, false, Image.FORMAT_RF)
	for x in img.get_width():
		for y in img.get_height():
			img.set_pixel(x, y, Color(noise.get_noise_2d(float(x), float(y)), 0.0, 0.0, 1.0))

	terrain.region_size = terrain_radius
	terrain.data.import_images([img, null, null], Vector3(-terrain_radius, terrain_base_y, -terrain_radius), 0.0, terrain_height)

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
