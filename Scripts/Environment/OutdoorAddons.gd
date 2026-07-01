@tool
extends Node3D

@export var sky_time: float = 9.5
@export var day_length_minutes: float = 24.0
@export var terrain_radius: int = 128
@export var terrain_base_y: float = -4.0
@export var terrain_height: float = 5.5
@export var enable_lens_effects: bool = true
@export var sun_energy: float = 1.45
@export var sun_rotation_degrees: Vector3 = Vector3(-38.0, -28.0, 0.0)
@export var sun_color: Color = Color(1.0, 0.93, 0.82)
var _rng := RandomNumberGenerator.new()


func _ready() -> void:
	if Engine.is_editor_hint():
		return

	_rng.seed = 1337
	_ensure_sun()
	_ensure_world_environment()
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


func _ensure_sun() -> DirectionalLight3D:
	var sun := get_node_or_null("QuestSun") as DirectionalLight3D
	if sun == null:
		sun = DirectionalLight3D.new()
		sun.name = "QuestSun"
		add_child(sun, true)

	sun.light_energy = sun_energy
	sun.light_color = sun_color
	sun.shadow_enabled = true
	sun.rotation_degrees = sun_rotation_degrees
	return sun


func _ensure_world_environment() -> void:
	var world_environment := get_node_or_null("QuestEnvironment") as WorldEnvironment
	if world_environment == null:
		world_environment = load("res://addons/lens_effects/world_environment.gd").new()
		world_environment.name = "QuestEnvironment"

	var environment := world_environment.environment
	if environment == null:
		environment = Environment.new()
		world_environment.environment = environment

	environment.background_mode = Environment.BG_CLEAR_COLOR
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = Color(0.61, 0.7, 0.78)
	environment.ambient_light_energy = 0.9
	environment.ambient_light_sky_contribution = 0.35
	environment.tonemap_mode = Environment.TONE_MAPPER_ACES
	environment.glow_enabled = true
	environment.fog_enabled = true
	environment.fog_density = 0.01
	environment.fog_sun_scatter = 0.45
	environment.fog_aerial_perspective = 0.2
	environment.fog_light_color = Color(0.97, 0.83, 0.67)
	environment.fog_light_energy = 0.75

	if enable_lens_effects:
		var compositor := world_environment.compositor
		if compositor == null:
			compositor = Compositor.new()
			world_environment.compositor = compositor

		if compositor.compositor_effects.is_empty():
			var lens_effect = load("res://addons/lens_effects/lens_flare_compositor_effect.gd").new()
			lens_effect.Weight = 0.11
			lens_effect.SampleCount = 72
			lens_effect.Anamorphic_Intensity = 180.0
			lens_effect.sun_color = Color(0.98, 0.88, 0.7, 0.42)
			compositor.compositor_effects = [lens_effect]

	if world_environment.get_parent() == null:
		add_child(world_environment, true)

	world_environment.set("sun", _ensure_sun())


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
