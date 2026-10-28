extends SceneTree

func _init():
	var shader: RDShaderFile = load("res://addons/lens_effects/lens_flares.glsl")
	var spirv = shader.get_spirv()
	var error = spirv.get_stage_compile_error(RenderingDevice.SHADER_STAGE_COMPUTE)
	if not error.is_empty() or spirv.get_stage_bytecode(RenderingDevice.SHADER_STAGE_COMPUTE).is_empty():
		push_error("EXPORT_SHADER_VERIFY: FAIL " + error)
		quit(1)
		return
	print("EXPORT_SHADER_VERIFY: PASS")
	quit(0)
