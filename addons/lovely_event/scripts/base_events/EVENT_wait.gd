extends EVENT
## waits X seconds before continuing queue.
class_name EVENT_wait

var wait_time : float;

## waits [time] seconds before continuing queue.
func _init( time : float = 1.0, skippable : bool = false ) -> void:
	wait_time = time;
	is_skippable = skippable;
	super._init();


func execute( _looping : bool, _dt : float ) -> RETURNTYPE:
	wait_time = max( wait_time-_dt, 0 );
	return RETURNTYPE.FINISHED if wait_time == 0 else RETURNTYPE.UNFINISHED;


func on_skip() -> void:
	pass # nothing needs to be done on skipping this event
