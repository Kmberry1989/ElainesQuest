@tool
extends Node3D
## One stable daylight rig and a quiet landscape around the authored play spaces.
## The Hub's flat clearing and its collision share exactly the same height.

@export var terrain_radius: int = 112
@export var terrain_height: float = 8.0
var _forest := false
var _noise := FastNoiseLite.new()


func _ready() -> void:
	if Engine.is_editor_hint():
		return
	_forest = get_parent().name == "World1Forest"
	_noise.seed = 1337
	_noise.frequency = 0.025
	_ensure_daylight()
	_build_landscape()
	call_deferred("_dress_world")


func _ensure_daylight() -> void:
	var sun := DirectionalLight3D.new()
	sun.name = "QuestSun"
	sun.rotation_degrees = Vector3(-52, -32, 0)
	sun.light_color = Color(1.0, 0.94, 0.84)
	sun.light_energy = 1.05
	sun.light_angular_distance = 2.0
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 80.0
	add_child(sun)

	var world := WorldEnvironment.new()
	world.name = "QuestEnvironment"
	var environment := Environment.new()
	var sky_material := ProceduralSkyMaterial.new()
	sky_material.sky_top_color = Color(0.24, 0.48, 0.69)
	sky_material.sky_horizon_color = Color(0.77, 0.85, 0.86)
	sky_material.ground_bottom_color = Color(0.23, 0.32, 0.24)
	sky_material.ground_horizon_color = sky_material.sky_horizon_color
	sky_material.sky_curve = 0.18
	sky_material.sun_angle_max = 8.0
	var sky := Sky.new()
	sky.sky_material = sky_material
	environment.sky = sky
	environment.background_mode = Environment.BG_SKY
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_SKY
	environment.ambient_light_energy = 0.65
	environment.reflected_light_source = Environment.REFLECTION_SOURCE_SKY
	environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	environment.ssao_enabled = true
	environment.ssao_radius = 1.2
	environment.ssao_intensity = 1.25
	environment.glow_enabled = false
	environment.fog_enabled = true
	environment.fog_density = 0.0025
	environment.fog_light_color = Color(0.67, 0.78, 0.8)
	environment.fog_light_energy = 0.65
	environment.fog_aerial_perspective = 0.35
	world.environment = environment
	add_child(world)


func ground_height(x: float, z: float) -> float:
	if _forest:
		# Keep scenery below the authored platform course and its hazard volume.
		return -9.0 + maxf(absf(z) - 17.0, 0.0) * 0.13 + _noise.get_noise_2d(x, z) * 2.0
	var distance := Vector2(x, z).length()
	var hills := smoothstep(29.0, 65.0, distance)
	return hills * (4.0 + (_noise.get_noise_2d(x, z) + 1.0) * terrain_height)


func _path_distance(point: Vector2) -> float:
	var distance := point.length() - 5.0
	for end in [Vector2(0, -17), Vector2(-17, 8), Vector2(17, 8), Vector2(0, 19)]:
		var nearest := Geometry2D.get_closest_point_to_segment(point, Vector2.ZERO, end)
		distance = minf(distance, point.distance_to(nearest) - 1.5)
	return distance


func _build_landscape() -> void:
	var surface := SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	surface.set_smooth_group(0)
	# Fine vertices near paths; one mesh, no runtime noise textures or terrain import.
	var step := 1.0
	var radius := terrain_radius
	for x in range(-radius, radius):
		for z in range(-radius, radius):
			for corner in [Vector2(0, 0), Vector2(1, 0), Vector2(0, 1), Vector2(1, 0), Vector2(1, 1), Vector2(0, 1)]:
				var px: float = (x + corner.x) * step
				var pz: float = (z + corner.y) * step
				var variation := (_noise.get_noise_2d(px * 4.0, pz * 4.0) + 1.0) * 0.5
				var color := Color(0.24, 0.36, 0.16).lerp(Color(0.36, 0.46, 0.23), variation)
				if not _forest:
					var path_weight := 1.0 - smoothstep(-0.3, 0.7, _path_distance(Vector2(px, pz)))
					color = color.lerp(Color(0.62, 0.53, 0.36), path_weight * 0.85)
				surface.set_color(color)
				surface.set_uv(Vector2(px, pz) * 0.25)
				surface.add_vertex(Vector3(px, ground_height(px, pz), pz))
	surface.generate_normals()
	surface.index()
	var landscape := MeshInstance3D.new()
	landscape.name = "MeadowLandscape"
	landscape.mesh = surface.commit()
	var material := StandardMaterial3D.new()
	material.vertex_color_use_as_albedo = true
	material.vertex_color_is_srgb = true
	material.roughness = 0.95
	landscape.material_override = material
	add_child(landscape)
	if not _forest:
		landscape.create_trimesh_collision()


func _dress_world() -> void:
	var dressing = load("res://Scripts/Environment/GardenDressing.gd").new()
	dressing.name = "GardenDressing"
	add_child(dressing)
	dressing.build(get_parent(), self, _forest)
