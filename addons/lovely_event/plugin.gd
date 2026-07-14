@tool
extends EditorPlugin


func _enable_plugin() -> void:
	# add singleton
	add_autoload_singleton( "LovelyEvent", "res://addons/lovely_event/scripts/LovelyEvent.gd" );


func _disable_plugin() -> void:
	# removes singleton
	remove_autoload_singleton( "LovelyEvent" );
