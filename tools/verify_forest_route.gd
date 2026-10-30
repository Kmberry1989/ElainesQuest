extends SceneTree
## Checks Forest gates and traversal with actual input/physics after defeating encounters.
## Does not substitute for combat tactics or a manual quest playthrough.
var player: CharacterBody3D
var passed := true

func _initialize() -> void:
	call_deferred("_run")
	call_deferred("_watchdog")

func _run() -> void:
	var forest: Node = load("res://Scenes/Levels/World_1_Forest.tscn").instantiate()
	root.add_child(forest)
	current_scene = forest
	await create_timer(1).timeout
	player = get_first_node_in_group("player")
	var manager := root.get_node("GameManager")
	manager.call("BefriendAnimal", "Toby")
	for enemy in get_nodes_in_group("enemy"):
		var health: Node = enemy.get_node_or_null("Health")
		if health != null:
			health.call("ApplyDamage", 999.0)
	await create_timer(2).timeout
	_check(not forest.get_node("FirstEncounterGate").visible and not forest.get_node("SecondEncounterGate").visible, "encounter gates unlock")
	var dialogue: Node = forest.get_node_or_null("DialogueUI")
	if dialogue != null:
		dialogue.call("HideDialogue")
	var names := ["StartMeadow", "StepPlatformA", "StepPlatformB", "LowerArena", "RidgeBridge", "CliffStep", "UpperArena", "FinalSanctuary"]
	for i in range(names.size() - 1):
		var previous: Node3D = forest.get_node(names[i])
		var next: Node3D = forest.get_node(names[i + 1])
		var previous_shape: CollisionShape3D = previous.get_node("CollisionShape3D")
		var next_shape: CollisionShape3D = next.get_node("CollisionShape3D")
		var right: float = previous.global_position.x + previous_shape.shape.size.x * 0.5
		var left: float = next.global_position.x - next_shape.shape.size.x * 0.5
		# Allow time to gain height before reaching the raised platform's side.
		var launch := minf(right - 0.6, left - 4.0)
		if not await _move_to(launch):
			_check(false, "approach " + names[i + 1])
			break
		Input.action_press("jump")
		var reached := await _move_to(left + 1.4)
		Input.action_release("jump")
		await create_timer(0.9).timeout
		var top: float = next.global_position.y + next_shape.shape.size.y * 0.5
		var landed: bool = reached and player.is_on_floor() and absf(player.global_position.y - top) < 0.15
		_check(landed, "jump and land on " + names[i + 1] + " at " + str(player.global_position))
		if not landed:
			break
	Input.action_release("move_right")
	Input.action_release("jump")
	print("FOREST_ROUTE_VERIFY: ", "PASS" if passed else "FAIL")
	quit(0 if passed else 1)

func _move_to(x: float) -> bool:
	Input.action_press("move_right")
	for frame in range(400):
		await physics_frame
		if player.global_position.x >= x:
			Input.action_release("move_right")
			return true
		if player.global_position.y < -1.0:
			break
	Input.action_release("move_right")
	return false

func _check(condition: bool, description: String) -> void:
	passed = passed and condition
	print("FOREST_ROUTE_VERIFY: ", "PASS " if condition else "FAIL ", description)

func _watchdog() -> void:
	await create_timer(75).timeout
	push_error("FOREST_ROUTE_VERIFY: timed out")
	quit(1)
