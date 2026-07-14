extends EVENT
## sets whether upcoming events can be skipped or not
class_name EVENT_canSkip

var skip : bool;

func _init( can_skip : bool ) -> void:
	skip = can_skip;
	super._init();


func execute( _looping : bool, _dt : float ) -> RETURNTYPE:
	current_queue.can_skip = skip;
	if not skip and current_queue.is_skipping:
		current_queue.is_skipping = false;
	return RETURNTYPE.FINISHED;
